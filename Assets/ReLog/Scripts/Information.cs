using ReLog.Networking;
using TMPro;
using UdonSharp;
using UnityEngine;

namespace ReLog
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(TMP_Text))]
    public class Information : UdonSharpBehaviour
    {
        public const string VERSION = "v1.0.0";

        public NetworkPool Pool;
        public int InformationToShow;

        private TMP_Text text;
        private NetworkedLogger localLogger;
        private NetworkedLogger masterLogger;

        private void UpdateNetworkers()
        {
            if(localLogger == null || !localLogger.IsValid(Pool.LocalPlayer))
                localLogger = Pool.Get(Pool.LocalPlayer);
            if (masterLogger == null || !masterLogger.IsValid(Pool.Master))
                masterLogger = Pool.Get(Pool.Master);
        }

        private void Start() => text = GetComponent<TMP_Text>();

        private void Update()
        {
            if(!Pool.IsReady) return;
            switch (InformationToShow)
            {
                case 0:
                    text.text = "Created By: 200Tigersbloxed, Version " + VERSION;
                    break;
                case 1:
                    UpdateNetworkers();
                    string t = "";
                    t += masterLogger != null
                        ? $"Master: <color={masterLogger.Color.ConvertToHex()}>{Pool.Master.GetPlayerStringIdentifier()}</color> "
                        : $"Master: {Pool.Master.GetPlayerStringIdentifier()} ";
                    t += localLogger != null
                        ? $"LocalPlayer: <color={localLogger.Color.ConvertToHex()}>{Pool.LocalPlayer.GetPlayerStringIdentifier()}</color> "
                        : $"LocalPlayer: {Pool.LocalPlayer.GetPlayerStringIdentifier()} ";
                    int playerCount = Pool.Players.Length;
                    int networkerCount = Pool.Loggers.Length;
                    t += playerCount <= networkerCount
                        ? $"Networkers: <color=#00ff00>{playerCount} / {networkerCount}</color>"
                        : $"Networkers: <color=#ff0000>{playerCount} / {networkerCount}</color>";
                    text.text = t;
                    break;
            }
        }
    }
}