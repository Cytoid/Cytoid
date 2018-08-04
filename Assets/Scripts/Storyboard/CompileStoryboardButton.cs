using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.Storyboard
{
    public class CompileStoryboardButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            StoryboardController.Instance.CompileStoryboard();
        }
    }
}