using System;
using UnityEngine;
using UnityEngine.UI;

public class NoteOffsetWidget : MonoBehaviour
{
    [GetComponent] public CanvasGroup canvasGroup;
    [GetComponent] public TransitionElement transitionElement;

    public Sprite compressSprite;
    public Sprite expandSprite;
    
    public InteractableMonoBehavior collapseButton;
    public Image collapseIcon;
    public TransitionElement contentHolder;
    public RadioGroup autoplayRadioGroup;
    public InteractableMonoBehavior decreaseMoreButton;
    public InteractableMonoBehavior decreaseButton;
    public InteractableMonoBehavior increaseButton;
    public InteractableMonoBehavior increaseMoreButton;
    public Text offsetText;

    public Game game;

    private float offset;
    private bool isCollapsed;

    private void Awake()
    {
        canvasGroup.alpha = 0;
        collapseButton.onPointerClick.AddListener(_ =>
        {
            if (!isCollapsed)
            {
                isCollapsed = true;
                collapseIcon.sprite = expandSprite;
                contentHolder.Leave();
            }
            else
            {
                isCollapsed = false;
                collapseIcon.sprite = compressSprite;
                contentHolder.Enter();
            }
            
        });
        autoplayRadioGroup.onSelect.AddListener(it =>
        {
            var value = bool.Parse(it);
            if (value) game.State.Mods.Add(Mod.Auto);
            else game.State.Mods.Remove(Mod.Auto);
        });
        decreaseMoreButton.onPointerClick.AddListener(it => ChangeGameNoteOffset(-0.05f));
        decreaseButton.onPointerClick.AddListener(it => ChangeGameNoteOffset(-0.01f));
        increaseButton.onPointerClick.AddListener(it => ChangeGameNoteOffset(+0.01f));
        increaseMoreButton.onPointerClick.AddListener(it => ChangeGameNoteOffset(+0.05f));
        game.onGameLoaded.AddListener(it =>
        {
            if (game.State.Mode != GameMode.Calibration)
            {
                Destroy(gameObject);
            }
            else
            {
                offset = Context.LocalPlayer.GetLevelNoteOffset(game.Level.Id);
                UpdateOffsetText();
                canvasGroup.alpha = 1;
                transitionElement.UseCurrentStateAsDefault();
                transitionElement.Enter();
                contentHolder.UseCurrentStateAsDefault();
                contentHolder.Enter();
                game.onGameCompleted.AddListener(_ => transitionElement.Leave());
                game.onGameAborted.AddListener(_ => transitionElement.Leave());
                game.onGameRetried.AddListener(_ => transitionElement.Leave());
                autoplayRadioGroup.Select("true", false);
                game.State.Mods.Add(Mod.Auto);
                transform.RebuildLayout();
            }
        });
    }

    private void ChangeGameNoteOffset(float dOffset)
    {
        offset += dOffset;
        UpdateOffsetText();
        game.Config.ChartOffset += dOffset;
        game.ResynchronizeChartOnNextFrame = true;
        // Save config
        Context.LocalPlayer.SetLevelNoteOffset(game.Level.Id, (float) Math.Round(offset, 2));
    }

    private void UpdateOffsetText()
    {
        offsetText.text = $"{(offset >= 0 ? "+" : "")}{offset:0.00}s";
    }
}