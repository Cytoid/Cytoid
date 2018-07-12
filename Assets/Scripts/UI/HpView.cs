using Cytus2.Models;
using DG.Tweening;
using QuickEngine.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class HpView : MonoBehaviour
    {

        public Canvas Canvas;
        
        private Image image;
        
        private void Awake()
        {
            image = GetComponent<Image>();
            image.rectTransform.SetWidth(Canvas.GetComponent<RectTransform>().rect.width);
            transform.ChangeLocalScale(x: 0);
        }

        private void Update()
        {
            if (CytoidApplication.CurrentPlay == null) return;
            if (Mod.Hard.IsEnabled() || Mod.ExHard.IsEnabled())
            {
                transform.DOScaleX((CytoidApplication.CurrentPlay.Hp == 0f && CytoidApplication.CurrentPlay.MaxHp == 0f) ? 0 : CytoidApplication.CurrentPlay.Hp / CytoidApplication.CurrentPlay.MaxHp, 0.4f).SetEase(Ease.OutQuad);
            }
        }
        
    }
}