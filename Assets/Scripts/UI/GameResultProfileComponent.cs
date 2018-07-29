using UnityEngine;

namespace Cytoid.UI
{
    public class GameResultProfileComponent : MonoBehaviour
    {
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
        }

        private void Start()
        {
            if (OnlinePlayer.Authenticated)
            {
                canvasGroup.alpha = 1;
            }
        }
    }
}