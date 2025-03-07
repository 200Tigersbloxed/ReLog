using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;

namespace ReLog
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LoggerView : UdonSharpBehaviour
    {
        public CoreLogger CoreLogger;
        
        public TMP_Text[] TextObjects;
        public ScrollRect[] Scrolls;
        public TMP_Dropdown[] Dropdowns;

        private int[] lastIndexes;

        public void ApplyDropdownOptions(TMP_Dropdown.OptionData[] options, int index)
        {
            foreach (TMP_Dropdown dropwdown in Dropdowns)
            {
                dropwdown.ClearOptions();
                dropwdown.AddOptions(options);
                dropwdown.value = index;
            }
        }
        
        public void ApplyDropdownIndex(int index)
        {
            for (int i = 0; i < lastIndexes.Length; i++)
            {
                TMP_Dropdown dropdown = Dropdowns[i];
                dropdown.value = index;
                lastIndexes[i] = index;
            }
        }

        public void ScrollToBottom()
        {
            foreach (ScrollRect scrollRect in Scrolls)
                scrollRect.normalizedPosition = Vector2.zero;
        }

        public void SetText(string text)
        {
            foreach (TMP_Text textObject in TextObjects)
                textObject.text = text;
        }
        
        public void _UpdateDropdown()
        {
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
            if (indexToUpdate < 0) return;
            CoreLogger._RefreshLog(indexToUpdate);
        }

        private void Start() => lastIndexes = new int[Dropdowns.Length];

        private void OnEnable() => CoreLogger.ShowPlayerLog();
    }
}