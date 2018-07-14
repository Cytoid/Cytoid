using System.Collections;
using UnityEngine;

namespace Cytus2.Models
{
    public class ScanlineView : SingletonMonoBehavior<ScanlineView>
    {
        public LineRenderer LineRenderer;
        public float AnimationDuration;
        public int Direction;

        private Color colorNext = new Color(1, 1, 1);
        private float colorNextSpeed;
        private Coroutine animationCoroutine;

        private void OnEnable()
        {
            LineRenderer.positionCount = 2;
            LineRenderer.SetPosition(0, new Vector3(0, 0, 0));
            LineRenderer.SetPosition(1, new Vector3(0, 0, 0));
            gameObject.GetComponent<LineRenderer>().startColor = new Color(1f, 1f, 1f);
            gameObject.GetComponent<LineRenderer>().startColor = new Color(1f, 1f, 1f);
            colorNext = new Color(1f, 1f, 1f);
            colorNextSpeed = 9.0f;
        }

        private IEnumerator ResetLine()
        {
            yield return null;
            LineRenderer.positionCount = 2;
            LineRenderer.SetPosition(0,
                new Vector3(-Camera.main.orthographicSize * Screen.width / Screen.height * 1000f, 0, 0));
            LineRenderer.SetPosition(1,
                new Vector3(Camera.main.orthographicSize * Screen.width / Screen.height * 1000f, 0, 0));
            LineRenderer.useWorldSpace = false;
            LineRenderer.endWidth = 0.05f;
            LineRenderer.startWidth = 0.05f;
        }

        public void EnsureSingleAnimation()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                StartCoroutine(ResetLine());
            }
        }

        public void PlayEnter()
        {
            EnsureSingleAnimation();
            animationCoroutine = StartCoroutine(PlayEnterAnimation());
        }

        public void PlayExit()
        {
            EnsureSingleAnimation();
            animationCoroutine = StartCoroutine(PlayExitAnimation());
        }

        public void PlaySpeedUp()
        {
            EnsureSingleAnimation();
            animationCoroutine = StartCoroutine(PlaySpeedUpAnimation());
        }

        public void PlaySpeedDown()
        {
            EnsureSingleAnimation();
            animationCoroutine = StartCoroutine(PlaySpeedDownAnimation());
        }

        IEnumerator PlaySpeedUpAnimation()
        {
            colorNext = new Color(0.82352f, 0.33725f, 0.41176f);
            colorNextSpeed = 6.0f;
            yield return new WaitForSeconds(3.5f);
            colorNext = new Color(1f, 1f, 1f);
            colorNextSpeed = 24.0f;
        }

        IEnumerator PlaySpeedDownAnimation()
        {
            colorNext = new Color(0.6289f, 0.78125f, 0.75f);
            colorNextSpeed = 6.0f;
            yield return new WaitForSeconds(3.5f);
            colorNext = new Color(1f, 1f, 1f);
            colorNextSpeed = 24.0f;
        }

        IEnumerator PlayExitAnimation()
        {
            yield return null;
            float timing = 0;
            LineRenderer.positionCount = 100;
            while (timing < AnimationDuration)
            {
                float progress = timing / AnimationDuration;
                var randomRange = progress / 10;
                for (int i = 0; i < 100; ++i)
                {
                    LineRenderer.SetPosition(i,
                        new Vector3(
                            (-Camera.main.orthographicSize * Screen.width / Screen.height + 2f *
                             Camera.main.orthographicSize * Screen.width / Screen.height * (i / 100f)) * (1 - progress),
                            Random.Range(-randomRange, randomRange)));
                }

                yield return null;
                // Continue here next frame
                timing += Time.deltaTime;
            }

            LineRenderer.positionCount = 0;
        }

        IEnumerator PlayEnterAnimation()
        {
            yield return null;
            float timing = 0;
            LineRenderer.positionCount = 100;
            while (timing < AnimationDuration)
            {
                float progress = timing / AnimationDuration;
                var randomRange = (1 - progress) / 10;
                for (int i = 0; i < 100; ++i)
                {
                    LineRenderer.SetPosition(i,
                        new Vector3(
                            (-Camera.main.orthographicSize * Screen.width / Screen.height + 2f *
                             Camera.main.orthographicSize * Screen.width / Screen.height * (i / 100f)) * progress,
                            Random.Range(-randomRange, randomRange)));
                }

                yield return null;
                //Continue here next frame
                timing += Time.deltaTime;
            }

            StartCoroutine(ResetLine());
        }

        void FixedUpdate()
        {
            Color currColor = gameObject.GetComponent<LineRenderer>().startColor;
            currColor = new Color((currColor.r * colorNextSpeed + colorNext.r) / (1 + colorNextSpeed),
                (currColor.g * colorNextSpeed + colorNext.g) / (1 + colorNextSpeed),
                (currColor.b * colorNextSpeed + colorNext.b) / (1 + colorNextSpeed));
            gameObject.GetComponent<LineRenderer>().startColor = currColor;
            gameObject.GetComponent<LineRenderer>().endColor = currColor;
        }
    }
}