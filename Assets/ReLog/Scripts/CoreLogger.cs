using System;
using ReLog.Networking;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace ReLog
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CoreLogger : UdonSharpBehaviour
    {
        private const string LINE_BREAK = "<br>";

        public NetworkPool Pool;
        public TMP_Text[] TextObjects;
        public ScrollRect[] Scrolls;
        public TMP_Dropdown[] Dropdowns;
        public Button[] ClearButtons;
        public Color WarnColor = new Color(1f, 0.75f, 0);
        public Color ErrorColor = new Color(0.6f, 0, 0f);
        public bool UsePersistentColors;

        private bool ready;
        private VRCPlayerApi local;
        private VRCPlayerApi focused;
        private string[] waitingLogs = new string[0];
        private int[] waitingLevels = new int[0];
        private int[] lastIndexes;
        private int allClear;

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
        }

        public void _Clear()
        {
            if (focused == null) return;
            NetworkedLogger networkedLogger = Pool.Get(focused);
            if(networkedLogger == null) return;
            networkedLogger.Clear();
            ShowPlayerLog(focused);
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
            foreach (TMP_Dropdown dropwdown in Dropdowns)
            {
                dropwdown.ClearOptions();
                dropwdown.AddOptions(options);
                dropwdown.value = index;
            }
            focused = player;
            ShowPlayerLog(player);
        }

        private void SetButtonVisibility(bool state)
        {
            foreach (Button clearButton in ClearButtons)
                clearButton.gameObject.SetActive(state);
        }

        private void ScrollToBottom()
        {
            foreach (ScrollRect scrollRect in Scrolls)
                scrollRect.normalizedPosition = new Vector2(0, 0);
        }

        private string CraftLogString(string log, int level, DateTime dateTime, Color playerColor, VRCPlayerApi player)
        {
            string extraColorTag = "";
            string endExtraColorTag = "";
            switch (level)
            {
                case 1:
                    extraColorTag = $"<color={WarnColor.ConvertToHex()}>";
                    endExtraColorTag = "</color>";
                    break;
                case 2:
                    extraColorTag = $"<color={ErrorColor.ConvertToHex()}>";
                    endExtraColorTag = "</color>";
                    break;
            }
            string text = extraColorTag +
                    $"[{dateTime.Hour}:{dateTime.Minute}:{dateTime.Second}.{dateTime.Millisecond}] [" +
                    endExtraColorTag +
                    $"<color={playerColor.ConvertToHex()}>{player.GetPlayerStringIdentifier()}</color>" + extraColorTag +
                    "] " + log + endExtraColorTag + LINE_BREAK;
            return text;
        }

        internal void ShowPlayerLog(VRCPlayerApi player)
        {
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
            string text = "";
            if (networkedLogger.logs.Length != networkedLogger.levels.Length ||
                networkedLogger.logs.Length != networkedLogger.logDates.Length) return;
            for (int i = networkedLogger.ClearedIndex; i < networkedLogger.logs.Length; i++)
            {
                int level = networkedLogger.levels[i];
                string log = networkedLogger.logs[i];
                DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(networkedLogger.logDates[i]).UtcDateTime;
                text += CraftLogString(log, level, dateTime, networkedLogger.Color, player);
            }
            foreach (TMP_Text textObject in TextObjects)
                textObject.text = text;
            ScrollToBottom();
            SetButtonVisibility(true);
        }

        private void ShowAll()
        {
            string text = "";
            string[] logs;
            int[] levels;
            long[] dates;
            Color[] colors;
            VRCPlayerApi[] players;
            Utils.GetSortedLogs(Pool.Loggers, out logs, out levels, out dates, out colors, out players);
            for (int i = allClear; i < logs.Length; i++)
            {
                string log = logs[i];
                int level = levels[i];
                DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(dates[i]).UtcDateTime;
                Color color = colors[i];
                VRCPlayerApi player = players[i];
                text += CraftLogString(log, level, dateTime, color, player);
            }
            foreach (TMP_Text textObject in TextObjects)
                textObject.text = text;
            ScrollToBottom();
            SetButtonVisibility(false);
        }

        private void Start()
        {
            local = VRC.SDKBase.Networking.LocalPlayer;
            lastIndexes = new int[Dropdowns.Length];
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
            int indexToUpdate = -1;
            for (int i = 0; i < Dropdowns.Length; i++)
            {
                TMP_Dropdown currentDropdown = Dropdowns[i];
                int lastIndex = lastIndexes[i];
                if (currentDropdown.value != lastIndex)
                {
                    indexToUpdate = currentDropdown.value;
                    break;
                }
            }
            if (indexToUpdate > -1)
            {
                for (int i = 0; i < Dropdowns.Length; i++)
                {
                    TMP_Dropdown currentDropdown = Dropdowns[i];
                    currentDropdown.value = indexToUpdate;
                    lastIndexes[i] = indexToUpdate;
                }
                _RefreshLog(indexToUpdate);
            }
        }
    }
}