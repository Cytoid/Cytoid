#if UNITY_INCLUDE_TESTS
using System.IO;
using NUnit.Framework;
using UnityEngine;

// Temporary source-level guards for storyboard lifecycle fixes.
// Prefer behavioral tests when EditMode can host Storyboard/AssetMemory fixtures.
public class StoryboardLifecycleGuardTests
{
    [Test]
    public void Storyboard_OnNoteClear_FiresInOrderThenRemovesSafely()
    {
        var body = MethodBody(
            Path.Combine(Application.dataPath, "Scripts/Storyboard/Storyboard.cs"),
            "public void OnNoteClear(Game game, Note note)",
            "public bool OnTrigger(Trigger trigger)");
        Assert.That(body, Does.Not.Contain("foreach (var trigger in Triggers)"));
        Assert.That(body, Does.Contain("for (var i = 0; i < Triggers.Count; i++)"));
        Assert.That(body, Does.Contain("Triggers.RemoveAt(i)"));
        Assert.That(body, Does.Contain("removals"));
    }

    [Test]
    public void Storyboard_OnTrigger_ReturnsShouldRemoveInsteadOfMutatingList()
    {
        var body = MethodBody(
            Path.Combine(Application.dataPath, "Scripts/Storyboard/Storyboard.cs"),
            "public bool OnTrigger(Trigger trigger)",
            "public JObject Compile()");
        Assert.That(body, Does.Not.Contain("Triggers.Remove"));
        Assert.That(body, Does.Contain("return trigger.CurrentUses == trigger.Uses"));
    }

    [Test]
    public void TextureScaler_ReleaseTemporary_RestoresActiveAndDestroysRt()
    {
        var body = MethodBody(
            Path.Combine(Application.dataPath, "Scripts/Utils/TextureScaler.cs"),
            "static void ReleaseTemporary(RenderTexture temporary, RenderTexture previous)",
            null);
        Assert.That(body, Does.Contain("RenderTexture.active = previous"));
        Assert.That(body, Does.Contain("temporary.Release()"));
        Assert.That(body, Does.Contain("Object.Destroy(temporary)"));
    }

    [Test]
    public void StoryboardRenderer_OnGameUpdate_DestroysFlattenForward()
    {
        // Brace-scoped to OnGameUpdate only (not the rest of the type).
        var body = MethodBody(
            Path.Combine(Application.dataPath, "Scripts/Storyboard/StoryboardRenderer.cs"),
            "public void OnGameUpdate(Game _)",
            null);
        Assert.That(body, Does.Contain(".Flatten(it => it.Children)"));
        Assert.That(body, Does.Contain("EnqueueDestroy(it.Component.Id)"));
        Assert.That(body, Does.Contain("for (var i = 0; i < renderersToDestroy.Count; i++)"));
        Assert.That(body, Does.Contain("DestroyObject(id, renderer)"));
        Assert.That(body, Does.Not.Contain("Count - 1"));
        Assert.That(body, Does.Not.Contain("i >= 0; i--"));
        Assert.That(body, Does.Not.Contain("EnqueueDestroy(renderer.Parent.Component.Id)"));
    }

    [Test]
    public void StoryboardRenderer_SpawnObjects_RollsBackOnFailure()
    {
        var path = Path.Combine(Application.dataPath, "Scripts/Storyboard/StoryboardRenderer.cs");
        // Brace-scope each method so helper definitions cannot satisfy SpawnObjects assertions.
        var spawn = MethodBody(path, "private async UniTask<List<TR>> SpawnObjects", null);
        var init = MethodBody(path, "private async UniTask InitializeSpawnedRenderer", null);
        var rollback = MethodBody(path, "private void RollbackSpawnedRenderer", null);

        Assert.That(spawn, Does.Contain("tasks.Add(InitializeSpawnedRenderer(renderer))"));
        Assert.That(spawn, Does.Contain("RollbackSpawnedRenderer(renderer)"));
        Assert.That(spawn, Does.Contain("catch"));
        Assert.That(init, Does.Contain("await renderer.Initialize()"));
        Assert.That(init, Does.Contain("RollbackSpawnedRenderer(renderer)"));
        Assert.That(rollback, Does.Contain("DestroyObject(id, renderer)"));

        var publish = spawn.IndexOf("ComponentRenderers[transformedObj.Id] = renderer");
        var parentCheck = spawn.IndexOf("transformedObj.ParentId != null");
        var targetCheck = spawn.IndexOf("transformedObj.TargetId != null");
        Assert.That(parentCheck, Is.GreaterThanOrEqualTo(0), "parent_id check missing");
        Assert.That(targetCheck, Is.GreaterThanOrEqualTo(0), "target_id check missing");
        Assert.That(publish, Is.GreaterThanOrEqualTo(0), "ComponentRenderers publish missing");
        Assert.That(publish, Is.GreaterThan(parentCheck));
        Assert.That(publish, Is.GreaterThan(targetCheck));
    }

    [Test]
    public void StoryboardRenderer_DestroyObject_UsesObjectType()
    {
        var body = MethodBody(
            Path.Combine(Application.dataPath, "Scripts/Storyboard/StoryboardRenderer.cs"),
            "public void DestroyObject(string id, StoryboardComponentRenderer renderer)",
            null);
        Assert.That(body, Does.Contain("renderer.ObjectType"));
        Assert.That(body, Does.Not.Contain("GetType()"));
        Assert.That(body, Does.Contain("finally"));
        Assert.That(body, Does.Contain("ReferenceEquals(current, renderer)"));
    }

    [Test]
    public void SpriteRenderer_StaleLoad_UsesCapturedPathAndRenderer()
    {
        var body = MethodBody(
            Path.Combine(Application.dataPath, "Scripts/Storyboard/Sprites/SpriteRenderer.cs"),
            "public override async UniTask Initialize()",
            "public override void Clear()");
        Assert.That(body, Does.Contain("var loadPath ="));
        Assert.That(body, Does.Contain("var mainRenderer = MainRenderer"));
        Assert.That(body, Does.Contain("ReleaseUnusedLoadedAsset(loadPath, mainRenderer)"));
        Assert.That(body, Does.Contain("IsInitializeStale"));
    }

    [Test]
    public void VideoRenderer_OwnsResourcesGate()
    {
        var body = MethodBody(
            Path.Combine(Application.dataPath, "Scripts/Storyboard/Videos/VideoRenderer.cs"),
            "public override void Dispose()",
            "private void UnsubscribePrepareCompleted");
        Assert.That(body, Does.Contain("if (ownsResources)"));
        Assert.That(body, Does.Contain("base.Dispose()"));
    }

    [Test]
    public void AssetMemory_WaiterRetriesWhenLeaderDroppedCache()
    {
        var body = MethodBody(
            Path.Combine(Application.dataPath, "Scripts/Utils/AssetMemory.cs"),
            "public async UniTask<T> LoadAsset<T>(",
            "public bool DisposeAsset(string path, AssetTag tag)");
        Assert.That(body, Does.Contain("return await LoadAsset<T>(path, tag, cancellationToken)"));
        Assert.That(body, Does.Not.Contain("Concurrent asset load failed"));
    }

    static string MethodBody(string path, string startMarker, string endMarker)
    {
        var source = File.ReadAllText(path);
        var methodStart = source.IndexOf(startMarker);
        Assert.That(methodStart, Is.GreaterThanOrEqualTo(0), $"Missing start marker: {startMarker}");
        int methodEnd;
        if (endMarker == null)
        {
            methodEnd = source.IndexOf('{', methodStart);
            Assert.That(methodEnd, Is.GreaterThan(methodStart));
            var depth = 0;
            for (var i = methodEnd; i < source.Length; i++)
            {
                if (source[i] == '{') depth++;
                else if (source[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        methodEnd = i + 1;
                        break;
                    }
                }
            }
        }
        else
        {
            methodEnd = source.IndexOf(endMarker, methodStart);
            Assert.That(methodEnd, Is.GreaterThan(methodStart), $"Missing end marker: {endMarker}");
        }

        return source.Substring(methodStart, methodEnd - methodStart);
    }
}
#endif
