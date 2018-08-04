using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class AvatarImage : MonoBehaviour
    {

        private bool loaded;
        private Image image;

        private void Awake()
        {
            image = GetComponent<Image>();

            if (SceneManager.GetActiveScene().name == "GameResult" && !OnlinePlayer.Authenticated)
            {
                image.color = Color.clear;
                transform.parent.GetComponent<Image>().color = Color.clear;
            }
        }

        private void Update()
        {
            if (OnlinePlayer.AvatarTexture != null && !loaded)
            {
                loaded = true;
                var texture = OnlinePlayer.AvatarTexture;
                
                var rect = new Rect(0, 0, texture.width, texture.height);
                var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100);

                image.overrideSprite = sprite;
            }

            if (loaded && OnlinePlayer.AvatarTexture == null)
            {
                loaded = false;
                image.overrideSprite = null;
            }
        }
    }
}