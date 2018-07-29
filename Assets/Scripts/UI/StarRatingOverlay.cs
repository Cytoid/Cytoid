using Lean.Touch;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class StarRatingOverlay : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
    {

        public StarRatingComponent Parent;
        public int Index;

        public void OnPointerEnter(PointerEventData eventData)
        {
            Parent.On(Index, false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Parent.On(Index, true);
        }
    }
}