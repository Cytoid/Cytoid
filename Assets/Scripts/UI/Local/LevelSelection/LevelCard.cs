using System;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelCard : InteractableMonoBehavior
{

    public Image cover;
    public CanvasGroup difficultyBallGroup;
    
    public Text artist;
    public Text title;
    public Text titleLocalized;

    private Level level;
    
    public GameObject difficultyBallPrefab;

    public void ScrollCellContent(object levelObject)
    {
        SetModel((Level) levelObject);
    }

    public void SetModel(Level level)
    {
        this.level = level;
        
        artist.text = level.Meta.artist;
        title.text = level.Meta.title;
        titleLocalized.text = level.Meta.artist_localized;
        titleLocalized.gameObject.SetActive(!string.IsNullOrEmpty(level.Meta.artist_localized));

        foreach (Transform child in difficultyBallGroup.transform)
            Destroy(child.gameObject);
        foreach (var chart in level.Meta.charts)
        {
            var difficultyBall = Instantiate(difficultyBallPrefab, difficultyBallGroup.transform)
                .GetComponent<DifficultyBall>();
            difficultyBall.SetModel(Difficulty.Parse(chart.type), chart.difficulty);
        }

        GetComponentInChildren<VerticalLayoutGroup>().transform.RebuildLayout();

        LoadCover();
    }

    public async void LoadCover()
    {
        var path = "file://" + level.Path + ".thumbnail";

        var sprite = await Context.SpriteCache.GetSprite(path);
        cover.sprite = sprite;
        cover.GetComponent<AspectRatioFitter>().aspectRatio = sprite.texture.width * 1.0f / sprite.texture.height;
    }

    public override async void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        await UniTask.Delay(20);

        if (IsPointerDown)
        {
            transform.DOScale(0.95f, Constants.TweenDuration).SetEase(Ease.OutCubic);
            cover.DOFade(1.0f, Constants.TweenDuration).SetEase(Ease.OutCubic);
            cover.rectTransform.DOScale(1.02f, 0.2f).SetEase(Ease.OutCubic);
        }
    }
    
    public override async void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        await UniTask.WaitUntil(() => !DOTween.IsTweening(transform));
        
        transform.DOScale(1f, Constants.TweenDuration).SetEase(Ease.OutCubic);
        cover.DOFade(0.5f, Constants.TweenDuration).SetEase(Ease.OutCubic);
        cover.rectTransform.DOScale(1.0f, 0.2f).SetEase(Ease.OutCubic);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        Context.ActiveLevel = level;
        Context.ScreenManager.ChangeScreen("GamePreparation", ScreenTransition.In, 0.4f,
            transitionFocus: GetComponent<RectTransform>().GetScreenSpaceCenter());
    }

}