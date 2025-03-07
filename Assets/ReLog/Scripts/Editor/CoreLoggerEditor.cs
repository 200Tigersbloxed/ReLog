using System;
using ReLog.Networking;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
#if !DISABLE_RELOG_ANALYSIS
using ReLog.RoslynAnalyzer;
using System.Linq;
using System.Reflection;
using UnityEditor.Callbacks;
#endif

namespace ReLog.Editor
{
    [CustomEditor(typeof(CoreLogger))]
    public class CoreLoggerEditor : UnityEditor.Editor
    {
        private CoreLogger coreLogger;
        public SerializedProperty LoggerViews;
        public SerializedProperty ClearButtons;
        public SerializedProperty WarnColor;
        public SerializedProperty ErrorColor;
        public SerializedProperty UsePersistentColors;

#if !DISABLE_RELOG_ANALYSIS
        private bool generateStage;
        private AffectedFile[] affectedFiles = Array.Empty<AffectedFile>();
        private bool check;
        private string[] files;
#endif
        
        private int networkerCount;
        private bool generateNetworkers;
        private bool moreThanOne;

        private int networkersToCreate = 32;

#if !DISABLE_RELOG_ANALYSIS
        private string[] GetAssemblies()
        {
            // TODO: Waiting for assembly fix upstream
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location)) // Exclude in-memory and dynamic assemblies
                .Select(a => a.Location)
                .ToArray();
        }

        [DidReloadScripts]
        private static void ApplyToAllLoggers()
        {
            ApplyEditCache cache = ApplyEditCache.Load();
            if (cache == null) return;
            MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in allMonoBehaviours)
            {
                Type type = behaviour.GetType();
                FieldInfo loggerField = type.GetField("Logger");
                if (loggerField != null && loggerField.FieldType == typeof(CoreLogger))
                {
                    loggerField.SetValue(behaviour, cache.Logger);
                    EditorUtility.SetDirty(behaviour.gameObject);
                    // Check if the object is part of a prefab
                    GameObject prefabRoot = PrefabUtility.GetCorrespondingObjectFromSource(behaviour.gameObject);
                    if (prefabRoot != null)
                    {
                        // Apply the change to the prefab asset itself
                        PrefabUtility.ApplyPrefabInstance(behaviour.gameObject, InteractionMode.UserAction);
                    }
                }
            }
            cache.Delete();
        }
#endif
        
        private static string GetGameObjectPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }
        
        private static bool IsInstancedInScene(GameObject obj)
        {
            string path = GetGameObjectPath(obj);
            if (path != "/" + obj.name)
                return true;
            foreach (GameObject rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (rootGameObject == obj.gameObject)
                    return true;
            }
            return false;
        }

        private static bool IsPrefab(GameObject obj) => PrefabUtility.GetPrefabInstanceHandle(obj) != null;

        private void UpdateNetworkerCount()
        {
            if (coreLogger == null || coreLogger.Pool == null)
            {
                networkerCount = 0;
                return;
            }
            networkerCount = coreLogger.Pool.transform.GetComponentsInChildren<NetworkedLogger>().Length;
        }

        private void CreatePool()
        {
            Transform coreLoggerTransform = coreLogger.transform;
            Transform poolObjectTransform = coreLoggerTransform.Find("Pool");
            if(poolObjectTransform == null)
            {
                poolObjectTransform = new GameObject("Pool").transform;
                poolObjectTransform.SetParent(coreLoggerTransform);
            }
            NetworkPool pool = poolObjectTransform.gameObject.GetComponent<NetworkPool>();
            if (pool == null)
                pool = poolObjectTransform.gameObject.AddComponent<NetworkPool>();
            coreLogger.Pool = pool;
            EditorUtility.SetDirty(pool.gameObject);
            EditorUtility.SetDirty(coreLogger.gameObject);
            UpdateNetworkerCount();
        }

        private void CreateNetworkers(int count)
        {
            Transform poolTransform = coreLogger.Pool.transform;
            GameObject[] objectsToDelete = new GameObject[poolTransform.childCount];
            for (int i = 0; i < poolTransform.childCount; i++)
            {
                Transform child = poolTransform.GetChild(i);
                objectsToDelete[i] = child.gameObject;
            }
            foreach (GameObject gameObject in objectsToDelete)
                DestroyImmediate(gameObject);
            for (int i = 0; i < count; i++)
            {
                GameObject networkerObject = new GameObject($"Networker ({i + 1})");
                networkerObject.transform.SetParent(poolTransform);
                NetworkedLogger networkedLogger = networkerObject.AddComponent<NetworkedLogger>();
                networkedLogger.Logger = coreLogger;
                EditorUtility.SetDirty(networkerObject);
            }
            UpdateNetworkerCount();
        }

        private void SanityCheck() => moreThanOne = FindObjectsOfType<CoreLogger>(true).Length > 1;

        private void OnEnable()
        {
            coreLogger = target as CoreLogger;
            if (coreLogger == null)
                throw new Exception("Invalid Inspector");
            LoggerViews = serializedObject.FindProperty("LoggerViews");
            ClearButtons = serializedObject.FindProperty("ClearButtons");
            WarnColor = serializedObject.FindProperty("WarnColor");
            ErrorColor = serializedObject.FindProperty("ErrorColor");
            UsePersistentColors = serializedObject.FindProperty("UsePersistentColors");
            UpdateNetworkerCount();
            SanityCheck();
        }

        public override async void OnInspectorGUI()
        {
            if (!IsInstancedInScene(coreLogger.gameObject))
            {
                GUILayout.Label("Please insert CoreLogger into scene!");
                return;
            }
            if (IsPrefab(coreLogger.gameObject))
            {
                EditorGUILayout.HelpBox("Please unpack prefab completely!", MessageType.Error);
                return;
            }
#if !DISABLE_RELOG_ANALYSIS
            if(generateStage)
            {
                if (!check)
                {
                    GUILayout.Label("Code Generation", EditorStyles.whiteLargeLabel);
                    GUILayout.Label("Stage One: File Detection");
                    EditorGUILayout.Separator();
                    GUILayout.Label(
                        "This stage of code generation will detect all files that reference UnityEditor.Debug.",
                        EditorStyles.miniLabel);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Check"))
                    {
                        files = GetAssemblies();
                        affectedFiles = Analysis.GetAffectedFilesAndLines("Assets", Analysis.DefaultAnalyzer, files).ToArray();
                        check = true;
                    }
                    if (GUILayout.Button("Cancel"))
                        generateStage = false;
                    EditorGUILayout.EndHorizontal();
                }
                else if(affectedFiles.Length > 0)
                {
                    GUILayout.Label("Code Generation", EditorStyles.whiteLargeLabel);
                    GUILayout.Label("Stage Two: File Conversion");
                    EditorGUILayout.Separator();
                    GUILayout.Label(
                        "This stage of code generation will list all target files.\nPlease verify all files are correct in this stage.",
                        EditorStyles.miniLabel);
                    EditorGUILayout.Space();
                    foreach (AffectedFile affectedFile in affectedFiles)
                    {
                        GUILayout.Label(affectedFile.FilePath);
                        EditorGUILayout.Space();
                        GUILayout.Label(affectedFile.LineNumber + " : " + affectedFile.LineText);
                        EditorGUILayout.Separator();
                    }
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button($"Convert {affectedFiles.Length} Script{(affectedFiles.Length > 1 ? "s" : "")}!"))
                    {
                        bool result = EditorUtility.DisplayDialog("ReLog",
                            $"Applying script changes to {affectedFiles.Length} file{(affectedFiles.Length > 1 ? "s" : "")}. This may cause damage to your files. Before continuing, MAKE A BACKUP! Are you sure you'd like to continue?",
                            "Yes", "No");
                        if(result)
                        {
                            await Analysis.ApplyRoslynFixesAsync("Assets", Analysis.DefaultAnalyzer,
                                Analysis.DefaultProvider,
                                files);
                            ApplyEditCache cache = CreateInstance<ApplyEditCache>();
                            cache.Logger = coreLogger;
                            cache.Save();
                            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                        }
                        check = false;
                        generateStage = false;
                    }
                    if (GUILayout.Button("Cancel"))
                    {
                        check = false;
                        generateStage = false;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("No affected files found!");
                    if (GUILayout.Button("OK"))
                    {
                        check = false;
                        generateStage = false;
                    }
                }
            }
#endif
            else if (generateNetworkers)
            {
                GUILayout.Label("Networker Generator", EditorStyles.whiteLargeLabel);
                networkersToCreate = EditorGUILayout.IntField("Networker Count", networkersToCreate);
                EditorGUILayout.BeginHorizontal();
                if(networkersToCreate <= 0)
                    GUILayout.Label("You must have at least 1 networker!", EditorStyles.miniLabel);
                else
                {
                    if (GUILayout.Button("Create!"))
                    {
                        CreateNetworkers(networkersToCreate);
                        generateNetworkers = false;
                    }
                }
                if (GUILayout.Button("Cancel"))
                    generateNetworkers = false;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                if (moreThanOne)
                    EditorGUILayout.HelpBox(
                        "More than one CoreLogger was detected in your scene! This kind of setup is unsupported, please only use one!",
                        MessageType.Warning);
                GUILayout.Label("Properties", EditorStyles.whiteLargeLabel);
                EditorGUILayout.PropertyField(LoggerViews, new GUIContent("Logger Views"));
                EditorGUILayout.PropertyField(WarnColor, new GUIContent("Warning Color"));
                EditorGUILayout.PropertyField(ErrorColor, new GUIContent("Error Color"));
                EditorGUILayout.PropertyField(UsePersistentColors, new GUIContent("Use Persistent Colors"));
                EditorGUILayout.Separator();
                GUILayout.Label("Networking", EditorStyles.whiteLargeLabel);
                if (coreLogger.Pool == null)
                    CreatePool();
                if(coreLogger.Pool != null)
                {
                    GUILayout.Label("Networkers: " + networkerCount);
                    if (GUILayout.Button("Redo Networking"))
                        generateNetworkers = true;
                }
#if !DISABLE_RELOG_ANALYSIS
                EditorGUILayout.Separator();
                GUILayout.Label("Code Generation", EditorStyles.whiteLargeLabel);
                EditorGUILayout.HelpBox(
                    "Generating code will rewrite detected files and may cause irreparable damage. Please make a backup before continuing!",
                    MessageType.Error);
                if (GUILayout.Button("Auto-Generate Logging"))
                    generateStage = true;
#endif
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}