using System;
using System.Collections.Generic;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelCard : InteractableMonoBehavior, IPointerClickHandler
{

    public Image cover;
    public Text artist;
    public Text title;
    public Text titleLocalized;

    public List<DifficultyBall> difficultyBalls = new List<DifficultyBall>();
    
    public override bool IsPointerEntered => false;

    private Level level;

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

        for (var index = 0; index < 3; index++)
        {
            if (index <= level.Meta.charts.Count - 1)
            {
                var chart = level.Meta.charts[index];
                difficultyBalls[index].gameObject.SetActive(true);
                difficultyBalls[index].SetModel(Difficulty.Parse(chart.type), chart.difficulty);
            }
            else
            {
                difficultyBalls[index].gameObject.SetActive(false);
            }
        }

        LayoutFixer.Fix(transform);

        LoadCover();
    }

    public async void LoadCover()
    {
        var path = "file://" + level.Path + ".thumbnail";

        var sprite = await Context.SpriteCache.CacheSprite(path, "localLevelCoverThumbnail");
        cover.sprite = sprite;
        cover.GetComponent<AspectRatioFitter>().aspectRatio = sprite.texture.width * 1.0f / sprite.texture.height;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        cover.DOFade(1.0f, 0.2f).SetEase(Ease.OutCubic);
        cover.rectTransform.DOScale(1.02f, 0.2f).SetEase(Ease.OutCubic);
    }
    
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        cover.DOFade(0.5f, 0.2f).SetEase(Ease.OutCubic);
        cover.rectTransform.DOScale(1.0f, 0.2f).SetEase(Ease.OutCubic);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        Context.SelectedLevel = level;
        Context.ScreenManager.ChangeScreen("GamePreparation", ScreenTransition.In, 0.4f,
            transitionFocus: GetComponent<RectTransform>().GetScreenSpaceCenter());
    }

}