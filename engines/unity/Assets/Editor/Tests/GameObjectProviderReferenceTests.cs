using System;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Guards the Game scene's prefab wiring. A reference that points at a child
/// object instead of the prefab root instantiates only that subtree, so
/// GetComponent on the clone silently returns null — the note pool then enqueues
/// nulls and ObjectPool.Dispose throws, killing every game teardown. Regression
/// for the drop-note prefabs pointing at their NoteFill children.
/// </summary>
public class GameObjectProviderReferenceTests
{
    [Test]
    public void PrefabReferencesPointAtRootObjectsCarryingTheirScripts()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
            var provider = UnityEngine.Object.FindObjectOfType<GameObjectProvider>();
            Assert.That(provider, Is.Not.Null, "Game.unity must contain a GameObjectProvider");

            AssertRootComponent(provider.clickNotePrefab, typeof(ClickNote), "clickNotePrefab");
            AssertRootComponent(provider.dragChildNotePrefab, typeof(DragChildNote), "dragChildNotePrefab");
            AssertRootComponent(provider.cDragHeadNotePrefab, typeof(DragHeadNote), "cDragHeadNotePrefab");
            AssertRootComponent(provider.dragHeadNotePrefab, typeof(DragHeadNote), "dragHeadNotePrefab");
            AssertRootComponent(provider.dropClickNotePrefab, typeof(DropClickNote), "dropClickNotePrefab");
            AssertRootComponent(provider.dropDragNotePrefab, typeof(DropDragNote), "dropDragNotePrefab");
            AssertRootComponent(provider.flickNotePrefab, typeof(FlickNote), "flickNotePrefab");
            AssertRootComponent(provider.holdNotePrefab, typeof(HoldNote), "holdNotePrefab");
            AssertRootComponent(provider.longHoldNotePrefab, typeof(LongHoldNote), "longHoldNotePrefab");
            AssertRootComponent(
                provider.noteIdPrefab != null ? provider.noteIdPrefab.gameObject : null,
                typeof(NoteId), "noteIdPrefab");
        }
        finally
        {
            // Leave an empty scene active: later tests must not inherit Game.unity
            // as the only open scene (unloading it is not supported).
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    private static void AssertRootComponent(GameObject prefab, Type script, string fieldName)
    {
        Assert.That(prefab, Is.Not.Null, $"{fieldName} is not assigned");
        Assert.That(prefab.GetComponent(script), Is.Not.Null,
            $"{fieldName} references '{prefab.name}', whose root is missing {script.Name}. " +
            "The reference likely points at a child object instead of the prefab root.");
    }
}
