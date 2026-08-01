using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private Color currentColor = Color.white;
    private Coroutine geometryAnimationCoroutine;

    private float uiEventOpacity = 1f;
    private float uiEventAnimationOpacity = 1f;
    private ChartUiAnimationKind uiEventAnimationKind = ChartUiAnimationKind.None;
    private float uiEventAnimationProgress = 1f;
    private bool hasAppliedUiEventAnimation;
    private readonly HashSet<MeshTriangle> triangles = new HashSet<MeshTriangle>();

    private bool exited;

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
    }

    private void OnEnable()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, new Vector3(0, 0, 0));
        lineRenderer.SetPosition(1, new Vector3(0, 0, 0));
        gameObject.GetComponent<LineRenderer>().startColor = new Color(1f, 1f, 1f);
        gameObject.GetComponent<LineRenderer>().startColor = new Color(1f, 1f, 1f);
        currentColor = Color.white;
    }

    private IEnumerator ResetLine()
    {
        yield return null;
        ApplyNormalLineGeometry();
    }

    private void StopGeometryAnimation()
    {
        if (geometryAnimationCoroutine != null)
        {
            StopCoroutine(geometryAnimationCoroutine);
            geometryAnimationCoroutine = null;
        }
    }

    public void PlayEnter()
    {
        StopGeometryAnimation();
        geometryAnimationCoroutine = StartCoroutine(PlayEnterAnimation());
    }

    public void PlayExit()
    {
        StopGeometryAnimation();
        geometryAnimationCoroutine = StartCoroutine(PlayExitAnimation());
    }

    IEnumerator PlayExitAnimation()
    {
        yield return null;
        float timing = 0;
        lineRenderer.positionCount = 100;
        while (timing < animationDuration)
        {
            ApplyLineGeometry(1 - timing / animationDuration);

            yield return null;
            // Continue here next frame
            timing += Time.deltaTime;
        }

        lineRenderer.positionCount = 0;
        exited = true;
        geometryAnimationCoroutine = null;
    }

    IEnumerator PlayEnterAnimation()
    {
        yield return null;
        float timing = 0;
        lineRenderer.positionCount = 100;
        while (timing < animationDuration)
        {
            ApplyLineGeometry(timing / animationDuration);

            yield return null;
            //Continue here next frame
            timing += Time.deltaTime;
        }

        StartCoroutine(ResetLine());
        exited = false;
        geometryAnimationCoroutine = null;
    }

    public float EffectiveOpacity => exited
        ? 0
        : Mathf.Clamp01(opacity * uiEventOpacity * uiEventAnimationOpacity);

    public void SetUiEventState(
        float targetOpacity,
        float animationOpacity,
        ChartUiAnimationKind animationKind,
        float animationProgress,
        bool applyPosition = true)
    {
        uiEventOpacity = Mathf.Clamp01(targetOpacity);
        uiEventAnimationOpacity = Mathf.Clamp01(animationOpacity);
        uiEventAnimationKind = animationKind;
        uiEventAnimationProgress = Mathf.Clamp01(animationProgress);

        if (uiEventAnimationKind == ChartUiAnimationKind.In)
        {
            hasAppliedUiEventAnimation = true;
            if (uiEventAnimationProgress >= 1) ApplyNormalLineGeometry();
            else ApplyLineGeometry(uiEventAnimationProgress);
        }
        else if (uiEventAnimationKind == ChartUiAnimationKind.Out)
        {
            hasAppliedUiEventAnimation = true;
            ApplyLineGeometry(1 - uiEventAnimationProgress);
        }
        else if (hasAppliedUiEventAnimation)
        {
            // A backward seek can move before the latest animation snapshot. Restore the stable
            // geometry once, without overriding the normal system enter animation every frame.
            ApplyNormalLineGeometry();
            hasAppliedUiEventAnimation = false;
        }

        // Storyboard position is updated during onGameLateUpdate. Reapply it here so position,
        // color and opacity all compose in the same rendered frame.
        if (applyPosition) ApplyPosition();
        ApplyVisualState();
    }

    public void SetUiEventOpacity(float targetOpacity, float animationOpacity)
    {
        uiEventOpacity = Mathf.Clamp01(targetOpacity);
        uiEventAnimationOpacity = Mathf.Clamp01(animationOpacity);
    }

    public void SetChartEventColor(Color color)
    {
        currentColor = color;
        ApplyVisualState();
    }

    private void ApplyLineGeometry(float visibleWidth)
    {
        visibleWidth = Mathf.Clamp01(visibleWidth);
        lineRenderer.positionCount = 100;
        var orthographicSize = game.camera.orthographicSize;
        var halfWidth = orthographicSize * UnityEngine.Screen.width / UnityEngine.Screen.height;
        for (var i = 0; i < 100; i++)
        {
            var x = (-halfWidth + 2f * halfWidth * (i / 100f)) * visibleWidth;
            lineRenderer.SetPosition(i, new Vector3(x, 0));
        }
        lineRenderer.useWorldSpace = false;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.startWidth = 0.05f;
    }

    private void ApplyNormalLineGeometry()
    {
        var halfWidth = game.camera.orthographicSize * UnityEngine.Screen.width / UnityEngine.Screen.height;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, new Vector3(-halfWidth * 1000f, 0, 0));
        lineRenderer.SetPosition(1, new Vector3(halfWidth * 1000f, 0, 0));
        lineRenderer.useWorldSpace = false;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.startWidth = 0.05f;
    }

    private void ApplyVisualState()
    {
        var color = colorOverride == Color.clear ? currentColor : colorOverride;
        color = color.WithAlpha(EffectiveOpacity);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        foreach (var triangle in triangles)
            if (triangle != null) triangle.ApplyOpacity(EffectiveOpacity);
    }

    public void RegisterTriangle(MeshTriangle triangle) => triangles.Add(triangle);

    public void UnregisterTriangle(MeshTriangle triangle) => triangles.Remove(triangle);

    public void OnGameUpdate(Game game)
    {
        ApplyPosition();

        // Direction
    }

    private void ApplyPosition()
    {
        if (positionOverride != float.MinValue)
        {
            transform.SetY(positionOverride);
        }
        else
        {
            transform.SetY(game.Chart.GetScannerPositionY(game.Time, game.Config.UseScannerSmoothing));
        }
    }
}
