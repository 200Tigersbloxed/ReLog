using System.Text.RegularExpressions;

namespace ReLog.Serializing
{
    public static class JsonParser
    {
        private static string EscapeJsonString(string input) => input.Replace("\\", "\\\\").Replace("\"", "\\\"")
            .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");

        private static string GetJsonString(string content, int level, long date) =>
            "{\"Log\":\"" + EscapeJsonString(content) + "\",\"Level\":" + level + ",\"Date\":" + date + "}";

        public static string UpdateJson(string[] logs, int[] levels, long[] logDates)
        {
            string[] existingObjs = new string[logs.Length];
            for (int i = 0; i < existingObjs.Length; i++)
            {
                string log = logs[i];
                int l = levels[i];
                long d = logDates[i];
                string obj = GetJsonString(log, l, d);
                existingObjs[i] = obj;
            }
            string final = "[";
            for (int i = 0; i < existingObjs.Length; i++)
            {
                final = final + existingObjs[i];
                if(i >= existingObjs.Length - 1) continue;
                final = final + ",";
            }
            final = final + "]";
            return final;
        }

#if RELOG_GOOD_OPTIMIZED_CODE
        private static string[] ExtractJsonObjects(string json, Regex objectRegex)
        {
            MatchCollection matches = objectRegex.Matches(json);
            string[] objects = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                objects[i] = matches[i].Value;
            }
            return objects;
        }

        public static string[] GetObjects(string json, Regex objectRegex) => ExtractJsonObjects(json, objectRegex);

        private static string ExtractJsonValue(string json, Regex regex)
        {
            Match match = regex.Match(json);
            return match.Success ? match.Groups[1].Value : "";
        }

        public static string GetLogAt(string[] objects, int index, Regex logRegex)
        {
            if (index < 0 || index >= objects.Length) return "";
            return ExtractJsonValue(objects[index], logRegex);
        }

        public static int GetLevelAt(string[] objects, int index, Regex levelRegex)
        {
            if (index < 0 || index >= objects.Length) return -1;
            return int.TryParse(ExtractJsonValue(objects[index], levelRegex), out int level) ? level : -1;
        }

        public static long GetDateAt(string[] objects, int index, Regex dateRegex)
        {
            if (index < 0 || index >= objects.Length) return -1;
            return long.TryParse(ExtractJsonValue(objects[index], dateRegex), out long date) ? date : -1;
        }
#else
        private static string[] ExtractJsonObjects(string json)
        {
            MatchCollection matches = Regex.Matches(json, "\\{[^{}]*\\}");
            string[] objects = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                objects[i] = matches[i].Value;
            }
            return objects;
        }
        
        private static string ExtractJsonValue(string json, string key)
        {
            Match match = Regex.Match(json, $"\"{key}\"\\s*:\\s*\"?(.*?)\"?(,|}})");
            return match.Success ? match.Groups[1].Value : "";
        }

        public static string[] GetObjects(string json) => ExtractJsonObjects(json);
        
        public static string GetLogAt(string[] objects, int index)
        {
            if (index < 0 || index >= objects.Length) return "";
            return ExtractJsonValue(objects[index], "Log");
        }

        public static int GetLevelAt(string[] objects, int index)
        {
            if (index < 0 || index >= objects.Length) return -1;
            return int.Parse(ExtractJsonValue(objects[index], "Level"));
        }

        public static long GetDateAt(string[] objects, int index)
        {
            if (index < 0 || index >= objects.Length) return -1;
            return long.Parse(ExtractJsonValue(objects[index], "Date"));
        }
#endif
    }
}