using System;
using System.Text;
using ReLog.Networking;
using TMPro;
using UnityEngine;
using VRC.SDKBase;

namespace ReLog
{
    internal static class Utils
    {
        private const string LINE_BREAK = "<br>";
        
        public static void Push(ref string[] arr, string item)
        {
            string[] newArr = new string[arr.Length + 1];
            Array.Copy(arr, newArr, arr.Length);
            newArr[newArr.Length - 1] = item;
            arr = newArr;
        }
        
        public static void Push(ref int[] arr, int item)
        {
            int[] newArr = new int[arr.Length + 1];
            Array.Copy(arr, newArr, arr.Length);
            newArr[newArr.Length - 1] = item;
            arr = newArr;
        }
        
        public static void Push(ref long[] arr, long item)
        {
            long[] newArr = new long[arr.Length + 1];
            Array.Copy(arr, newArr, arr.Length);
            newArr[newArr.Length - 1] = item;
            arr = newArr;
        }
        
        public static void Push(ref bool[] arr, bool item)
        {
            bool[] newArr = new bool[arr.Length + 1];
            Array.Copy(arr, newArr, arr.Length);
            newArr[newArr.Length - 1] = item;
            arr = newArr;
        }
        
        public static void Push(ref NetworkedLogger[] arr, NetworkedLogger item)
        {
            NetworkedLogger[] newArr = new NetworkedLogger[arr.Length + 1];
            Array.Copy(arr, newArr, arr.Length);
            newArr[newArr.Length - 1] = item;
            arr = newArr;
        }
        
        public static string ConvertToHex(this Color color)
        {
            int red = (int)Math.Round(color.r * 255);
            int green = (int)Math.Round(color.g * 255);
            int blue = (int)Math.Round(color.b * 255);
            int alpha = (int)Math.Round(color.a * 255);
            return $"#{red:X2}{green:X2}{blue:X2}{alpha:X2}";
        }

        public static bool IsDisabled(this LoggerView[] loggerViews)
        {
            bool v = true;
            foreach (LoggerView disabledCheck in loggerViews)
            {
                if(!disabledCheck.gameObject.activeInHierarchy) continue;
                v = false;
                break;
            }
            return v;
        }

        public static void ApplyDropdowns(this LoggerView[] loggerViews, TMP_Dropdown.OptionData[] options, int index)
        {
            foreach (LoggerView loggerView in loggerViews)
                loggerView.ApplyDropdownOptions(options, index);
        }
        
        public static void ApplyDropdowns(this LoggerView[] loggerViews,int index)
        {
            foreach (LoggerView loggerView in loggerViews)
                loggerView.ApplyDropdownIndex(index);
        }
        
        public static void ScrollToBottom(this LoggerView[] loggerViews)
        {
            foreach (LoggerView loggerView in loggerViews)
                loggerView.ScrollToBottom();
        }
        
        public static void SetText(this LoggerView[] loggerViews, string text)
        {
            foreach (LoggerView loggerView in loggerViews)
                loggerView.SetText(text);
        }
        
        public static string CraftLogString(string log, int level, DateTime dateTime, Color playerColor, VRCPlayerApi player, Color warnColor, Color errorColor)
        {
            string extraColorTag = "";
            string endExtraColorTag = "";
            switch (level)
            {
                case 1:
                    extraColorTag = $"<color={warnColor.ConvertToHex()}>";
                    endExtraColorTag = "</color>";
                    break;
                case 2:
                    extraColorTag = $"<color={errorColor.ConvertToHex()}>";
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

        public static string GetPlayerStringIdentifier(this VRCPlayerApi player) =>
            $"{player.displayName} ({player.playerId})";
        
        public static void GetFormattedLogs(NetworkedLogger[] loggers, Color warnColor, Color errorColor, out string text)
        {
            StringBuilder finalLogText = new StringBuilder();
            int totalLogs = 0;
            int[] pointers = new int[loggers.Length];
            for (int i = 0; i < loggers.Length; i++)
            {
                pointers[i] = loggers[i].ClearedIndex;
                totalLogs += loggers[i].logs.Length - loggers[i].ClearedIndex;
            }
            while (totalLogs > 0)
            {
                long minTimestamp = long.MaxValue;
                int minIndex = -1;
                for (int i = 0; i < loggers.Length; i++)
                {
                    if (pointers[i] < loggers[i].logs.Length)
                    {
                        long timestamp = loggers[i].logDates[pointers[i]];
                        if (timestamp < minTimestamp)
                        {
                            minTimestamp = timestamp;
                            minIndex = i;
                        }
                    }
                }
                if (minIndex == -1) break;
                NetworkedLogger selectedLogger = loggers[minIndex];
                int selectedIndex = pointers[minIndex];
                // This shouldn't happen, but just in case...
                if (selectedIndex < selectedLogger.logs.Length)
                {
                    finalLogText.Append(CraftLogString(selectedLogger.logs[selectedIndex],
                        selectedLogger.levels[selectedIndex],
                        DateTimeOffset.FromUnixTimeMilliseconds(selectedLogger.logDates[selectedIndex]).UtcDateTime,
                        selectedLogger.Color, selectedLogger.GetOwner(), warnColor, errorColor));
                }
                pointers[minIndex]++;
                totalLogs--;
            }
            text = finalLogText.ToString();
        }
    }
}