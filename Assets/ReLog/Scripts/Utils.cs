using System;
using System.Text;
using ReLog.Networking;
using UnityEngine;
using VRC.SDKBase;

namespace ReLog
{
    internal static class Utils
    {
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

        public static string GetPlayerStringIdentifier(this VRCPlayerApi player) =>
            $"{player.displayName} ({player.playerId})";
        
        public static void GetSortedLogs(NetworkedLogger[] loggers, out string[] sortedLogs, out int[] sortedLevels, out long[] sortedLogDates, out Color[] sortedColors, out VRCPlayerApi[] players, bool ignoreIndex = false)
        {
            int totalLogs = 0;
            foreach (var logger in loggers)
            {
                if(logger.logs.Length != logger.levels.Length || logger.logs.Length != logger.logDates.Length) continue;
                totalLogs += logger.logs.Length - (ignoreIndex ? 0 : logger.ClearedIndex);
            }
            string[] allLogs = new string[totalLogs];
            int[] allLevels = new int[totalLogs];
            long[] allLogDates = new long[totalLogs];
            Color[] allColors = new Color[totalLogs];
            VRCPlayerApi[] allPlayers = new VRCPlayerApi[totalLogs];
            int index = 0;
            foreach (var logger in loggers)
            {
                int count = logger.logs.Length;
                for (int i = ignoreIndex ? 0 : logger.ClearedIndex; i < count; i++)
                {
                    allLogs[index] = logger.logs[i];
                    allLevels[index] = logger.levels[i];
                    allLogDates[index] = logger.logDates[i];
                    allColors[index] = logger.Color;
                    allPlayers[index] = logger.GetOwner();
                    index++;
                }
            }
            for (int i = 0; i < totalLogs - 1; i++)
            {
                for (int j = 0; j < totalLogs - 1 - i; j++)
                {
                    if (allLogDates[j] > allLogDates[j + 1])
                    {
                        Swap(ref allLogDates[j], ref allLogDates[j + 1]);
                        Swap(ref allLogs[j], ref allLogs[j + 1]);
                        Swap(ref allLevels[j], ref allLevels[j + 1]);
                        Swap(ref allColors[j], ref allColors[j + 1]);
                        Swap(ref allPlayers[j], ref allPlayers[j + 1]);
                    }
                }
            }
            sortedLogs = allLogs;
            sortedLevels = allLevels;
            sortedLogDates = allLogDates;
            sortedColors = allColors;
            players = allPlayers;
        }

        private static void Swap(ref long a, ref long b)
        {
            long temp = a;
            a = b;
            b = temp;
        }

        private static void Swap(ref string a, ref string b)
        {
            string temp = a;
            a = b;
            b = temp;
        }

        private static void Swap(ref int a, ref int b)
        {
            int temp = a;
            a = b;
            b = temp;
        }

        private static void Swap(ref Color a, ref Color b)
        {
            Color temp = a;
            a = b;
            b = temp;
        }
        
        private static void Swap(ref VRCPlayerApi a, ref VRCPlayerApi b)
        {
            VRCPlayerApi temp = a;
            a = b;
            b = temp;
        }
    }
}