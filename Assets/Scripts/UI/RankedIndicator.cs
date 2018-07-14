using System.Collections.Generic;
using System.Linq;
using Cytus2.Controllers;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace Cytoid.UI
{
    public class RankedIndicator : MonoBehaviour
    {
        private void Update()
        {
            gameObject.SetActive(CytoidApplication.CurrentPlay.IsRanked);
        }
    }
}