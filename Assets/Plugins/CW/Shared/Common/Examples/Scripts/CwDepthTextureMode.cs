using UnityEngine;
using CW.Common;

namespace CW.Common
{
	/// <summary>This component allows you to control a Camera component's depthTextureMode setting.</summary>
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Camera))]
	[AddComponentMenu("CW/Common/CW Depth Texture Mode")]
	public class CwDepthTextureMode : MonoBehaviour
	{
		/// <summary>The depth mode that will be applied to the camera.</summary>
		public DepthTextureMode DepthMode { set { depthMode = value; UpdateDepthMode(); } get { return depthMode; } } [SerializeField] private DepthTextureMode depthMode = DepthTextureMode.None;

		[System.NonSerialized]
		private Camera cachedCamera;

		public void UpdateDepthMode()
		{
			if (cachedCamera == null) cachedCamera = GetComponent<Camera>();

			cachedCamera.depthTextureMode = depthMode;
		}

		protected virtual void Update()
		{
			UpdateDepthMode();
		}
	}
}

#if UNITY_EDITOR
namespace CW.Common
{
	using UnityEditor;
	using TARGET = CwDepthTextureMode;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwDepthTextureMode_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("depthMode", "The depth mode that will be applied to the camera.");
		}

		public static void RequireDepth()
		{
			var found = false;

			foreach (var camera in Camera.allCameras)
			{
				var mask = camera.depthTextureMode;

				if (mask == DepthTextureMode.DepthNormals || ((int)mask & 1) != 0)
				{
					found = true; break;
				}
			}

			if (found == false)
			{
				CwEditor.Separator();

				if (Camera.main != null)
				{
					if (WritesDepth(Camera.main) == false)
					{
						if (CwEditor.HelpButton("This component requires your camera to render a Depth Texture, but it doesn't.", UnityEditor.MessageType.Error, "Fix", 50.0f) == true)
						{
							CwHelper.GetOrAddComponent<CwDepthTextureMode>(Camera.main.gameObject).DepthMode = DepthTextureMode.Depth;

							CwHelper.SelectAndPing(Camera.main);
						}
					}
				}
				else
				{
					CwEditor.Error("This component requires your camera to render a Depth Texture, but none of the cameras in your scene do. This can be fixed with the SgtDepthTextureMode component.");

					foreach (var camera in Camera.allCameras)
					{
						if (CwHelper.Enabled(camera) == true)
						{
							CwHelper.GetOrAddComponent<CwDepthTextureMode>(camera.gameObject).DepthMode = DepthTextureMode.Depth;

							CwHelper.SelectAndPing(camera);
						}
					}
				}
			}
		}

		private static bool WritesDepth(Camera camera)
		{
			return camera != null && camera.depthTextureMode == DepthTextureMode.DepthNormals || ((int)camera.depthTextureMode & 1) != 0;
		}
	}
}
#endif