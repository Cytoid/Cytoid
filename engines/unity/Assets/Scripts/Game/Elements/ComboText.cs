using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ComboText : MonoBehaviour
{
    public Text text;
    public Game game;
    public float punchScale = 1.15f;
    public float punchDuration = 0.08f;
    public float fadeDuration = 0.1f;
    public Ease ease = Ease.OutSine;

    private int lastCombo;
    private Sequence lastSequence;
    private bool exited;

    private void OnValidate()
    {
        this.AutoFill(ref text);
    }

    protected void Awake()
    {
        this.AutoFill(ref text);
        text.text = "";
        text.color = text.color.WithAlpha(0);
        game.onGameLoaded.AddListener(_ => OnGameLoaded());
        game.onGameBeforeExit.AddListener(_ => OnGameBeforeExit());
    }

    protected void OnGameLoaded()
    {
    }

    protected void LateUpdate()
    {
        if (!exited && game.IsLoaded && game.State.IsStarted)
        {
            if (game.State.Mode == GameMode.Calibration) return;
            var combo = game.TierPlaySession?.Combo ?? game.State.Combo;
            if (combo != lastCombo)
            {
                if (combo > 0)
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
            lastCombo = combo;
            text.text = lastCombo + "x";
        }
    }

    public void OnGameBeforeExit()
    {
        exited = true;
        text.DOFade(0, fadeDuration).SetEase(ease);
    }
}
