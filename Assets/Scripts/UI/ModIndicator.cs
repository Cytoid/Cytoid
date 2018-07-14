using System;
using Cytus2.Models;
using UnityEngine;

namespace Cytoid.UI
{
    public class ModIndicator : MonoBehaviour
    {
        public string Mod;

        private void Start()
        {
            var mod = (Mod) Enum.Parse(typeof(Mod), Mod);
            gameObject.SetActive(CytoidApplication.CurrentPlay.Mods.Contains(mod));
        }
    }
}