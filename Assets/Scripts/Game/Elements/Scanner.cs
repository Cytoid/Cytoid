using System.Collections;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public class Scanner : SingletonMonoBehavior<Scanner>
{
    public static Color SpeedUpColor = new Color(0.82352f, 0.33725f, 0.41176f);
    public static Color SpeedDownColor = new Color(0.6289f, 0.78125f, 0.75f);
    
    public Game game;

    public Color colorOverride = Color.clear;
    public float positionOverride = float.MinValue;
    public float opacity = 1f;

    public LineRenderer lineRenderer;
    public float animationDuration;

    private Color colorNext = new Color(1, 1, 1);
    private float colorNextSpeed;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        game.onGameLoaded.AddListener(_ => {
            if (game.State.Mods.Contains(Mod.HideScanline))
            {
                lineRenderer.enabled = false;
            }
        });
        game.onGameStarted.AddListener(_ => PlayEnter());
        game.onGameCompleted.AddListener(_ => PlayExit());
        game.onGameUpdate.AddListener(OnGameUpdate);
        game.onGameSpeedUp.AddListener(_ => PlaySpeedUp());
        game.onGameSpeedDown.AddListener(_ => PlaySpeedDown());
    }

    private void OnEnable()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, new Vector3(0, 0, 0));
        lineRenderer.SetPosition(1, new Vector3(0, 0, 0));
        gameObject.GetComponent<LineRenderer>().startColor = new Color(1f, 1f, 1f);
        gameObject.GetComponent<LineRenderer>().startColor = new Color(1f, 1f, 1f);
        colorNext = new Color(1f, 1f, 1f, opacity);
        colorNextSpeed = 9.0f;
    }

    private IEnumerator ResetLine()
    {
        yield return null;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0,
            new Vector3(-Camera.main.orthographicSize * UnityEngine.Screen.width / UnityEngine.Screen.height * 1000f, 0,
                0));
        lineRenderer.SetPosition(1,
            new Vector3(Camera.main.orthographicSize * UnityEngine.Screen.width / UnityEngine.Screen.height * 1000f, 0,
                0));
        lineRenderer.useWorldSpace = false;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.startWidth = 0.05f;
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
        colorNext = SpeedUpColor;
        colorNextSpeed = 6.0f;
        yield return new WaitForSeconds(3.5f);
        colorNext = new Color(1f, 1f, 1f);
        colorNextSpeed = 24.0f;
    }

    IEnumerator PlaySpeedDownAnimation()
    {
        colorNext = SpeedDownColor;
        colorNextSpeed = 6.0f;
        yield return new WaitForSeconds(3.5f);
        colorNext = new Color(1f, 1f, 1f);
        colorNextSpeed = 24.0f;
    }

    IEnumerator PlayExitAnimation()
    {
        yield return null;
        float timing = 0;
        lineRenderer.positionCount = 100;
        while (timing < animationDuration)
        {
            var progress = timing / animationDuration;
            var randomRange = progress / 10;
            for (var i = 0; i < 100; i++)
            {
                var orthographicSize = Camera.main.orthographicSize;
                lineRenderer.SetPosition(i,
                    new Vector3(
                        (-orthographicSize * UnityEngine.Screen.width / UnityEngine.Screen.height + 2f *
                         orthographicSize * UnityEngine.Screen.width / UnityEngine.Screen.height * (i / 100f)) *
                        (1 - progress),
                        Random.Range(-randomRange, randomRange)));
            }

            yield return null;
            // Continue here next frame
            timing += Time.deltaTime;
        }

        lineRenderer.positionCount = 0;
    }

    IEnumerator PlayEnterAnimation()
    {
        yield return null;
        float timing = 0;
        lineRenderer.positionCount = 100;
        while (timing < animationDuration)
        {
            var progress = timing / animationDuration;
            var randomRange = (1 - progress) / 10;
            for (var i = 0; i < 100; i++)
            {
                var orthographicSize = Camera.main.orthographicSize;
                lineRenderer.SetPosition(i,
                    new Vector3(
                        (-orthographicSize * UnityEngine.Screen.width / UnityEngine.Screen.height + 2f *
                         orthographicSize * UnityEngine.Screen.width / UnityEngine.Screen.height * (i / 100f)) *
                        progress,
                        Random.Range(-randomRange, randomRange)));
            }

            yield return null;
            //Continue here next frame
            timing += Time.deltaTime;
        }

        StartCoroutine(ResetLine());
    }

    public void OnGameUpdate(Game game)
    {
        var chart = game.Chart;
        
        // Color
        Color color;
        if (colorOverride == Color.clear)
        {
            color = gameObject.GetComponent<LineRenderer>().startColor;
            color = new Color((color.r * colorNextSpeed + colorNext.r) / (1 + colorNextSpeed),
                (color.g * colorNextSpeed + colorNext.g) / (1 + colorNextSpeed),
                (color.b * colorNextSpeed + colorNext.b) / (1 + colorNextSpeed));
        }
        else
        {
            color = colorOverride;
        }

        color = color.WithAlpha(opacity);
        gameObject.GetComponent<LineRenderer>().startColor = color;
        gameObject.GetComponent<LineRenderer>().endColor = color;

        // Position
        if (positionOverride != float.MinValue)
        {
            transform.SetY(chart.GetScanlinePosition01(positionOverride));
        }
        else
        {
            transform.SetY(chart.GetScannerPositionY(game.Time, game.Config.UseScannerSmoothing));
        }
        
        // Direction
    }
}