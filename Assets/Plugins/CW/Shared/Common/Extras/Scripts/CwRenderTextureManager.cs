//#define USE_CUSTOM_TEMPORARY
using UnityEngine;
using System.Collections.Generic;

namespace CW.Common
{
	[ExecuteInEditMode]
	[DefaultExecutionOrder(1000)]
	[HelpURL(CwShared.HelpUrlPrefix + "CwRenderTextureManager")]
	[AddComponentMenu(CwShared.ComponentMenuPrefix + "Render Texture Manager")]
	public class CwRenderTextureManager : MonoBehaviour
	{
		/// <summary>This allows you to set how many frames an unused RenderTexture will remaining in memory before it's released.</summary>
		public int Lifetime { set { lifetime = value; } get { return lifetime; } } [SerializeField] private int lifetime = 3;

#if USE_CUSTOM_TEMPORARY
		private class Entry
		{
			public RenderTexture RT;

			public RenderTextureDescriptor Desc;

			public int Life;

			public static Stack<Entry> Pool = new Stack<Entry>();
		}

		private static List<Entry> entries = new List<Entry>();

		private static LinkedList<CwRenderTextureManager> instances = new LinkedList<CwRenderTextureManager>();

		private LinkedListNode<CwRenderTextureManager> node;

		public static RenderTexture GetTemporary(RenderTextureDescriptor desc, string title)
		{
			if (instances.Count == 0)
			{
				new GameObject("CwRenderTextureManager").AddComponent<CwRenderTextureManager>();
			}

			for (var i = entries.Count - 1; i >= 0; i--)
			{
				var entry = entries[i];

				if (entry.RT == null)
				{
					entry.RT = null;

					Entry.Pool.Push(entry);

					entries.RemoveAt(i);

					continue;
				}

				if (Match(ref entry.Desc, ref desc) == true)
				{
					Entry.Pool.Push(entry);

					entries.RemoveAt(i);

					entry.RT.name = title;

					if (entry.RT.IsCreated() == false)
					{
						entry.RT.Create();
					}

					return entry.RT;
				}
			}

			var rt = new RenderTexture(desc);

			rt.name = title;

			return rt;
		}

		public static RenderTexture ReleaseTemporary(RenderTexture rt)
		{
			if (rt != null)
			{
				if (instances.Count > 0)
				{
					var entry = Entry.Pool.Count > 0 ? Entry.Pool.Pop() : new Entry();

					entry.RT    = rt;
					entry.Desc  = rt.descriptor;
					entry.Life  = Mathf.Max(1, instances.First.Value.lifetime);

					entries.Add(entry);

					rt.DiscardContents();
				}
				else
				{
					rt.Release();

					DestroyImmediate(rt);
				}
			}

			return null;
		}

		protected virtual void OnEnable()
		{
			node = instances.AddLast(this);
		}

		protected virtual void OnDisable()
		{
			instances.Remove(node); node = null;

			if (instances.Count == 0)
			{
				for (var i = entries.Count - 1; i >= 0; i--)
				{
					var entry = entries[i];

					if (entry.RT != null)
					{
						entry.RT.Release();

						DestroyImmediate(entry.RT);
					}

					Entry.Pool.Push(entry);
				}

				entries.Clear();
			}
		}

		protected virtual void LateUpdate()
		{
			if (node == instances.First)
			{
				Tick();
			}
		}

		private void Tick()
		{
			for (var i = entries.Count - 1; i >= 0; i--)
			{
				var entry = entries[i];

				if (entry.Life > 0)
				{
					entry.Life -= 1;

					if (entry.Life == 0 && entry.RT != null && entry.RT.IsCreated() == true)
					{
						entry.RT.Release();
					}
				}
			}
		}

		private static bool Match(ref RenderTextureDescriptor a, ref RenderTextureDescriptor b)
		{
			if (a.enableRandomWrite != b.enableRandomWrite) return false;
			if (a.autoGenerateMips != b.autoGenerateMips) return false;
			if (a.useMipMap != b.useMipMap) return false;
			if (a.memoryless != b.memoryless) return false;
			if (a.flags != b.flags) return false;
			if (a.vrUsage != b.vrUsage) return false;
			if (a.shadowSamplingMode != b.shadowSamplingMode) return false;
			if (a.dimension != b.dimension) return false;
			if (a.depthBufferBits != b.depthBufferBits) return false;
			if (a.stencilFormat != b.stencilFormat) return false;
			if (a.colorFormat != b.colorFormat) return false;
			if (a.bindMS != b.bindMS) return false;
			if (a.graphicsFormat != b.graphicsFormat) return false;
			if (a.mipCount != b.mipCount) return false;
			if (a.volumeDepth != b.volumeDepth) return false;
			if (a.msaaSamples != b.msaaSamples) return false;
			if (a.height != b.height) return false;
			if (a.width != b.width) return false;
			if (a.sRGB != b.sRGB) return false;
			if (a.useDynamicScale != b.useDynamicScale) return false;

			return true;
		}
#else
		public static RenderTexture GetTemporary(RenderTextureDescriptor desc, string title)
		{
			var renderTexture = RenderTexture.GetTemporary(desc);

			// TODO: For some reason RenderTexture.GetTemporary ignores the useMipMap flag?!
			if (renderTexture.useMipMap != desc.useMipMap)
			{
				renderTexture.Release();

				renderTexture.descriptor = desc;

				renderTexture.Create();
			}

			return renderTexture;
		}

		public static RenderTexture ReleaseTemporary(RenderTexture renderTexture)
		{
			if (renderTexture != null)
			{
				renderTexture.DiscardContents();

				RenderTexture.ReleaseTemporary(renderTexture);
			}

			return null;
		}
#endif
	}
}

#if UNITY_EDITOR
namespace CW.Common
{
	using UnityEditor;
	using TARGET = CwRenderTextureManager;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwRenderTextureManager_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("lifetime", "This allows you to set how many frames an unused RenderTexture will remaining in memory before it's released.");
		}
	}
}
#endif