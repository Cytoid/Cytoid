using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class RankingsViewMoreButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            Application.OpenURL("https://cytoid.io/browse/" + CytoidApplication.CurrentLevel.id + "?rankings=" +
                CytoidApplication.CurrentChartType);
        }
    }
}