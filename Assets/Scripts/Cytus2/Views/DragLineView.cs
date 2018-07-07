using System.Collections;
using System.Linq;
using Cytus2.Controllers;
using Cytus2.Models;
using UnityEngine;

namespace Cytus2.Views
{
    public class DragLineView : MonoBehaviour
    {
        public ChartNote FromNote;
        public ChartNote ToNote;

        public float IntroRatio;
        public float OutroRatio;

        public float Length;

        private Game game;
        private SpriteRenderer spriteRenderer;
        private GameNote gameNote;

        private void Awake()
        {
            game = Game.Instance;
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            spriteRenderer.material.SetFloat("_End", 0.0f);
            spriteRenderer.material.SetFloat("_Start", 0.0f);
            StartCoroutine(Init());
        }

        private IEnumerator Init()
        {
            yield return null;
            Length = Vector3.Distance(FromNote.position, ToNote.position);
            spriteRenderer.material.mainTextureScale = new Vector2(1.0f, Length / 0.16f);
            gameObject.transform.position = FromNote.position;
            gameObject.transform.eulerAngles = new Vector3(0, 0, -FromNote.rotation);
            gameObject.transform.localScale = new Vector3(1.0f, Length / 0.16f);
            spriteRenderer.sortingOrder = FromNote.id;
        }

        private void Update()
        {
            if (game is StoryboardGame)
            {
                spriteRenderer.enabled = OutroRatio < 1;
            }
            else
            {
                if (OutroRatio >= 1)
                {
                    Destroy(gameObject);
                }
            }

            if (!SimpleVisualOptions.Instance.OpaqueDragLine)
            {
                if (gameNote == null && game.GameNotes.ContainsKey(ToNote.id))
                {
                    gameNote = game.GameNotes[ToNote.id];
                }

                if (gameNote != null)
                {
                    var fill = ((SimpleNoteView) gameNote.View).Fill;
                    spriteRenderer.color = spriteRenderer.color.WithAlpha(fill.enabled ? fill.color.a : 0);
                }
            }

            var time = game.Time;
            IntroRatio = (ToNote.intro_time - time - 0.133f) /
                         (ToNote.intro_time - FromNote.nextdraglinestarttime - 0.133f);
            OutroRatio = (time - FromNote.start_time) / (ToNote.start_time - FromNote.start_time);

            if (IntroRatio > 0 && IntroRatio < 1)
            {
                spriteRenderer.material.SetFloat("_End", 1.0f - IntroRatio);
            }
            else if (IntroRatio <= 0)
            {
                spriteRenderer.material.SetFloat("_End", 1.0f);
            }
            else
            {
                spriteRenderer.material.SetFloat("_End", 0.0f);
            }

            if (OutroRatio > 0 && OutroRatio < 1)
            {
                spriteRenderer.material.SetFloat("_Start", OutroRatio);
            }
            else if (OutroRatio <= 0)
            {
                spriteRenderer.material.SetFloat("_Start", 0.0f);
            }
            else
            {
                spriteRenderer.material.SetFloat("_Start", 1.0f);
            }
        }
    }
}