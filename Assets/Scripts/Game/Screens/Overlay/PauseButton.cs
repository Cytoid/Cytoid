using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class PauseButton : InteractableMonoBehavior
{
    public Game game;

    [GetComponent] public TransitionElement transitionElement;
    [FormerlySerializedAs("interactableMonoBehavior")] [GetComponent] public InteractableMonoBehavior interactableMonoBehavior;
    public CanvasGroup canvasGroup;

    public float normalOpacity = 0.3f;
    public float highlightedOpacity = 0.7f;
    public float animationDuration = 0.4f;
    
    private bool highlighted = false;
    private float willUnhighlightTimestamp = 0;
    
    protected void Awake()
    {
        canvasGroup.alpha = normalOpacity;
        canvasGroup.interactable = false;
        game.onGameLoaded.AddListener(game =>
        {
            if (game.State.Mode != GameMode.Tier)
            {
                canvasGroup.interactable = true;
                game.onGameCompleted.AddListener(_ =>
                {
                    transitionElement.leaveTo = Transition.Default;
                    transitionElement.Leave();
                });
                game.onGamePaused.AddListener(_ =>
                {
                    transitionElement.leaveTo = Transition.Default;
                    transitionElement.Leave();
                });
                game.onGameUnpaused.AddListener(_ =>
                {
                    canvasGroup.alpha = normalOpacity;
                    transitionElement.enterFrom = Transition.Default;
                    transitionElement.Enter();
                });
                interactableMonoBehavior.onPointerClick.AddListener(_ =>
                {
                    if (!highlighted)
                    {
                        Highlight();
                    }
                    else
                    {
                        Unhighlight();
                        game.Pause();
                    }
                });
            }
            else
            {
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
            }
        });
    }

    protected void Update()
    {
        if (highlighted && Time.realtimeSinceStartup > willUnhighlightTimestamp)
        {
            Unhighlight();
        }
    }

    private void Highlight()
    {
        highlighted = true;
        canvasGroup.DOFade(highlightedOpacity, animationDuration);
        willUnhighlightTimestamp = Time.realtimeSinceStartup + 2;
    }

    private void Unhighlight()
    {
        highlighted = false;
        canvasGroup.DOFade(normalOpacity, animationDuration);
    }
}