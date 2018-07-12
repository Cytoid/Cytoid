using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace Cytoid.UI
{
    public class ModButton : MonoBehaviour
    {
        public string Mod;
        public List<string> IncompatiableMods = new List<string>();

        private Button button;

        private void Awake()
        {
            button = GetComponentInChildren<Button>();
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            var array = PlayerPrefsExt.GetStringArray("mods", new string[0]);
            var list = array.ToList();
            if (list.Contains(Mod))
            {
                list.Remove(Mod);
            }
            else
            {
                list.Add(Mod);
                IncompatiableMods.ForEach(it => list.Remove(it));
            }

            PlayerPrefsExt.SetStringArray("mods", list.ToArray());
        }

        private void Update()
        {
            var colors = button.colors;
            colors.colorMultiplier = PlayerPrefsExt.GetStringArray("mods", new string[0]).Contains(Mod) ? 1f : 0.5f;
            button.colors = colors;
        }
    }
    
}