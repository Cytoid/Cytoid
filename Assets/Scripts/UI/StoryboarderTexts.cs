using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class StoryboarderTexts : MonoBehaviour
    {
        private Level level;
        private void Update()
        {
            if (CytoidApplication.CurrentLevel != level)
            {
                level = CytoidApplication.CurrentLevel;
                if (level == null || level.storyboarder == null)
                {
                    GetComponentInChildren<CanvasGroup>().alpha = 0;
                }
                else
                {
                    GetComponentInChildren<CanvasGroup>().alpha = 1;
                    var storyboarder = CytoidApplication.CurrentLevel.storyboarder;
                    GetComponentsInChildren<Text>()[1].text = storyboarder;
                }
            }
        }
    }
}