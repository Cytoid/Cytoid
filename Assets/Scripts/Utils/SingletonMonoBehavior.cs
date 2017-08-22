using UnityEngine;
 
public class SingletonMonoBehavior<T> : MonoBehaviour where T : MonoBehaviour
{
	
	private static T instance;
 
	public static T Instance
	{
		get { return instance ?? (instance = FindObjectOfType(typeof(T)) as T); }
		set { instance = value; }
	}

	protected virtual void Awake()
	{
		instance = this as T;
	}

	protected virtual void OnDestroy()
	{
		instance = null;
	}
	
}