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
    
    private float lastScore;
    private Sequence lastSequence;
    
    protected void Awake()
    {
        text.text = "000000";
    }

    protected void LateUpdate()
    {
        if (game.IsLoaded && game.State.IsStarted)
        {
            if (game.State.Score != lastScore)
            {
                lastSequence?.Kill();
                transform.localScale = new Vector3((punchScale + 1) / 2f, punchScale, 1);
                lastSequence = DOTween.Sequence()
                    .Append(transform.DOScale(1, punchDuration).SetEase(ease));
            }
            lastScore = game.State.Score;
            text.text = Mathf.FloorToInt(lastScore).ToString("D6");
        }
    }
}