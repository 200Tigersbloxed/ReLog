using System;
using System.Text;
using ReLog.Networking;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using Debug = UnityEngine.Debug;

namespace ReLog
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CoreLogger : UdonSharpBehaviour
    {
        public NetworkPool Pool;
        public LoggerView[] LoggerViews;
        public Color WarnColor = new Color(1f, 0.75f, 0);
        public Color ErrorColor = new Color(0.6f, 0, 0f);
        public bool UsePersistentColors;

        private bool ready;
        private int lastIndex;
        private VRCPlayerApi local;
        private VRCPlayerApi focused;
        private string[] waitingLogs = new string[0];
        private int[] waitingLevels = new int[0];

        public void Log(string message) => SendLog(0, message);
        public void LogWarning(string message) => SendLog(1, message);
        public void LogError(string message) => SendLog(2, message);

        public void _RefreshLog(int index)
        {
            if (index == 0)
            {
                // All
                focused = null;
                ShowAll();
            }
            else
            {
                // Specific Player
                VRCPlayerApi player = Pool.Players[index - 1];
                focused = player;
                ShowPlayerLog(player);
            }
            if(index == lastIndex) return;
            lastIndex = index;
            LoggerViews.ApplyDropdowns(index);
        }

        public void _Clear()
        {
            if (focused == null)
            {
                foreach (NetworkedLogger networkedLogger in Pool.Loggers)
                    networkedLogger.Clear();
                ShowAll();
            }
            else
            {
                NetworkedLogger networkedLogger = Pool.Get(focused);
                if (networkedLogger == null) return;
                networkedLogger.Clear();
                ShowPlayerLog(focused);
            }
        }

        public void ClearEverything()
        {
            foreach (NetworkedLogger networkedLogger in Pool.Loggers)
                networkedLogger.Clear();
            ShowPlayerLog();
        }

        private void SendLog(int level, string message)
        {
            if (!ready)
            {
                Utils.Push(ref waitingLogs, message);
                Utils.Push(ref waitingLevels, level);
                return;
            }
            NetworkedLogger networkedLogger = Pool.Get(local);
            if(networkedLogger == null)
            {
                Debug.LogError("No NetworkedLogger for " + local.displayName);
                return;
            }
            networkedLogger.PushNetworkLog(level, message);
            if(focused == local || focused == null)
                ShowPlayerLog(local);
        }

        private void UpdateDropdown(VRCPlayerApi[] players)
        {
            string last = "";
            if (focused != null)
                last = focused.GetPlayerStringIdentifier();
            int index = 0;
            VRCPlayerApi player = null;
            TMP_Dropdown.OptionData[] options = new TMP_Dropdown.OptionData[players.Length + 1];
            options[0] = new TMP_Dropdown.OptionData("All");
            for (int i = 0; i < players.Length; i++)
            {
                options[i + 1] = new TMP_Dropdown.OptionData(players[i].GetPlayerStringIdentifier());
                if(players[i].GetPlayerStringIdentifier() != last) continue;
                index = i + 1;
                player = players[i];
            }
            LoggerViews.ApplyDropdowns(options, index);
            focused = player;
            ShowPlayerLog(player);
        }

        private void ScrollToBottom() => LoggerViews.ScrollToBottom();

        internal void ShowPlayerLog(VRCPlayerApi player = null)
        {
            if(LoggerViews.IsDisabled()) return;
            if (player == null) player = focused;
            if (focused == null)
            {
                ShowAll();
                return;
            }
            if(focused != player) return;
            NetworkedLogger networkedLogger = Pool.Get(player);
            if(networkedLogger == null)
            {
                Debug.LogError("Could not find NetworkedLogger for player " + player.displayName);
                return;
            }
            StringBuilder text = new StringBuilder();
            if (networkedLogger.logs.Length != networkedLogger.levels.Length ||
                networkedLogger.logs.Length != networkedLogger.logDates.Length) return;
            for (int i = networkedLogger.ClearedIndex; i < networkedLogger.logs.Length; i++)
            {
                int level = networkedLogger.levels[i];
                string log = networkedLogger.logs[i];
                DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(networkedLogger.logDates[i]).UtcDateTime;
                text.Append(Utils.CraftLogString(log, level, dateTime, networkedLogger.Color, player, WarnColor, ErrorColor));
            }
            string finalText = text.ToString();
            LoggerViews.SetText(finalText);
            ScrollToBottom();
        }

        private void ShowAll()
        {
            if(LoggerViews.IsDisabled()) return;
            string text;
            Utils.GetFormattedLogs(Pool.Loggers, WarnColor, ErrorColor, out text);
            LoggerViews.SetText(text);
            ScrollToBottom();
        }

        private void Start()
        {
            local = VRC.SDKBase.Networking.LocalPlayer;
            Pool.Initialize();
            ready = true;
            for (int i = 0; i < waitingLogs.Length; i++)
            {
                string log = waitingLogs[i];
                int level = waitingLevels[i];
                SendLog(level, log);
            }
        }

        private void Update()
        {
            if(Pool._Update())
                UpdateDropdown(Pool.Players);
        }
    }
}