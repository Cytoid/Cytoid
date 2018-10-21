using Cytus2.Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.Storyboard
{
    public class ReloadStoryboardButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            StartCoroutine(StoryboardController.Instance.Reload());
        }

    }
}