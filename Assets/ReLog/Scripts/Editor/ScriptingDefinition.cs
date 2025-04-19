using System.Collections.Generic;
using UnityEditor;

namespace ReLog.Editor
{
    [InitializeOnLoad]
    internal class ScriptingDefinition
    {
        private const string RELOG_SD = "RELOG";

        static ScriptingDefinition() => AddScriptingDefineSymbol(RELOG_SD);
        
        private static void AddScriptingDefineSymbol(string define)
        {
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string[] defines;
            PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup, out defines);
            List<string> clone = new List<string>(defines);
            if(!clone.Contains(define))
                clone.Add(define);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, clone.ToArray());
        }
    }
}