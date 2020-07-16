
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneLoader
{
    private readonly string currentScene;
    private readonly string scene;
    private AsyncOperation asyncOperation;
    public bool IsLoaded { get; private set; }

    public SceneLoader(string scene)
    {
        currentScene = SceneManager.GetActiveScene().name;
        this.scene = scene;
    }

    public async UniTask Load()
    {
        Context.PreSceneChanged.Invoke(currentScene, scene);
        asyncOperation = SceneManager.LoadSceneAsync(scene);
        asyncOperation.allowSceneActivation = false;
        await UniTask.WaitUntil(() => asyncOperation.progress == 0.9f); // Kudos to Unity devs, best API design ever
        IsLoaded = true;
    }

    public async void Activate()
    {
        Debug.Log($"[SceneLoader] Changing scene from {currentScene} to {scene}");
        if (asyncOperation == null) await UniTask.WaitUntil(() => asyncOperation != null);
        asyncOperation.allowSceneActivation = true;
        await UniTask.WaitUntil(() => asyncOperation.isDone);
        Context.PostSceneChanged.Invoke(currentScene, scene);
    }
}

public class PreSceneChangedEvent : UnityEvent<string, string>
{
}

public class PostSceneChangedEvent : UnityEvent<string, string>
{
}