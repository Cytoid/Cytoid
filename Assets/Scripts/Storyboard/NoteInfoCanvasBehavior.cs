using Cytus2.Controllers;
using UnityEngine;

namespace Cytoid.Storyboard
{
    public class NoteInfoCanvasBehavior : MonoBehaviour
    {
        private CanvasGroup canvas;

        private void Awake()
        {
            canvas = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            canvas.alpha = ((StoryboardGame) StoryboardGame.Instance).HideUi ? 0 : 1;
        }
    }
}