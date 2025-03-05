using System;
using ReLog.Serializing;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;
using Random = System.Random;

namespace ReLog.Networking
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class NetworkedLogger : UdonSharpBehaviour
    {
        public CoreLogger Logger;
        
        [HideInInspector] public string[] logs = new string[0];
        [HideInInspector] public int[] levels = new int[0];
        [HideInInspector] public long[] logDates = new long[0];
        [UdonSynced] [HideInInspector] public string lastJson = "[]";
        [UdonSynced] [HideInInspector] public Color Color;

        public int ClearedIndex;

        private VRCPlayerApi local;
        
        // TODO: Use Compiled Regex
        // For some reason I cannot get this to work at runtime.
        // UdonSharp says this is supported in the class list, and it even compiles, but it just throws Exceptions anyways
#if RELOG_GOOD_OPTIMIZED_CODE
        private readonly Regex ObjectRegex = new Regex("\\{[^{}]*\\}", RegexOptions.Compiled);
        private readonly Regex LogRegex = new Regex("\"Log\"\\s*:\\s*\"(.*?)\"", RegexOptions.Compiled);
        private readonly Regex LevelRegex = new Regex("\"Level\"\\s*:\\s*(\\d+)", RegexOptions.Compiled);
        private readonly Regex DateRegex = new Regex("\"Date\"\\s*:\\s*(\\d+)", RegexOptions.Compiled);
#endif

        public VRCPlayerApi GetOwner() => VRC.SDKBase.Networking.GetOwner(gameObject);

        public bool IsValid(VRCPlayerApi player) => GetOwner() == player && logs.Length > 0;

        public void Clear()
        {
            if(logs == null || logs.Length <= 0)
            {
                ClearedIndex = 0;
                return;
            }
            ClearedIndex = logs.Length;
        }

        private void Deserialize()
        {
#if RELOG_GOOD_OPTIMIZED_CODE
            string[] objects = JsonParser.GetObjects(lastJson, ObjectRegex);
#else
            string[] objects = JsonParser.GetObjects(lastJson);
#endif
            int length = objects.Length;
            logs = new string[length];
            levels = new int[length];
            logDates = new long[length];
            for (int i = 0; i < length; i++)
            {
#if RELOG_GOOD_OPTIMIZED_CODE
                string log = JsonParser.GetLogAt(objects, i, LogRegex);
                int level = JsonParser.GetLevelAt(objects, i, LevelRegex);
                long date = JsonParser.GetDateAt(objects, i, DateRegex);
#else
                string log = JsonParser.GetLogAt(objects, i);
                int level = JsonParser.GetLevelAt(objects, i);
                long date = JsonParser.GetDateAt(objects, i);
#endif
                logs[i] = log;
                levels[i] = level;
                logDates[i] = date;
            }
        }
        
        internal void PushNetworkLog(int level, string content)
        {
            long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Utils.Push(ref logs, content);
            Utils.Push(ref levels, level);
            Utils.Push(ref logDates, time);
            lastJson = JsonParser.UpdateJson(logs, levels, logDates);
            RequestSerialization();
        }

        internal void GenerateColor()
        {
            Random random;
            if (Logger.UsePersistentColors)
            {
                VRCPlayerApi owner = GetOwner();
                random = owner == null ? new Random() : new Random(owner.displayName.GetHashCode());
            }
            else
                random = new Random();
            Color = new Color((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble(), 1);
        }

        public override void OnDeserialization(DeserializationResult result)
        {
            Deserialize();
            if(logs.Length != levels.Length || logs.Length != logDates.Length) return;
            Logger.ShowPlayerLog(GetOwner());
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            ClearedIndex = 0;
            if(local != GetOwner()) return;
            Debug.Log(local.displayName + " Obtained " + gameObject.name);
            logs = new string[0];
            levels = new int[0];
            logDates = new long[0];
            lastJson = "[]";
            GenerateColor();
            RequestSerialization();
        }

        internal void Initialize()
        {
            local = VRC.SDKBase.Networking.LocalPlayer;
            GenerateColor();
        }
    }
}
