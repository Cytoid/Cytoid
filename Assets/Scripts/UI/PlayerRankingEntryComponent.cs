using System;
using System.Collections;
using Unicache.Plugin;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class PlayerRankingEntryComponent : MonoBehaviour
    {
        public PlayerRankingQueryResult.Ranking Ranking;
        public Text PositionText;
        public Image AvatarImage;
        public Text NameText;
        public Text DataText;

        public void Load()
        {
            PositionText.text = "#" + Ranking.rank;
            NameText.text = Ranking.player;
            DataText.text = string.Format(Ranking.data);
            CytoidApplication.Cache.Fetch(Ranking.avatar_url)
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