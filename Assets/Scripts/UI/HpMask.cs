using DG.Tweening;
using UnityEngine;

namespace Cytoid.UI
{
    public class HpMask : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private float toAlpha;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            if (CytoidApplication.CurrentPlay.MaxHp > 0)
            {
                var p = CytoidApplication.CurrentPlay.Hp / (CytoidApplication.CurrentPlay.MaxHp / 1.5f);
                toAlpha = 1 - Mathf.Clamp01(p);
            }

            canvasGroup.DOFade(toAlpha, 0.4f).SetEase(Ease.OutQuad);
        }
    }
}