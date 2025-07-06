#if UNITY_2021_3 && !(UNITY_2021_3_0 || UNITY_2021_3_1 || UNITY_2021_3_2 || UNITY_2021_3_3 || UNITY_2021_3_4 || UNITY_2021_3_5 || UNITY_2021_3_6 || UNITY_2021_3_7 || UNITY_2021_3_8 || UNITY_2021_3_9 || UNITY_2021_3_10 || UNITY_2021_3_11 || UNITY_2021_3_12 || UNITY_2021_3_13 || UNITY_2021_3_14 || UNITY_2021_3_15 || UNITY_2021_3_16 || UNITY_2021_3_17)
	#define CW_HAS_NEW_FIND
#elif UNITY_2022_2 && !(UNITY_2022_2_0 || UNITY_2022_2_1 || UNITY_2022_2_2 || UNITY_2022_2_3 || UNITY_2022_2_4)
	#define CW_HAS_NEW_FIND
#elif UNITY_2023_1_OR_NEWER
	#define CW_HAS_NEW_FIND
#endif

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using EventSystem = UnityEngine.EventSystems.EventSystem;

namespace CW.Common
{
	public static partial class CwHelper
	{
		private static Stack<Random.State> seedStates = new Stack<Random.State>();

		public static List<Material> tempMaterials = new List<Material>();

		public static List<MaterialPropertyBlock> tempProperties = new List<MaterialPropertyBlock>();

		public static event System.Action<Camera> OnCameraPreRender;

		public static event System.Action<Camera> OnCameraPostRender;

		private static Stack<RenderTexture> actives = new Stack<RenderTexture>();

		private static int uniqueSeed;

		private static Mesh quadMesh;
		private static bool quadMeshSet;

		static CwHelper()
		{
			Camera.onPreRender += (camera) =>
				{
					if (OnCameraPreRender != null) OnCameraPreRender(camera);
				};

			Camera.onPostRender += (camera) =>
				{
					if (OnCameraPostRender != null) OnCameraPostRender(camera);
				};

			UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += (context, camera) =>
				{
					if (OnCameraPreRender != null) OnCameraPreRender(camera);
				};

			UnityEngine.Rendering.RenderPipelineManager.endCameraRendering += (context, camera) =>
				{
					if (OnCameraPostRender != null) OnCameraPostRender(camera);
				};
		}

		public static bool IsSRP
		{
			get
			{
				return UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
			}
		}

		public static bool IsBIRP
		{
			get
			{
				return UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null;
			}
		}

		public static bool IsURP
		{
			get
			{
				var crp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;

				if (crp != null)
				{
					var title = crp.GetType().ToString();

					if (title.Contains("Universal") == true)
					{
						return true;
					}
				}

				return false;
			}
		}

		public static bool IsHDRP
		{
			get
			{
				var crp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;

				if (crp != null)
				{
					var title = crp.GetType().ToString();

					if (title.Contains("HighDefinition") == true)
					{
						return true;
					}
				}

				return false;
			}
		}

		public static T FindAnyObjectByType<T>(bool includeInactive = false)
			where T : Object
		{
#if CW_HAS_NEW_FIND
			return Object.FindAnyObjectByType<T>(includeInactive == true ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
#else
			return Object.FindObjectOfType<T>(includeInactive);
#endif
		}

		public static T[] FindObjectsByType<T>()
			where T : Object
		{
#if CW_HAS_NEW_FIND
			return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
			return Object.FindObjectsOfType<T>();
#endif
		}

		public static Mesh GetQuadMesh()
		{
			if (quadMeshSet == false)
			{
				var gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);

				quadMeshSet = true;
				quadMesh    = gameObject.GetComponent<MeshFilter>().sharedMesh;

				Object.DestroyImmediate(gameObject);
			}

			return quadMesh;
		}

		public static T GetOrAddComponent<T>(GameObject gameObject, bool recordUndo = true)
			where T : Component
		{
			if (gameObject != null)
			{
				var component = gameObject.GetComponent<T>();

				if (component == null) component = AddComponent<T>(gameObject, recordUndo);

				return component;
			}

			return null;
		}

		public static T AddComponent<T>(GameObject gameObject, bool recordUndo = true)
			where T : Component
		{
			if (gameObject != null)
			{
#if UNITY_EDITOR
					if (Application.isPlaying == true)
					{
						return gameObject.AddComponent<T>();
					}
					else
					{
						if (recordUndo == true)
						{
							return UnityEditor.Undo.AddComponent<T>(gameObject);
						}
						else
						{
							return gameObject.AddComponent<T>();
						}
					}
#else
					return gameObject.AddComponent<T>();
#endif
			}

			return null;
		}

		/// <summary>This will return true if the specified layer index (0..31) is inside the specified layer mask (e.g. from LayerMask).</summary>
		public static bool IndexInMask(int index, int mask)
		{
			return ((1 << index) & mask) != 0;
		}

		public static Camera GetCamera(Camera currentCamera, GameObject gameObject = null)
		{
			if (currentCamera == null)
			{
				if (gameObject != null)
				{
					currentCamera = gameObject.GetComponent<Camera>();
				}

				if (currentCamera == null)
				{
					currentCamera = Camera.main;
				}
			}

			return currentCamera;
		}

		public static Vector3 GetObserverPosition(Transform observer)
		{
			if (observer != null)
			{
				return observer.position;
			}

			var mainCamera = Camera.main;

			if (mainCamera != null)
			{
				return mainCamera.transform.position;
			}

			return Vector3.zero;
		}

		public static bool Enabled(Behaviour b)
		{
			return b != null && b.isActiveAndEnabled == true;
		}

		public static void BeginSeed()
		{
			uniqueSeed += Random.Range(int.MinValue, int.MaxValue);

			BeginSeed(uniqueSeed);
		}

		public static void BeginSeed(int newSeed)
		{
			seedStates.Push(Random.state);

			Random.InitState(newSeed);
		}

		public static void EndSeed()
		{
			Random.state = seedStates.Pop();
		}

		public static Color Brighten(Color color, float brightness, bool convertToGamma = true)
		{
			if (convertToGamma == true)
			{
				color = ToGamma(color);
			}

			color.r *= brightness;
			color.g *= brightness;
			color.b *= brightness;

			return color;
		}

		public static Color Premultiply(Color color)
		{
			color.r *= color.a;
			color.g *= color.a;
			color.b *= color.a;

			return color;
		}

		public static float Saturate(float c)
		{
			if (c >= 0.0f && c <= 1.0f)
			{
				return c;
			}

			return c < 0.5f ? 0.0f : 1.0f;
		}

		public static Color Saturate(Color c)
		{
			c.r = Saturate(c.r);
			c.g = Saturate(c.g);
			c.b = Saturate(c.b);
			c.a = Saturate(c.a);

			return c;
		}

		public static void Resize<T>(List<T> list, int size)
		{
			if (list.Count > size)
			{
				list.RemoveRange(size, list.Count - size);
			}
			else
			{
				list.Capacity = size;

				for (var i = list.Count; i < size; i++)
				{
					list.Add(default(T));
				}
			}
		}

		public static float Sharpness(float a, float p)
		{
			if (p >= 0.0f)
			{
				return Mathf.Pow(a, p);
			}
			else
			{
				return 1.0f - Mathf.Pow(1.0f - a, - p);
			}
		}

		public static Color ToLinear(Color gamma)
		{
			if (QualitySettings.activeColorSpace == ColorSpace.Linear)
			{
				return gamma.linear;
			}

			return gamma;
		}

		public static float ToLinear(float gamma)
		{
			if (QualitySettings.activeColorSpace == ColorSpace.Linear)
			{
				return Mathf.Pow(gamma, 1.0f / 2.2f);
			}

			return gamma;
		}

		public static Color ToGamma(Color linear)
		{
			if (QualitySettings.activeColorSpace == ColorSpace.Linear)
			{
				return linear.gamma;
			}

			return linear;
		}

		public static float ToGamma(float linear)
		{
			if (QualitySettings.activeColorSpace == ColorSpace.Linear)
			{
				return Mathf.Pow(linear, 2.2f);
			}

			return linear;
		}

		public static float UniformScale(Vector3 scale)
		{
			return System.Math.Max(System.Math.Max(scale.x, scale.y), scale.z);
		}

		public static void BeginActive(RenderTexture renderTexture)
		{
			actives.Push(RenderTexture.active);

			RenderTexture.active = renderTexture;
		}

		public static void EndActive()
		{
			RenderTexture.active = actives.Pop();
		}

		public static void SetTempMaterial(Material material)
		{
			tempMaterials.Clear();
			tempProperties.Clear();

			tempMaterials.Add(material);
		}

		public static void SetTempMaterial(Material material1, Material material2)
		{
			tempMaterials.Clear();
			tempProperties.Clear();

			tempMaterials.Add(material1);
			tempMaterials.Add(material2);
		}

		public static void SetTempMaterial(List<Material> materials)
		{
			tempMaterials.Clear();
			tempProperties.Clear();

			if (materials != null)
			{
				tempMaterials.AddRange(materials);
			}
		}

		public static void SetTempMaterial(MaterialPropertyBlock properties)
		{
			tempMaterials.Clear();
			tempProperties.Clear();

			tempProperties.Add(properties);
		}

		private static List<Material> materials = new List<Material>();

		public static void AddMaterial(Renderer r, Material m)
		{
			if (r != null && m != null)
			{
				var sms = r.sharedMaterials;

				materials.Clear();

				foreach (var sm in sms)
				{
					if (sm == m)
					{
						return;
					}
				}

				foreach (var sm in sms)
				{
					if (sm != null)
					{
						materials.Add(sm);
					}
				}

				materials.Add(m);

				r.sharedMaterials = materials.ToArray(); materials.Clear();
			}
		}

		// Prevent applying the same shader material twice
		public static void ReplaceMaterial(Renderer r, Material m)
		{
			if (r != null && m != null)
			{
				var sms = r.sharedMaterials;

				foreach (var sm in sms)
				{
					if (sm == m)
					{
						return;
					}
				}

				foreach (var sm in sms)
				{
					if (sm != null)
					{
						if (sm.shader != m.shader)
						{
							materials.Add(sm);
						}
					}
				}

				materials.Add(m);

				r.sharedMaterials = materials.ToArray(); materials.Clear();
			}
		}

		public static void RemoveMaterial(Renderer r, Material m)
		{
			if (r != null)
			{
				var sms = r.sharedMaterials;

				materials.Clear();

				foreach (var sm in sms)
				{
					if (sm != null && sm != m)
					{
						materials.Add(sm);
					}
				}

				r.sharedMaterials = materials.ToArray(); materials.Clear();
			}
		}
	
		public static Texture2D CreateTempTexture2D(string name, int width, int height, TextureFormat format = TextureFormat.ARGB32, bool mips = false, bool linear = false)
		{
			var texture2D = new Texture2D(width, height, format, mips, linear);

			texture2D.name = name;
			texture2D.hideFlags = HideFlags.DontSave;

			return texture2D;
		}

		public static Material CreateTempMaterial(string materialName, string shaderName)
		{
			var shader = Shader.Find(shaderName);

			if (shader == null)
			{
				Debug.LogError("Failed to find shader: " + shaderName);
			}

			return CreateTempMaterial(materialName, shader);
		}

		public static Material CreateTempMaterial(string materialName, Shader shader)
		{
			var material = new Material(shader);

			material.name      = materialName;
			material.hideFlags = HideFlags.HideAndDontSave;

			return material;
		}

		public static Material CreateTempMaterial(string materialName, Material source)
		{
			var material = new Material(source);

			material.name      = materialName;
			material.hideFlags = HideFlags.HideAndDontSave;

			return material;
		}

		public static T Destroy<T>(T o)
			where T : Object
		{
			if (o != null)
			{
	#if UNITY_EDITOR
				if (Application.isPlaying == true)
				{
					Object.Destroy(o);
				}
				else
				{
					Object.DestroyImmediate(o);
				}
	#else
				Object.Destroy(o);
	#endif
			}

			return null;
		}

		public static GameObject CreateGameObject(string name, int layer, Transform parent = null, string recordUndo = null)
		{
			return CreateGameObject(name, layer, parent, Vector3.zero, Quaternion.identity, Vector3.one, recordUndo);
		}

		public static GameObject CreateGameObject(string name, int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, string recordUndo = null)
		{
			var gameObject = new GameObject(name);

			gameObject.layer = layer;

			gameObject.transform.SetParent(parent, false);

			gameObject.transform.localPosition = localPosition;
			gameObject.transform.localRotation = localRotation;
			gameObject.transform.localScale    = localScale;

#if UNITY_EDITOR
			if (recordUndo != null)
			{
				UnityEditor.Undo.RegisterCreatedObjectUndo(gameObject, recordUndo);
			}
#endif

			return gameObject;
		}

		/// <summary>This method allows you to create a UI element with the specified component and specified parent, with behavior consistent with Unity's built-in UI element creation.</summary>
		public static T CreateElement<T>(Transform parent)
			where T : Component
		{
			var gameObject = new GameObject(typeof(T).Name);

#if UNITY_EDITOR
			UnityEditor.Undo.RegisterCreatedObjectUndo(gameObject, "Create " + typeof(T).Name);
#endif

			var component = gameObject.AddComponent<T>();

			// Auto attach to canvas?
			if (parent == null || parent.GetComponentInParent<Canvas>() == null)
			{
				var canvas = FindAnyObjectByType<Canvas>();

				if (canvas == null)
				{
					canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();

					canvas.gameObject.layer = LayerMask.NameToLayer("UI");

					canvas.renderMode = RenderMode.ScreenSpaceOverlay;

					// Make event system?
					if (EventSystem.current == null)
					{
#if ENABLE_INPUT_SYSTEM && __INPUTSYSTEM__
						new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
#else
						new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
#endif
					}
				}

				parent = canvas.transform;
			}

			gameObject.layer = parent.gameObject.layer;

			component.transform.SetParent(parent, false);

			return component;
		}

		public static float Reciprocal(float v)
		{
			return v != 0.0f ? 1.0f / v : 0.0f;
		}

		public static double Reciprocal(double v)
		{
			return v != 0.0 ? 1.0 / v : 0.0;
		}

		public static float Divide(float a, float b)
		{
			return b != 0.0f ? a / b : 0.0f;
		}

		public static double Divide(double a, double b)
		{
			return b != 0.0 ? a / b : 0.0;
		}

		public static float Acos(float v)
		{
			if (v >= -1.0f && v <= 1.0f)
			{
				return (float)System.Math.Acos(v);
			}

			return 0.0f;
		}

		public static double Acos(double v)
		{
			if (v >= -1.0 && v <= 1.0)
			{
				return System.Math.Acos(v);
			}

			return 0.0f;
		}

		public static float DampenFactor(float speed, float elapsed)
		{
			if (speed < 0.0f)
			{
				return 1.0f;
			}

#if UNITY_EDITOR
			if (Application.isPlaying == false)
			{
				return 1.0f;
			}
#endif

			return 1.0f - Mathf.Pow((float)System.Math.E, -speed * elapsed);
		}

		public static float DampenFactor(float damping, float deltaTime, float linear)
		{
			var factor = DampenFactor(damping, deltaTime);

			return Mathf.Clamp01(factor + linear * deltaTime);
		}

		public static float Atan2(Vector2 xy)
		{
			return Mathf.Atan2(xy.x, xy.y);
		}
		
		public static int Mod(int a, int b)
		{
			var m = a % b;

			if (m < 0)
			{
				return m + b;
			}

			return m;
		}

		public static float Mod(float a, float b)
		{
			var m = a % b;

			if (m < 0.0f)
			{
				return m + b;
			}

			return m;
		}

		public static Texture2D GetReadableCopy(Texture texture, TextureFormat format = TextureFormat.ARGB32, bool mipMaps = false, int width = 0, int height = 0)
		{
			var newTexture = default(Texture2D);

			if (texture != null)
			{
				if (width <= 0)
				{
					width = texture.width;
				}

				if (height <= 0)
				{
					height = texture.height;
				}

				var desc          = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 0);
				var renderTexture = CwRenderTextureManager.GetTemporary(desc, "CwHelper GetReadableCopy");

				newTexture = new Texture2D(width, height, format, mipMaps, false);

				BeginActive(renderTexture);
					Graphics.Blit(texture, renderTexture);

					newTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
				EndActive();

				CwRenderTextureManager.ReleaseTemporary(renderTexture);

				newTexture.Apply();
			}

			return newTexture;
		}
	}
}

#if UNITY_EDITOR
namespace CW.Common
{
	using UnityEditor;

	public static partial class CwHelper
	{
		private static Material cachedShapeOutline;

		private static readonly int _CW_ShapeTex     = Shader.PropertyToID("_CW_ShapeTex");
		private static readonly int _CW_ShapeCoords  = Shader.PropertyToID("_CW_ShapeCoords");
		private static readonly int _CW_ShapeChannel = Shader.PropertyToID("_CW_ShapeChannel");
		private static readonly int _CW_ShapeColor   = Shader.PropertyToID("_CW_ShapeColor");

		public static void DrawShapeOutline(Texture shapeTexture, int shapeChannel, Matrix4x4 shapeMatrix)
		{
			DrawShapeOutline(shapeTexture, shapeChannel, shapeMatrix, new Rect(0, 0, 1, 1), Color.white);
		}

		public static void DrawShapeOutline(Texture shapeTexture, int shapeChannel, Matrix4x4 shapeMatrix, Color color)
		{
			DrawShapeOutline(shapeTexture, shapeChannel, shapeMatrix, new Rect(0, 0, 1, 1), color);
		}

		public static void DrawShapeOutline(Texture shapeTexture, int shapeChannel, Matrix4x4 shapeMatrix, Rect rect, Color color)
		{
			DrawShapeOutline(shapeTexture, shapeChannel, shapeMatrix, new Vector4(rect.xMin, rect.yMin, rect.xMax, rect.yMax), color);
		}

		public static void DrawShapeOutline(Texture shapeTexture, int shapeChannel, Matrix4x4 shapeMatrix, Vector4 coords, Color color)
		{
			if (shapeTexture != null)
			{
				if (cachedShapeOutline == null)
				{
					cachedShapeOutline = CreateTempMaterial("Shape Outline", "Hidden/CW/ShapeOutline");
				}

				var channel = Vector4.zero;

				channel[shapeChannel] = 1.0f;

				cachedShapeOutline.SetTexture(_CW_ShapeTex, shapeTexture);
				cachedShapeOutline.SetVector(_CW_ShapeChannel, channel);
				cachedShapeOutline.SetVector(_CW_ShapeCoords, coords);
				cachedShapeOutline.SetColor(_CW_ShapeColor, color);

				if (cachedShapeOutline.SetPass(0) == true)
				{
					Graphics.DrawMeshNow(GetQuadMesh(), shapeMatrix);
				}
			}
		}

		public static void ClearSelection()
		{
			Selection.objects = new Object[0];
		}

		public static void AddToSelection(Object o)
		{
			var os = new List<Object>(Selection.objects);

			os.Add(o);

			Selection.objects = os.ToArray();
		}

		public static string AssetToGUID<T>(T obj)
			where T : Object
		{
			var path = AssetDatabase.GetAssetPath(obj);

			if (string.IsNullOrEmpty(path) == false)
			{
				return AssetDatabase.AssetPathToGUID(path);
			}

			return null;
		}

		public static T LoadAssetAtGUID<T>(string guid)
			where T : Object
		{
			if (string.IsNullOrEmpty(guid) == false)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);

				if (string.IsNullOrEmpty(path) == false)
				{
					return AssetDatabase.LoadAssetAtPath<T>(path);
				}
			}

			return null;
		}

		public static T LoadFirstAsset<T>(string pattern) // e.g. "Name t:mesh"
			where T : Object
		{
			var guids = AssetDatabase.FindAssets(pattern);

			if (guids.Length > 0)
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[0]);

				return (T)AssetDatabase.LoadAssetAtPath(path, typeof(T));
			}

			return null;
		}

		public static void SelectAndPing(Object o)
		{
			Selection.activeObject = o;

			EditorApplication.delayCall += () => EditorGUIUtility.PingObject(o);
		}

		public static Transform GetSelectedParent()
		{
			if (Selection.activeGameObject != null)
			{
				return Selection.activeGameObject.transform;
			}

			return null;
		}

		public static T GetAssetImporter<T>(Object asset)
			where T : AssetImporter
		{
			return GetAssetImporter<T>((AssetDatabase.GetAssetPath(asset)));
		}

		public static T GetAssetImporter<T>(string path)
			where T : AssetImporter
		{
			return AssetImporter.GetAtPath(path) as T;
		}

		public static void ReimportAsset(Object asset)
		{
			ReimportAsset(AssetDatabase.GetAssetPath(asset));
		}
	
		public static void ReimportAsset(string path)
		{
			AssetDatabase.ImportAsset(path);
		}
	
		public static bool IsAsset(Object o)
		{
			return o != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(o)) == false;
		}

		public static TextureImporter ExportTextureDialog(Texture2D texture2D, string title)
		{
			if (texture2D != null)
			{
				var root = Application.dataPath;
				var path = EditorUtility.SaveFilePanel("Export " + title, root, title, "png");

				if (string.IsNullOrEmpty(path) == false)
				{
					var data = texture2D.EncodeToPNG();

					System.IO.File.WriteAllBytes(path, data);

					Debug.Log("Exported " + title + " Texture to " + path);

					if (path.StartsWith(root) == true)
					{
						var local = path.Substring(root.Length - "Assets".Length);

						AssetDatabase.ImportAsset(local);

						return GetAssetImporter<TextureImporter>(local);
					}
				}
			}

			return null;
		}

		public static AssetImporter ExportAssetDialog(Object asset, string title)
		{
			if (asset != null)
			{
				var root = Application.dataPath;
				var path = EditorUtility.SaveFilePanel("Export " + title, root, title, "asset");

				if (string.IsNullOrEmpty(path) == false)
				{
					if (path.StartsWith(root) == true)
					{
						var local = path.Substring(root.Length - "Assets".Length);

						Debug.Log("Exported " + title + " Asset to " + local);

						var clone = Object.Instantiate(asset);

						AssetDatabase.CreateAsset(clone, local);

						return GetAssetImporter<AssetImporter>(local);
					}
				}
			}

			return null;
		}

		/// <summary>This method creates an empty GameObject prefab at the current asset folder</summary>
		public static GameObject CreatePrefabAsset(string name)
		{
			var gameObject = new GameObject(name);
			var path       = AssetDatabase.GetAssetPath(Selection.activeObject);

			if (string.IsNullOrEmpty(path) == true)
			{
				path = "Assets";
			}

			path = AssetDatabase.GenerateUniqueAssetPath(path + "/" + name + ".prefab");

			var prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, path);

			Object.DestroyImmediate(gameObject);

			Selection.activeObject = prefab;

			return prefab;
		}
	}
}
#endif