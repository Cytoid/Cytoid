using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneExtensions
{
    public static GameObject FindInSceneGameObjectWithTag(this Scene scene, string tag)
    {
        return GameObject.FindGameObjectsWithTag(tag).First(it => it.scene == scene);
    }
}