using System;
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
        
        [UdonSynced] [HideInInspector] public string[] logs = new string[0];
        [UdonSynced] [HideInInspector] public int[] levels = new int[0];
        [UdonSynced] [HideInInspector] public long[] logDates = new long[0];
        [UdonSynced] [HideInInspector] public Color Color;

        public int ClearedIndex;

        private VRCPlayerApi local;

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

        internal void PushNetworkLog(int level, string content)
        {
            long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Utils.Push(ref logs, content);
            Utils.Push(ref levels, level);
            Utils.Push(ref logDates, time);
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
