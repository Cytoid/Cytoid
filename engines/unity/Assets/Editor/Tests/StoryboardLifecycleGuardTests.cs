#if UNITY_INCLUDE_TESTS
using System.IO;
using NUnit.Framework;
using UnityEngine;

// Source-level guards for storyboard lifecycle fixes (trigger remove, destroy typing, RT release).
public class StoryboardLifecycleGuardTests
{
    [Test]
    public void Storyboard_OnNoteClear_DoesNotForeachRemoveTriggers()
    {
        var source = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Storyboard/Storyboard.cs"));
        var methodStart = source.IndexOf("public void OnNoteClear(Game game, Note note)");
        Assert.That(methodStart, Is.GreaterThanOrEqualTo(0));
        var methodEnd = source.IndexOf("public bool OnTrigger(Trigger trigger)", methodStart);
        Assert.That(methodEnd, Is.GreaterThan(methodStart));
        var body = source.Substring(methodStart, methodEnd - methodStart);
        Assert.That(body, Does.Not.Contain("foreach (var trigger in Triggers)"));
        Assert.That(body, Does.Contain("for (var i = Triggers.Count - 1; i >= 0; i--)"));
        Assert.That(body, Does.Contain("Triggers.RemoveAt(i)"));
    }

    [Test]
    public void Storyboard_OnTrigger_ReturnsShouldRemoveInsteadOfMutatingList()
    {
        var source = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Storyboard/Storyboard.cs"));
        var methodStart = source.IndexOf("public bool OnTrigger(Trigger trigger)");
        Assert.That(methodStart, Is.GreaterThanOrEqualTo(0));
        var methodEnd = source.IndexOf("public JObject Compile()", methodStart);
        Assert.That(methodEnd, Is.GreaterThan(methodStart));
        var body = source.Substring(methodStart, methodEnd - methodStart);
        Assert.That(body, Does.Not.Contain("Triggers.Remove"));
        Assert.That(body, Does.Contain("return trigger.CurrentUses == trigger.Uses"));
    }

    [Test]
    public void TextureScaler_ReleasesTemporaryRenderTexture()
    {
        var source = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Utils/TextureScaler.cs"));
        Assert.That(source, Does.Contain("ReleaseTemporary"));
        Assert.That(source, Does.Contain("temporary.Release()"));
        Assert.That(source, Does.Contain("Object.Destroy(temporary)"));
        Assert.That(source, Does.Contain("RenderTexture.active = previous"));
    }

    [Test]
    public void StoryboardRenderer_DestroyUsesObjectTypeNotGetType()
    {
        var source = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Storyboard/StoryboardRenderer.cs"));
        Assert.That(source, Does.Contain("renderer.ObjectType"));
        Assert.That(source, Does.Not.Contain("TypedComponentRenderers[it.GetType()]"));
        Assert.That(source, Does.Contain("public void DestroyObject(string id, StoryboardComponentRenderer renderer)"));
    }

    [Test]
    public void StoryboardRenderers_GuardSharedResourceOwnership()
    {
        Assert.That(File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Storyboard/Videos/VideoRenderer.cs")),
            Does.Contain("ownsResources"));
        Assert.That(File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Storyboard/Sprites/SpriteRenderer.cs")),
            Does.Contain("IsInitializeStale"));
        Assert.That(File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Storyboard/Sprites/SpriteRenderer.cs")),
            Does.Contain("ReleaseUnusedLoadedAsset"));
    }
}
#endif
