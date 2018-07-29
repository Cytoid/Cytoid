using Lean.Touch;
using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class StarRatingComponent : SingletonMonoBehavior<StarRatingComponent>
    {

        public float Rating
        {
            get { return rating; }
            set
            {
                rating = value;
                UpdateView();
            }
        }

        private float rating;

        public GameObject OverlayRoot;
        public Image[] Overlays;

        private int currentFinger = -1;

        protected override void Awake()
        {
            base.Awake();
            Overlays = OverlayRoot.gameObject.GetComponentsInChildren<Image>(OverlayRoot);
            LeanTouch.OnFingerDown += OnFingerDown;
            LeanTouch.OnFingerUp += OnFingerUp;
            
            foreach (var overlay in Overlays)
            {
                overlay.color = overlay.color.WithAlpha(0f);
            }

            for (var index = 0; index < Overlays.Length; index++)
            {
                var overlay = Overlays[index];
                var script = overlay.gameObject.AddComponent<StarRatingOverlay>();
                script.Parent = this;
                script.Index = index;
            }
        }

        protected virtual void OnFingerDown(LeanFinger finger)
        {
            currentFinger = finger.Index;
        }

        protected virtual void OnFingerUp(LeanFinger finger)
        {
            currentFinger = -1;
        }

        public void On(int index, bool down)
        {
            if (!down && currentFinger == -1) return;
            
            rating = 0.5f * (index + 1);

            UpdateView();
        }

        private void UpdateView()
        {
            int i;
            for (i = 0; i < 10; i++)
            {
                Overlays[i].color = Overlays[i].color.WithAlpha(0);
            }

            var rounded = (int) rating;
            for (i = 0; i < rounded * 2; i++)
            {
                Overlays[i].color = Overlays[i].color.WithAlpha(1);
            }

            if (rating > rounded)
            {
                Overlays[i].color = Overlays[i].color.WithAlpha(1);
            }
        }

    }
}