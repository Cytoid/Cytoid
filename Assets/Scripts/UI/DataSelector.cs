using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class DataSelector : MonoBehaviour
    {
        public string Key;
        public List<string> Selections;

        private int currentSelection;

        private void Awake()
        {
            var buttons = GetComponentsInChildren<Button>();
            buttons[0].onClick.AddListener(Previous);
            buttons[1].onClick.AddListener(Next);
            var current = PlayerPrefs.GetString(Key);
            for (var index = 0; index < Selections.Count; index++)
            {
                var selection = Selections[index];
                if (selection == current)
                {
                    currentSelection = index;
                    break;
                }
            }

            Select();
        }

        private void Previous()
        {
            currentSelection--;
            if (currentSelection < 0)
            {
                currentSelection = Selections.Count - 1;
            }

            Select();
        }

        private void Next()
        {
            currentSelection++;
            if (currentSelection >= Selections.Count)
            {
                currentSelection = 0;
            }

            Select();
        }

        private void Select()
        {
            PlayerPrefs.SetString(Key, Selections[currentSelection]);
            GetComponentInChildren<Text>().text = Selections[currentSelection];
        }
    }
}