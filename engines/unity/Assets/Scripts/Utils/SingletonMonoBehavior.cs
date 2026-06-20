using Cysharp.Threading.Tasks;
using UnityEngine;

public class SingletonMonoBehavior<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get => instance ? instance : instance = FindObjectOfType(typeof(T)) as T;
        set => instance = value;
    }

    protected virtual async void Awake()
    {
        if (instance != null)
        {
            // Wait until it is null
            await UniTask.WaitUntil(() => instance == null);
            if (this == null) return;
        }
        instance = this as T;
    }

    protected virtual void OnDestroy()
    {
        instance = null;
    }
}