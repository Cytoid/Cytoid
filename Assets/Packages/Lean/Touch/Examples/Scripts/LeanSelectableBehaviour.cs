using UnityEngine;

namespace Lean.Touch
{
	/// <summary>This is the base class for all components that need to implement some kind of special logic when selected. You can do this manually without this class, but this makes it much easier.</summary>
	public abstract class LeanSelectableBehaviour : MonoBehaviour
	{
		[System.NonSerialized]
		private LeanSelectable selectable;

		public LeanSelectable Selectable
		{
			get
			{
				if (selectable == null)
				{
					UpdateSelectable();
				}

				return selectable;
			}
		}

		protected virtual void OnEnable()
		{
			UpdateSelectable();

			// Hook LeanSelectable events
			selectable.OnSelect.AddListener(OnSelect);
			selectable.OnSelectUp.AddListener(OnSelectUp);
			selectable.OnDeselect.AddListener(OnDeselect);
		}

		protected virtual void OnDisable()
		{
			UpdateSelectable();

			// Unhook LeanSelectable events
			selectable.OnSelect.RemoveListener(OnSelect);
			selectable.OnSelectUp.RemoveListener(OnSelectUp);
			selectable.OnDeselect.RemoveListener(OnDeselect);
		}

		/// <summary>Called when selection begins (finger = the finger that selected this).</summary>
		protected virtual void OnSelect(LeanFinger finger)
		{
		}

		/// <summary>Called when the selecting finger goes up (finger = the finger that selected this).</summary>
		protected virtual void OnSelectUp(LeanFinger finger)
		{
		}

		/// <summary>Called when this is deselected, if OnSelectUp hasn't been called yet, it will get called first.</summary>
		protected virtual void OnDeselect()
		{
		}

		private void UpdateSelectable()
		{
			if (selectable == null)
			{
				selectable = GetComponentInParent<LeanSelectable>();

				if (selectable == null)
				{
					Debug.LogError("This GameObject or one of its parents must have the LeanSelectable component.", this);
				}
			}
		}
	}
}