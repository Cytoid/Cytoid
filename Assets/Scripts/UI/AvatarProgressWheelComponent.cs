using DG.Tweening;
using QuickEngine.Extensions;
using UnityEngine;

namespace Cytoid.UI
{
    public class AvatarProgressWheelComponent : MonoBehaviour
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
            if ((OnlinePlayer.Authenticating || (OnlinePlayer.Authenticated && !LevelSelectionController.Instance.LoadedAvatar)) && !loading)
            {
                loading = true;
                canvasGroup.DOFade(1, 0.5f).SetEase(Ease.InQuad);
            }
            else if (!OnlinePlayer.Authenticating && LevelSelectionController.Instance.LoadedAvatar && loading)
            {
                loading = false;
                canvasGroup.DOFade(0, 0.5f).SetEase(Ease.OutQuad);
            }
        }
    }
}