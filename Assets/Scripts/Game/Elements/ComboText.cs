using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ComboText : MonoBehaviour
{
    [GetComponent] public Text text;
    public Game game;
    public float punchScale = 1.15f;
    public float punchDuration = 0.08f;
    public float fadeDuration = 0.1f;
    public Ease ease = Ease.OutSine;

    private int lastCombo;
    private Sequence lastSequence;
    
    protected void Awake()
    {
        text.text = "";
        text.color = text.color.WithAlpha(0);
        game.onGameReadyToExit.AddListener(_ => OnGameReadyToExit());
    }

    protected void LateUpdate()
    {
        if (game.IsLoaded && game.State.IsStarted)
        {
            if (game.Config.IsCalibration) return;
            
            if (game.State.Combo != lastCombo)
            {
                if (game.State.Combo > 0)
                {
                    lastSequence?.Kill();
                    transform.localScale = new Vector3((punchScale + 1) / 2f, punchScale, 1);
                    lastSequence = DOTween.Sequence()
                        .Append(text.DOFade(1, fadeDuration).SetEase(ease))
                        .Append(transform.DOScale(1, punchDuration).SetEase(ease));
                }
                else
                {
                    text.DOFade(0, fadeDuration).SetEase(ease);
                }
            }
            lastCombo = game.State.Combo;
            text.text = lastCombo + "x";
        }
    }

    public void OnGameReadyToExit()
    {
        text.DOFade(0, fadeDuration).SetEase(ease);
    }
}