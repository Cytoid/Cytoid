
using UniRx.Async;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneLoader
{
    public readonly string CurrentScene;
    public string Scene;
    public AsyncOperation AsyncOperation;

    public SceneLoader(string scene)
    {
        CurrentScene = SceneManager.GetActiveScene().name;
        Scene = scene;
    }

    public async UniTask Load()
    {
        Context.PreSceneChangedEvent.Invoke(CurrentScene, Scene);
        AsyncOperation = SceneManager.LoadSceneAsync(Scene);
        AsyncOperation.allowSceneActivation = false;
        await AsyncOperation;
    }

    public async void Activate()
    {
        if (AsyncOperation == null) await UniTask.WaitUntil(() => AsyncOperation != null);
        AsyncOperation.allowSceneActivation = true;
        await UniTask.WaitUntil(() => AsyncOperation.isDone);
        Context.PostSceneChangedEvent.Invoke(CurrentScene, Scene);
    }
}

public class PreSceneChangedEvent : UnityEvent<string, string>
{
}

public class PostSceneChangedEvent : UnityEvent<string, string>
{
}