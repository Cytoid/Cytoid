using UnityEngine;
using Lean.Common;

namespace Lean.Touch
{
	/// <summary>This is the base class for all components that need to implement some kind of special logic when selected. You can do this manually without this class, but this makes it much easier.
	/// NOTE: This component will register and unregister the associated LeanSelectable in OnEnable and OnDisable.</summary>
	public abstract class LeanSelectableByFingerBehaviour : MonoBehaviour
	{
		[System.NonSerialized]
		private LeanSelectableByFinger selectable;

		/// <summary>This tells you which LeanSelectable is currently associated with this component.</summary>
		public LeanSelectableByFinger Selectable
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
			Register(GetComponentInParent<LeanSelectableByFinger>());
		}

		/// <summary>This method allows you to manually register the LeanSelectable this component is associated with.</summary>
		public void Register(LeanSelectableByFinger newSelectable)
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
					selectable.OnSelectedSelectFinger.AddListener(OnSelectedSelectFinger);
					selectable.OnSelectedSelectFingerUp.AddListener(OnSelectedSelectFingerUp);
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
				selectable.OnSelectedSelectFinger.RemoveListener(OnSelectedSelectFinger);
				selectable.OnSelectedSelectFingerUp.RemoveListener(OnSelectedSelectFingerUp);
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

		/// <summary>Called when selection begins (finger = the finger that selected this).</summary>
		protected virtual void OnSelectedSelectFinger(LeanSelectByFinger select, LeanFinger finger)
		{
		}

		/// <summary>Called when the selecting finger goes up (finger = the finger that selected this).</summary>
		protected virtual void OnSelectedSelectFingerUp(LeanSelectByFinger select, LeanFinger finger)
		{
		}

		/// <summary>Called when this is deselected, if OnSelectUp hasn't been called yet, it will get called first.</summary>
		protected virtual void OnDeselected(LeanSelect select)
		{
		}
	}
}