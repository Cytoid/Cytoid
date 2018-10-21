using System.Collections;
using CodeStage.AdvancedFPSCounter;
using Cytoid.Storyboard;
using Cytoid.UI;
using Cytus2.Models;
using Lean.Touch;
using UnityEngine;
using Text = UnityEngine.UI.Text;

namespace Cytus2.Controllers
{
    public class StoryboardGame : Game
    {
        public DirectSlider SeekSlider;
        public CanvasGroup UICanvasGroup;

        public bool HideUi;

        protected override IEnumerator Start()
        {
            yield return StartCoroutine(base.Start());

            Play.Mods.Add(Mod.Auto);

            GameOptions.Instance.ChartOffset = 0;

            SeekSlider.onValueChanged.AddListener(Seek);

            Application.runInBackground = true;
        }

        public virtual void Seek(float value)
        {
            AudioSource.time = value * AudioSource.clip.length;
            StoryboardController.Instance.Reset();
        }

        protected override void Update()
        {
            base.Update();
            SeekSlider.SetDirectly(AudioPercentage);
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (AudioSource.isPlaying)
                {
                    AudioSource.Pause();
                }
                else
                {
                    AudioSource.Play();
                }
            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                if (HideUi)
                {
                    HideUi = false;
                    UICanvasGroup.alpha = 1;
                }
                else
                {
                    HideUi = true;
                    UICanvasGroup.alpha = 0;
                }
            }
        }

        protected override void UpdateOnScreenNotes()
        {
        }

        protected override void OnFingerDown(LeanFinger finger)
        {
        }

        protected override void OnFingerSet(LeanFinger finger)
        {
        }

        protected override void OnFingerUp(LeanFinger finger)
        {
        }

        protected override IEnumerator UnpauseCoroutine()
        {
            UnpauseImmediately();
            return null;
        }

        protected override void OnApplicationPause(bool willPause)
        {
        }

        public override void Complete()
        {
        }
    }
}