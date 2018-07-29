using System;
using System.Collections;
using Unicache.Plugin;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class RankingEntryComponent : MonoBehaviour
    {
        public RankingQueryResult.Ranking Ranking;
        public Text PositionText;
        public Image AvatarImage;
        public Text NameText;
        public Text PerformanceText;
        
        public void Load()
        {
            PositionText.text = "#" + Ranking.rank;
            NameText.text = Ranking.player;
            var grade = ScoreGrades.From(Ranking.score);
            PerformanceText.text = string.Format("<b><color=#{0}>{1}</color></b> {2} / {3:0.00}%", ColorUtility.ToHtmlStringRGB(grade.Color()), grade, Mathf.FloorToInt(Ranking.score), Math.Floor((float) Ranking.accuracy / 100 * 100) / 100);
            CytoidApplication.cache.Fetch(Ranking.avatar_url)
                .DoOnError(error => { })
                .ByteToTexture2D()
                .Subscribe(texture =>
                {
                    try
                    {
                        var rect = new Rect(0, 0, texture.width, texture.height);
                        var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100);

                        AvatarImage.overrideSprite = sprite;
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }, error =>
                {
                    // ignored
                });
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }
        
    }
}