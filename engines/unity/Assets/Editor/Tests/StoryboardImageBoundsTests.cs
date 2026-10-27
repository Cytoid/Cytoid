using Cytoid.Storyboard.Sprites;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using StoryboardRenderer = Cytoid.Storyboard.StoryboardRenderer;

public class StoryboardImageBoundsTests
{
    private Camera camera;
    private Canvas canvas;
    private RectTransform parent;
    private Image original;
    private StoryboardImage patched;
    private StoryboardRenderer renderer;

    [SetUp]
    public void SetUp()
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            Assert.Ignore("Requires a graphics device for pixel comparisons.");

        camera = new GameObject("Bounds camera", typeof(Camera)).GetComponent<Camera>();
        camera.transform.position = new Vector3(0, 26.666667f, -10);
        camera.fieldOfView = 53.2f;
        camera.nearClipPlane = .01f;
        camera.farClipPlane = 5000;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 1 << 31;
        camera.targetTexture = new RenderTexture(800, 600, 24);
        canvas = new GameObject("Original canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup))
            .GetComponent<Canvas>();
        canvas.gameObject.layer = 31;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 100;
        Canvas.ForceUpdateCanvases();
        parent = new GameObject("Note", typeof(RectTransform)).GetComponent<RectTransform>();
        parent.SetParent(canvas.transform, false);
        parent.anchorMin = parent.anchorMax = Vector2.zero;
        parent.anchoredPosition = new Vector2(400, 100);
        parent.sizeDelta = Vector2.zero;
        original = CreateImage<Image>();
        patched = CreateImage<StoryboardImage>();
        patched.gameObject.SetActive(false);
        renderer = new StoryboardRenderer(null);
        typeof(StoryboardRenderer).GetProperty(nameof(StoryboardRenderer.Camera)).SetValue(renderer, camera);
        var sprite = new Cytoid.Storyboard.Sprites.SpriteRenderer(renderer, new Cytoid.Storyboard.Sprite());
        typeof(Cytoid.Storyboard.Sprites.SpriteRenderer).GetProperty("Image").SetValue(sprite, patched);
        renderer.ComponentRenderers.Add("sprite", sprite);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(canvas.gameObject);
        var target = camera.targetTexture;
        camera.targetTexture = null;
        Object.DestroyImmediate(target);
        Object.DestroyImmediate(camera.gameObject);
    }

    [TestCase(400f, 800f)]
    [TestCase(508.9f, 1017.8f)]
    [TestCase(509f, 1018f)]
    [TestCase(1400f, 3200f)]
    [TestCase(1600f, 3200f)]
    public void DepthSpritesRenderWithOriginalGeometry(float y, float z)
    {
        original.transform.localPosition = patched.transform.localPosition = new Vector3(0, y, z);
        var before = Pixels();
        var originalCorners = new Vector3[4];
        original.rectTransform.GetWorldCorners(originalCorners);
        var originalMesh = original.canvasRenderer.GetMesh();
        var originalVertices = originalMesh.vertices;
        var originalUvs = originalMesh.uv;
        var originalTriangles = originalMesh.triangles;
        var originalCenter = originalMesh.bounds.center;
        ShowPatched();
        var after = Pixels();
        Assert.That(after, Is.GreaterThan(0));
        if (before > 0) Assert.That(after, Is.EqualTo(before));
        var patchedCorners = new Vector3[4];
        patched.rectTransform.GetWorldCorners(patchedCorners);
        for (var i = 0; i < 4; i++)
            Assert.That(Vector3.Distance(originalCorners[i], patchedCorners[i]), Is.LessThan(.002f));
        var patchedMesh = patched.canvasRenderer.GetMesh();
        Assert.That(patchedMesh.vertices, Is.EqualTo(originalVertices));
        Assert.That(patchedMesh.uv, Is.EqualTo(originalUvs));
        Assert.That(patchedMesh.triangles, Is.EqualTo(originalTriangles));
        Assert.That(LastGeneratedBounds().center, Is.EqualTo(originalCenter));
        Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera));
    }

    [Test]
    public void RectMaskStillClipsTheOriginalImageGeometry()
    {
        // A sorting-override subcanvas intentionally stops ancestor mask lookup.
        original.GetComponent<Canvas>().overrideSorting = false;
        patched.GetComponent<Canvas>().overrideSorting = false;
        parent.sizeDelta = new Vector2(160, 20);
        parent.gameObject.AddComponent<RectMask2D>();
        original.transform.localPosition = patched.transform.localPosition = new Vector3(0, 10, 0);
        var before = Pixels();
        ShowPatched();
        Assert.That(before, Is.GreaterThan(0));
        Assert.That(Pixels(), Is.EqualTo(before));
        parent.GetComponent<RectMask2D>().enabled = false;
        Assert.That(Pixels(), Is.GreaterThan(before));
    }

    [Test]
    public void OriginalCanvasOpacityAndActivationStillControlSprites()
    {
        ShowPatched();
        Assert.That(Pixels(), Is.GreaterThan(0));
        var group = canvas.GetComponent<CanvasGroup>();
        group.alpha = 0;
        Assert.That(Pixels(), Is.Zero);
        group.alpha = 1;
        Assert.That(Pixels(), Is.GreaterThan(0));
        canvas.gameObject.SetActive(false);
        Assert.That(Pixels(), Is.Zero);
        canvas.gameObject.SetActive(true);
        Assert.That(Pixels(), Is.GreaterThan(0));
    }

    [Test]
    public void CachedMeshCanMoveIntoTheDepthSection()
    {
        ShowPatched();
        Assert.That(Pixels(), Is.GreaterThan(0));
        patched.transform.localPosition = new Vector3(0, 1400, 3200);
        Assert.That(Pixels(), Is.GreaterThan(0));
        patched.transform.localPosition = Vector3.zero;
        Assert.That(Pixels(), Is.GreaterThan(0));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CachedMeshTracksProjectionSwitches(bool switchWhileInactive)
    {
        renderer.SetCameraOrthographic(true);
        ShowPatched();
        patched.transform.localPosition = new Vector3(0, 1400, 3200);
        Pixels();
        Assert.That(LastGeneratedBounds().extents.magnitude, Is.LessThan(1000));

        for (var i = 0; i < 3; i++)
        {
            if (switchWhileInactive) patched.gameObject.SetActive(false);
            renderer.SetCameraOrthographic(false);
            if (switchWhileInactive) patched.gameObject.SetActive(true);
            Assert.That(Pixels(), Is.GreaterThan(0), "Perspective must repair the cached mesh in the same render.");
            Assert.That(LastGeneratedBounds().extents.x, Is.GreaterThanOrEqualTo(1000000));

            if (switchWhileInactive) patched.gameObject.SetActive(false);
            renderer.SetCameraOrthographic(true);
            if (switchWhileInactive) patched.gameObject.SetActive(true);
            Pixels();
            Assert.That(LastGeneratedBounds().extents.magnitude, Is.LessThan(1000), "Orthographic must restore tight bounds.");
        }

        var dirtyCalls = 0;
        patched.RegisterDirtyVerticesCallback(() => dirtyCalls++);
        renderer.SetCameraOrthographic(true);
        Assert.That(dirtyCalls, Is.Zero, "An unchanged projection must not rebuild the mesh.");
    }

    [TestCase(0f, 0f, 1f, 1f, true)]
    [TestCase(63.43495f, 0f, 1f, 1f, true)]
    [TestCase(25f, 55f, -1f, 2f, true)]
    [TestCase(70f, 35f, .5f, -2f, false)]
    public void OrthographicDepthGeometryMatchesWorldSpace(float xAngle, float yAngle, float xScale, float yScale, bool overrideSorting)
    {
        renderer.SetCameraOrthographic(true);
        ShowPatched();
        patched.GetComponent<Canvas>().overrideSorting = overrideSorting;
        parent.anchorMin = parent.anchorMax = new Vector2(.5f, .5f);
        parent.anchoredPosition = Vector2.zero;
        parent.localRotation = Quaternion.Euler(10, 15, 20);
        parent.localScale = new Vector3(.8f, 1.2f, 1);
        patched.transform.localPosition = new Vector3(0, 0, 800);
        patched.transform.localRotation = Quaternion.Euler(xAngle, yAngle, 12);
        patched.transform.localScale = new Vector3(xScale, yScale, 1);
        var screenPixels = Pixels();
        Assert.That(screenPixels, Is.GreaterThan(0));
        Assert.That(LastGeneratedBounds().extents.magnitude, Is.LessThan(1000));
        canvas.renderMode = RenderMode.WorldSpace;
        Assert.That(Pixels(), Is.EqualTo(screenPixels));
    }

    [Test]
    public void MaskedCachedMeshRecoversAfterProjectionSwitch()
    {
        original.GetComponent<Canvas>().overrideSorting = false;
        renderer.SetCameraOrthographic(true);
        ShowPatched();
        parent.sizeDelta = new Vector2(160, 20);
        var mask = parent.gameObject.AddComponent<RectMask2D>();
        patched.transform.localPosition = new Vector3(0, 1400, 3200);
        Assert.That(Pixels(), Is.Zero);
        Assert.That(patched.canvasRenderer.cull, Is.True);
        renderer.SetCameraOrthographic(false);
        Assert.That(Pixels(), Is.Zero);
        mask.enabled = false;
        Assert.That(Pixels(), Is.GreaterThan(0));
    }

    [Test]
    public void AddingAndRemovingRectMaskRestoresCachedBounds()
    {
        original.GetComponent<Canvas>().overrideSorting = false;
        patched.GetComponent<Canvas>().overrideSorting = false;
        parent.sizeDelta = new Vector2(160, 20);
        original.transform.localPosition = patched.transform.localPosition = new Vector3(0, 10, 0);
        ShowPatched();
        var full = Pixels();
        var mask = parent.gameObject.AddComponent<RectMask2D>();
        var clipped = Pixels();
        Assert.That(clipped, Is.GreaterThan(0).And.LessThan(full));
        patched.gameObject.SetActive(false);
        original.gameObject.SetActive(true);
        original.GetComponent<Canvas>().overrideSorting = false;
        original.RecalculateClipping();
        original.RecalculateMasking();
        Assert.That(Pixels(), Is.EqualTo(clipped));
        ShowPatched();
        mask.enabled = false;
        Assert.That(Pixels(), Is.EqualTo(full));
    }

    [Test]
    public void StencilMaskStillClipsOriginalGeometry()
    {
        original.GetComponent<Canvas>().overrideSorting = false;
        patched.GetComponent<Canvas>().overrideSorting = false;
        parent.sizeDelta = new Vector2(160, 20);
        parent.gameObject.layer = 31;
        parent.gameObject.AddComponent<Image>().color = Color.red;
        var mask = parent.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        original.transform.localPosition = patched.transform.localPosition = new Vector3(0, 10, 0);
        var before = Pixels();
        ShowPatched();
        Assert.That(before, Is.GreaterThan(0));
        Assert.That(Pixels(), Is.EqualTo(before));
        mask.enabled = false;
        Assert.That(Pixels(), Is.GreaterThan(before));
    }

    private T CreateImage<T>() where T : Image
    {
        var go = new GameObject(typeof(T).Name, typeof(RectTransform), typeof(T), typeof(Canvas));
        go.layer = 31;
        go.transform.SetParent(parent, false);
        go.GetComponent<Canvas>().overrideSorting = true;
        go.GetComponent<Canvas>().sortingOrder = 1;
        var image = go.GetComponent<T>();
        image.color = Color.green;
        image.rectTransform.sizeDelta = new Vector2(160, 40);
        image.transform.localRotation = Quaternion.Euler(63.43495f, 0, 0);
        return image;
    }

    private void ShowPatched()
    {
        var overrideSorting = original.GetComponent<Canvas>().overrideSorting;
        original.gameObject.SetActive(false);
        patched.gameObject.SetActive(true);
        // Canvas activation can restore overrideSorting; both sides must use the
        // same setting before comparing mask inheritance.
        patched.GetComponent<Canvas>().overrideSorting = overrideSorting;
        patched.RecalculateClipping();
        patched.RecalculateMasking();
    }

    private int Pixels()
    {
        Canvas.ForceUpdateCanvases();
        camera.Render();
        var previous = RenderTexture.active;
        var target = camera.targetTexture;
        var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            pixels.Apply();
            var count = 0;
            foreach (var p in pixels.GetPixels32())
                if (p.g > 50 && p.g > p.r * 2 && p.g > p.b * 2) count++;
            return count;
        }
        finally
        {
            RenderTexture.active = previous;
            Object.DestroyImmediate(pixels);
        }
    }

    private Bounds LastGeneratedBounds()
    {
        // Inspect the shared mesh immediately after this fixture's geometry submission.
        var property = typeof(Graphic).GetProperty("workerMesh",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        return ((Mesh)property.GetValue(null)).bounds;
    }
}

public class StoryboardImagePrefabTests
{
    [Test]
    public void SpritePrefabUsesBoundsFixAndKeepsItsImageSettings()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StaticResources/Prefabs/Storyboard/Sprite.prefab");
        Assert.That(prefab, Is.Not.Null);
        var image = prefab.GetComponent<Image>();
        Assert.That(image, Is.TypeOf<StoryboardImage>());
        Assert.That(image.preserveAspect, Is.True);
        Assert.That(image.raycastTarget, Is.False);
        Assert.That(image.rectTransform.sizeDelta, Is.EqualTo(new Vector2(200, 200)));
    }
}
