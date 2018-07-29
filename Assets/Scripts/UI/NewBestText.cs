using DoozyUI;
using UnityEngine;

namespace Cytoid.UI
{
    public class NewBestText : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private UIElement uiElement;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            uiElement = GetComponent<UIElement>();
            canvasGroup.alpha = 0;
            uiElement.enabled = false;
            EventKit.Subscribe("new best", Enable);
        }

        private void OnDestroy()
        {
            EventKit.Unsubscribe("new best", Enable);
        }

        private void Enable()
        {
            uiElement.enabled = true;
        }
    }
}