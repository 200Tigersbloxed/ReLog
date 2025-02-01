using System.IO;
using UnityEditor;
using UnityEngine;

namespace ReLog.Editor
{
    public class ApplyEditCache : ScriptableObject
    {
        private const string LOCATION = "Assets/ReLog/Temp/cache.asset";
        private const string DIR = "Assets/ReLog/Temp";
        
        public CoreLogger Logger;

        public void Save()
        {
            if (!Directory.Exists(DIR))
                Directory.CreateDirectory(DIR);
            if(File.Exists(LOCATION))
                File.Delete(LOCATION);
            AssetDatabase.CreateAsset(this, LOCATION);
            AssetDatabase.SaveAssets();
        }

        public void Delete()
        {
            if(!File.Exists(LOCATION)) return;
            AssetDatabase.DeleteAsset(LOCATION);
        }

        public static ApplyEditCache Load()
        {
            if(!File.Exists(LOCATION)) return null;
            return AssetDatabase.LoadAssetAtPath<ApplyEditCache>(LOCATION);
        }
    }
}