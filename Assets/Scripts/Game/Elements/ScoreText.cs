using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ScoreText : MonoBehaviour
{
    [GetComponent] public Text text;
    public Game game;
    public float punchScale = 1.2f;
    public float punchDuration = 0.2f;
    public Ease ease = Ease.OutCubic;

    private double lastScore;
    private Sequence lastSequence;

    protected void Awake()
    {
        text.text = "";
    }

    protected void LateUpdate()
    {
        if (game.IsLoaded)
        {
            if (game.Config.IsCalibration)
            {
                text.text = "";
                return;
            }
            if (game.State.IsStarted)
            {
                if (game.State.Score != lastScore)
                {
                    lastSequence?.Kill();
                    transform.localScale = new Vector3((punchScale + 1) / 2f, punchScale, 1);
                    lastSequence = DOTween.Sequence()
                        .Append(transform.DOScale(1, punchDuration).SetEase(ease));
                }

                lastScore = game.State.Score;
                text.text = ((int) lastScore).ToString("D6");
            }
            else
            {
                text.text = "000000";
            }
        }
    }
}