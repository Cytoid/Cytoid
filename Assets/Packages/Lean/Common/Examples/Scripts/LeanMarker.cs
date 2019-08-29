using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;

namespace Lean.Common.Examples
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(LeanMarker))]
	public class LeanMarker_Inspector : LeanInspector<LeanMarker>
	{
		protected override void DrawInspector()
		{
			BeginError(Any(t => t.Target == null));
				Draw("target");
			EndError();
		}
	}
}
#endif

namespace Lean.Common.Examples
{
	/// <summary>This component marks the Target object using the current GameObject name.
	/// This allows you to quickly find and access it from anywhere using the LeanMarker.Reference component.</summary>
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[AddComponentMenu("Lean/Common/Lean Marker")]
	public class LeanMarker : MonoBehaviour
	{
		/// <summary>This struct can be added to your custom components, allowing you to quickly find and efficiently access a marked GameObject.</summary>
		public class Reference<T>
			where T : Object
		{
			public Reference(string newName)
			{
				if (string.IsNullOrEmpty(newName) == true)
				{
					throw new System.ArgumentException("Cannot reference a null or empty marker!");
				}

				name = newName;
			}

			protected string name;

			protected bool cached;

			protected T instance;

			public T Instance
			{
				get
				{
					if (cached == false)
					{
						Find();
					}

					return instance;
				}
			}

			protected virtual void Build(LeanMarker marker)
			{
				if (typeof(T) == typeof(GameObject))
				{
					if (marker.target != null)
					{
						if (marker.target is GameObject)
						{
							instance = (T)marker.target; return;
						}
						else if (marker.target is Component)
						{
							instance = (T)(Object)((Component)marker.target).gameObject; return;
						}
					}
					else
					{
						instance = (T)(Object)marker.gameObject; return;
					}
				}
				else if (typeof(T).IsSubclassOf(typeof(Component)))
				{
					if (marker.target != null)
					{
						if (marker.target is T)
						{
							instance = (T)marker.target; return;
						}
						else if (marker.target is GameObject)
						{
							var component = ((GameObject)marker.target).GetComponent<T>();

							if (component != null)
							{
								instance = component; return;
							}
						}
						else if (marker.target is Component)
						{
							var component = ((Component)marker.target).GetComponent<T>();

							if (component != null)
							{
								instance = component; return;
							}
						}
					}
					else
					{
						var component = marker.gameObject.GetComponent<T>();

						if (component != null)
						{
							instance = component; return;
						}
					}
				}
				else if (marker.target != null && marker.target is T)
				{
					instance = (T)marker.target; return;
				}

				throw new System.MissingMemberException();
			}

			protected void Find()
			{
				var marker = default(LeanMarker);

				if (instances.TryGetValue(name, out marker) == true)
				{
					Build(marker);

					return;
				}
				else
				{
					var markers = FindObjectsOfType<LeanMarker>();

					for (var i = markers.Length - 1; i >= 0; i--)
					{
						marker = markers[i];

						if (marker.name == name)
						{
							Build(marker);

							return;
						}
					}
				}

				throw new System.NullReferenceException("Failed to find LeanMarker in scene with name: " + name);
			}
		}

		/// <summary>This stores all active an enables LeanMarker instances by their GameObject name.</summary>
		private static Dictionary<string, LeanMarker> instances = new Dictionary<string, LeanMarker>();

		/// <summary>The marker is pointing to this Object.</summary>
		public Object Target { set { target = value; } get { return target; } } [SerializeField] private Object target;

		[System.NonSerialized]
		private string registeredName;

		protected virtual void OnEnable()
		{
			registeredName = name;

			instances.Add(registeredName, this);
		}

		protected virtual void OnDisable()
		{
			instances.Remove(registeredName);
		}
#if UNITY_EDITOR
		protected virtual void Reset()
		{
			target = gameObject;
		}
#endif
	}
}