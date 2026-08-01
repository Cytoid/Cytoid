using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UiEventRuntimeTests
{
    private const string GameScenePath = "Assets/Scenes/Game.unity";

    [Test]
    public void ScanlineRestoresGeometryAfterSeekingBeforeAnAnimation()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
        try
        {
            var scanner = FindScanner(scene);
            scanner.SetUiEventState(1, 1, ChartUiAnimationKind.Out, 1, false);
            Assert.That(scanner.lineRenderer.positionCount, Is.EqualTo(100));
            Assert.That(scanner.lineRenderer.GetPosition(0), Is.EqualTo(scanner.lineRenderer.GetPosition(99)));

            scanner.SetUiEventState(1, 1, ChartUiAnimationKind.None, 1, false);
            Assert.That(scanner.lineRenderer.positionCount, Is.EqualTo(2));
            Assert.That(scanner.lineRenderer.GetPosition(0).x,
                Is.LessThan(scanner.lineRenderer.GetPosition(1).x));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void ScannerAndTriangleReceiveTheSameComposedOpacity()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
        GameObject triangleObject = null;
        Material material = null;
        try
        {
            var scanner = FindScanner(scene);
            scanner.opacity = 0.4f;

            triangleObject = new GameObject("UiEventRuntimeTriangle");
            triangleObject.SetActive(false);
            triangleObject.AddComponent<MeshFilter>();
            var meshRenderer = triangleObject.AddComponent<MeshRenderer>();
            material = new Material(Shader.Find("Sprites/Default"));
            meshRenderer.sharedMaterial = material;
            var triangle = triangleObject.AddComponent<MeshTriangle>();
            triangleObject.SetActive(true);
            SetPrivateField(triangle, "meshRenderer", meshRenderer);
            SetPrivateField(triangle, "material", material);
            SetPrivateField(triangle, "scanner", scanner);
            scanner.RegisterTriangle(triangle);

            scanner.SetUiEventState(0.5f, 0.25f, ChartUiAnimationKind.None, 1, false);

            Assert.That(scanner.EffectiveOpacity, Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(meshRenderer.sharedMaterial.color.a, Is.EqualTo(0.005f).Within(0.0001f));
        }
        finally
        {
            if (triangleObject != null) Object.DestroyImmediate(triangleObject);
            if (material != null) Object.DestroyImmediate(material);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void BoundaryOpacityMultipliesStoryboardAndBothUiEventLayers()
    {
        Assert.That(GameRenderer.ComposeBoundaryOpacity(0.5f, 0.4f, 0.25f),
            Is.EqualTo(0.01f).Within(0.0001f));
    }

    [Test]
    public void StoryboardColorOverrideWinsOverChartEventColor()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
        try
        {
            var scanner = FindScanner(scene);
            scanner.opacity = 0.5f;
            scanner.colorOverride = Color.green;
            scanner.SetChartEventColor(Color.red);

            Assert.That(scanner.lineRenderer.startColor.r, Is.EqualTo(0).Within(0.0001f));
            Assert.That(scanner.lineRenderer.startColor.g, Is.EqualTo(1).Within(0.0001f));
            Assert.That(scanner.lineRenderer.startColor.a, Is.EqualTo(0.5f).Within(0.002f));

            scanner.colorOverride = Color.clear;
            scanner.SetChartEventColor(Color.red);
            Assert.That(scanner.lineRenderer.startColor.r, Is.EqualTo(1).Within(0.0001f));
            Assert.That(scanner.lineRenderer.startColor.g, Is.EqualTo(0).Within(0.0001f));
            Assert.That(scanner.lineRenderer.startColor.a, Is.EqualTo(0.5f).Within(0.002f));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void TooltipAppliesSeekSampledMessageRichTextAndSpacing()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
        try
        {
            var tooltip = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<GameTooltipText>(true))
                .Single();
            tooltip.ApplyChartEventState(new ChartEventPresentationState(
                true,
                ChartEventPresentationKind.Message,
                "<b>Hello</b>",
                new Color(0.25f, 0.5f, 0.75f),
                Color.white,
                0.75f,
                21));

            Assert.That(tooltip.tmp.text, Is.EqualTo("<b>Hello</b>"));
            Assert.That(tooltip.tmp.color.r, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(tooltip.tmp.color.g, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(tooltip.tmp.color.b, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(tooltip.tmp.color.a, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(tooltip.tmp.characterSpacing, Is.EqualTo(21).Within(0.0001f));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static Scanner FindScanner(Scene scene)
    {
        var scanner = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Scanner>(true))
            .Single();
        Assert.That(scanner.game, Is.Not.Null);
        Assert.That(scanner.game.camera, Is.Not.Null);
        Assert.That(scanner.lineRenderer, Is.Not.Null);
        return scanner;
    }

    private static void SetPrivateField<T>(MeshTriangle triangle, string name, T value) =>
        typeof(MeshTriangle).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(triangle, value);

    private static void SetPrivateField<T>(Scanner scanner, string name, T value) =>
        typeof(Scanner).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(scanner, value);

}
