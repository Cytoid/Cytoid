using Cytus2.Controllers;
using UnityEngine;

namespace Cytoid.Storyboard
{
    public class NoteInfo : MonoBehaviour
    {
        private CanvasGroup canvas;
        private SpriteRenderer parent;

        private void Awake()
        {
            canvas = GetComponent<CanvasGroup>();
            parent = transform.parent.GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (Game.Instance is StoryboardGame)
            {
                canvas.alpha = ((StoryboardGame) StoryboardGame.Instance).HideUi ? 0 : (parent.enabled ? 1 : 0);
            }
        }
    }
}