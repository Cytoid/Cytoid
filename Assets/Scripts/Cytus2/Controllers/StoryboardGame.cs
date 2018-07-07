using System.Collections;
using Cytoid.Storyboard;
using Cytus2.Models;
using Lean.Touch;
using UnityEngine;
using Slider = Cytoid.UI.Slider;
using Text = UnityEngine.UI.Text;

namespace Cytus2.Controllers
{
    public class StoryboardGame : Game
    {

        public Canvas InfoCanvas;
        public Slider SeekSlider;

        public bool HideUi;
        
        protected override IEnumerator Start()
        {
            yield return StartCoroutine(base.Start());
            GameOptions.Instance.WillAutoPlay = true;
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
                    SeekSlider.gameObject.SetActive(true);
                }
                else
                {
                    HideUi = true;
                    SeekSlider.gameObject.SetActive(false);
                }
            }
        }

        public override void SpawnNote(ChartNote note)
        {
            base.SpawnNote(note);
            // Generate note id holder
            var canvas = Instantiate(InfoCanvas, GameNotes[note.id].transform.Find("NoteFill"));
            canvas.GetComponentInChildren<Text>().text = "" + note.id;
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