using UnityEngine;

namespace Lean.Touch
{
	/// <summary>This component allows you to spawn a prefab at a point relative to a finger and the specified ScreenDepth.
	/// NOTE: To trigger the prefab spawn you must call the Spawn method on this component from somewhere.</summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanSpawn")]
	public class LeanSpawn : MonoBehaviour
	{
		public enum RotateType
		{
			ThisTransform,
			ScreenDepthNormal
		}

		[Tooltip("The prefab that this component can spawn.")]
		public Transform Prefab;

		[Tooltip("How should the spawned prefab be rotated?")]
		public RotateType RotateTo;

		[Tooltip("The conversion method used to find a world point from a screen point.")]
		public LeanScreenDepth ScreenDepth;

		[Space]
		[Tooltip("This allows you to offset the finger position.")]
		public Vector2 PixelOffset;

		[Tooltip("If you want the pixels to scale based on device resolution, then specify the canvas whose scale you want to use here.")]
		public Canvas PixelScale;

		[Space]
		[Tooltip("This allows you to offset the spawned object position.")]
		public Vector3 WorldOffset;

		[Tooltip("This allows you transform the WorldOffset to be relative to the specified Transform.")]
		public Transform WorldRelativeTo;

		/// <summary>This will spawn Prefab at the specified finger based on the ScreenDepth setting.</summary>
		public virtual void Spawn(LeanFinger finger)
		{
			var instance = default(Transform);

			TrySpawn(finger, ref instance);
		}

		protected bool TrySpawn(LeanFinger finger, ref Transform instance)
		{
			if (Prefab != null && finger != null)
			{
				// Spawn and position
				instance = Instantiate(Prefab);

				UpdateSpawnedTransform(finger, instance);

				// Select?
				var selectable = instance.GetComponent<LeanSelectable>();

				if (selectable != null)
				{
					selectable.Select(finger);
				}

				return true;
			}

			return false;
		}

		protected void UpdateSpawnedTransform(LeanFinger finger, Transform instance)
		{
			// Grab screen position of finger, and optionally offset it
			var screenPoint = finger.ScreenPosition;

			if (PixelScale != null)
			{
				screenPoint += PixelOffset * PixelScale.scaleFactor;
			}
			else
			{
				screenPoint += PixelOffset;
			}

			// Converted screen position to world position, and optionally offset it
			var worldPoint = ScreenDepth.Convert(screenPoint, gameObject, instance);

			if (WorldRelativeTo != null)
			{
				worldPoint += WorldRelativeTo.TransformPoint(WorldOffset);
			}
			else
			{
				worldPoint += WorldOffset;
			}

			// Write position
			instance.position = worldPoint;

			// Write rotation
			switch (RotateTo)
			{
				case RotateType.ThisTransform:
				{
					instance.rotation = transform.rotation;
				}
				break;

				case RotateType.ScreenDepthNormal:
				{
					instance.up = LeanScreenDepth.LastWorldNormal;
				}
				break;
			}
		}
	}
}