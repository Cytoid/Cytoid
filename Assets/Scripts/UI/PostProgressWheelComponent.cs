using DG.Tweening;
using QuickEngine.Extensions;
using UnityEngine;

namespace Cytoid.UI
{
    public class PostProgressWheelComponent : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private bool loading;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            transform.eulerAngles = new Vector3(0, 0, transform.eulerAngles.z - 135 * Time.deltaTime);
            if (GameResultController.Instance.IsUploading && !loading)
            {
                loading = true;
                canvasGroup.DOFade(1, 0.5f).SetEase(Ease.InQuad);
            }
            else if (!GameResultController.Instance.IsUploading && loading)
            {
                loading = false;
                canvasGroup.DOFade(0, 0.5f).SetEase(Ease.OutQuad);
            }
        }
    }
}