using UnityEngine;

namespace Lean.Common
{
	/// <summary>This is the base class for all components that need to implement some kind of special logic when selected. You can do this manually without this class, but this makes it much easier.
	/// NOTE: This component will register and unregister the associated LeanSelectable in OnEnable and OnDisable.</summary>
	public abstract class LeanSelectableBehaviour : MonoBehaviour
	{
		[System.NonSerialized]
		private LeanSelectable selectable;

		/// <summary>This tells you which LeanSelectable is currently associated with this component.</summary>
		public LeanSelectable Selectable
		{
			get
			{
				if (selectable == null)
				{
					Register();
				}

				return selectable;
			}
		}

		/// <summary>This method allows you to manually register the LeanSelectable this component is associated with. This is useful if you're manually spawning and attaching children from code.</summary>
		[ContextMenu("Register")]
		public void Register()
		{
			Register(GetComponentInParent<LeanSelectable>());
		}

		/// <summary>This method allows you to manually register the LeanSelectable this component is associated with.</summary>
		public void Register(LeanSelectable newSelectable)
		{
			if (newSelectable != selectable)
			{
				// Unregister existing
				Unregister();

				// Register a new one?
				if (newSelectable != null)
				{
					selectable = newSelectable;

					selectable.OnSelected.AddListener(OnSelected);
					selectable.OnDeselected.AddListener(OnDeselected);
				}
			}
		}

		/// <summary>This method allows you to manually register the LeanSelectable this component is associated with. This is useful if you're changing the associated LeanSelectable.</summary>
		[ContextMenu("Unregister")]
		public void Unregister()
		{
			if (selectable != null)
			{
				selectable.OnSelected.RemoveListener(OnSelected);
				selectable.OnDeselected.RemoveListener(OnDeselected);

				selectable = null;
			}
		}

		protected virtual void OnEnable()
		{
			Register();
		}

		protected virtual void Start()
		{
			if (selectable == null)
			{
				Register();
			}
		}

		protected virtual void OnDisable()
		{
			Unregister();
		}

		/// <summary>Called when selection begins.</summary>
		protected virtual void OnSelected(LeanSelect select)
		{
		}

		/// <summary>Called when this is deselected.</summary>
		protected virtual void OnDeselected(LeanSelect select)
		{
		}
	}
}