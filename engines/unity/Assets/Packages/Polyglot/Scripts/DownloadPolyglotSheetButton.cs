using UnityEngine;
using UnityEngine.UI;

namespace Polyglot
{
    public class DownloadPolyglotSheetButton : MonoBehaviour
    {
        [SerializeField]
        private Button button;

        [SerializeField]
        private RectTransform progressbar = null;

        private float startWidth;

        private void Reset()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            startWidth = progressbar.sizeDelta.x;
            button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            StartCoroutine(LocalizationImporter.DownloadPolyglotSheet(UpdateProgressbar));
        }

        private bool UpdateProgressbar(float progress)
        {
            if (progressbar != null)
            {
                progressbar.sizeDelta = new Vector2(progress * startWidth, progressbar.sizeDelta.y);
            }

            return false;
        }
    }
}
