using Cytus2.Controllers;
using UnityEngine;

namespace Cytoid.Storyboard
{
    public class NoteInfo : MonoBehaviour
    {
        private CanvasGroup canvas;

        private void Awake()
        {
            canvas = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            if (Game.Instance is StoryboardGame)
            {
                canvas.alpha = ((StoryboardGame) StoryboardGame.Instance).HideUi ? 0 : 1;
            }
        }
    }
}