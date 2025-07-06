using System;
using System.Threading;
using DG.Tweening;
using Ink.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class BadgeDisplay : InteractableMonoBehavior
{

    [GetComponent] public Image image;
    [GetComponent] public GraphicRaycaster graphicRaycaster;
    public bool interactable = true;
    
    private Badge badge;
    private CancellationTokenSource tokenSource;

    public void Awake()
    {
        Clear();
    }

    public void Clear()
    {
        badge = null;
        tokenSource?.Cancel();
        tokenSource = null;
        image.DOKill();
        image.sprite = null;
        image.SetAlpha(0.7f);
        graphicRaycaster.enabled = false;
    }

    public void SetModel(Badge badge)
    {
        this.badge = badge;
        LoadImage();
        graphicRaycaster.enabled = interactable;
        if (interactable) onPointerClick.SetListener(_ =>
        {
            DialogueOverlay.CurrentBadge = badge;
            var story = new Story(Resources.Load<TextAsset>("Stories/Badge").text);
            DialogueOverlay.Show(story);
        });
    }

    public async void LoadImage()
    {
        tokenSource?.Cancel();
        tokenSource = new CancellationTokenSource();
        image.DOKill();
        image.color = Color.black;
        image.SetAlpha(1);

        Sprite sprite;
        try
        {
            var path = badge.GetImageUrl();
            sprite = await Context.AssetMemory.LoadAsset<Sprite>(path, AssetTag.Badge, tokenSource.Token);
        }
        catch
        {
            return;
        }
        image.sprite = sprite;
        image.DOColor(Color.white, 0.2f);
    }

}
