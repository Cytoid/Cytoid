using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class ArtistSourceButton : MonoBehaviour
    {
        private Level level;

        private void Awake()
        {
            EventKit.Subscribe<string>("meta reloaded", OnLevelMetaReloaded);
        }

        private void OnDestroy()
        {
            EventKit.Unsubscribe<string>("meta reloaded", OnLevelMetaReloaded);
        }

        private void OnLevelMetaReloaded(string levelId)
        {
            level = null;
        }
        
        private void Update()
        {
            if (CytoidApplication.CurrentLevel != level)
            {
                level = CytoidApplication.CurrentLevel;
                if (level == null || level.artist_source == null)
                {
                    GetComponentInChildren<Image>().enabled = false;
                }
                else
                {
                    GetComponentInChildren<Image>().enabled = true;
                    var link = CytoidApplication.CurrentLevel.artist_source;
                    var btn = GetComponent<Button>();
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => Application.OpenURL(link));
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
            }
        }
    }
}