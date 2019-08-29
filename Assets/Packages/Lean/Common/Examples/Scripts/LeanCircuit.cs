using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;

namespace Lean.Common.Examples
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(LeanCircuit))]
	public class LeanCircuit_Inspector : LeanInspector<LeanCircuit>
	{
		private int currentPath;

		protected override void DrawInspector()
		{
			if (Target.Paths != null)
			{
				currentPath = EditorGUILayout.IntSlider(currentPath, 0, Target.Paths.Count - 1);
			}

			EditorGUILayout.Separator();

			Draw("LineRadius");
			Draw("PointRadius");
			Draw("ShadowColor");
			Draw("ShadowOffset");

			EditorGUILayout.Separator();

			Draw("Paths");

			Target.UpdateMesh();
		}

		protected override void DrawScene()
		{
			var dirty  = false;
			var matrix = Target.transform.localToWorldMatrix;

			Undo.RecordObject(Target, "Points Changed");

			if (Target.Paths != null && currentPath >= 0 && currentPath < Target.Paths.Count)
			{
				var path = Target.Paths[currentPath];

				if (path.Points != null)
				{
					Handles.matrix = matrix;

					Handles.BeginGUI();
					{
						for (var i = 0; i < path.Points.Count; i++)
						{
							var point     = path.Points[i];
							var pointName = "Point " + i;
							var scrPoint  = Camera.current.WorldToScreenPoint(matrix.MultiplyPoint(point));
							var rect      = new Rect(0.0f, 0.0f, 50.0f, 20.0f); rect.center = new Vector2(scrPoint.x, UnityEngine.Screen.height - scrPoint.y - 35.0f);
							var rect1     = rect; rect.x += 1.0f;
							var rect2     = rect; rect.x -= 1.0f;
							var rect3     = rect; rect.y += 1.0f;
							var rect4     = rect; rect.y -= 1.0f;

							GUI.Label(rect1, pointName, EditorStyles.miniBoldLabel);
							GUI.Label(rect2, pointName, EditorStyles.miniBoldLabel);
							GUI.Label(rect3, pointName, EditorStyles.miniBoldLabel);
							GUI.Label(rect4, pointName, EditorStyles.miniBoldLabel);
							GUI.Label(rect, pointName, EditorStyles.whiteMiniLabel);
						}

						for (var i = 1; i < path.Points.Count; i++)
						{
							var pointA   = path.Points[i - 1];
							var pointB   = path.Points[i];
							var midPoint = (pointA + pointB) * 0.5f;
							var scrPoint = Camera.current.WorldToScreenPoint(matrix.MultiplyPoint(midPoint));
				
							if (GUI.Button(new Rect(scrPoint.x - 5.0f, UnityEngine.Screen.height - scrPoint.y - 45.0f, 20.0f, 20.0f), "+") == true)
							{
								path.Points.Insert(i, midPoint); dirty = true;
							}
						}
					}
					Handles.EndGUI();

					for (var i = 0; i < path.Points.Count; i++)
					{
						var oldPoint = path.Points[i];
						var newPoint = Handles.PositionHandle(oldPoint, Quaternion.identity);

						if (oldPoint != newPoint)
						{
							newPoint.x = Mathf.Round(newPoint.x);
							newPoint.y = Mathf.Round(newPoint.y);
							newPoint.z = Mathf.Round(newPoint.z);

							path.Points[i] = newPoint; dirty = true;
						}
					}
				}
			}

			if (dirty == true)
			{
				EditorUtility.SetDirty(Target);
			}
		}
	}
}
#endif

namespace Lean.Common.Examples
{
	/// <summary>This component generates a basic circuit mesh based on the specified paths, with circles at the end of each path, unless they intersect another.</summary>
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(MeshFilter))]
	[AddComponentMenu("")]
	public class LeanCircuit : MonoBehaviour
	{
		[System.Serializable]
		public class Path
		{
			public List<Vector3> Points;
		}

		class Node
		{
			public Vector3 Point;
			public int     Count;

			public bool Increment(Vector3 p)
			{
				if (Point == p)
				{
					Count += 1;

					return true;
				}

				return false;
			}
		}

		public List<Path> Paths;

		public float LineRadius = 0.2f;

		public float PointRadius = 0.5f;

		public Color ShadowColor = Color.black;

		public Vector3 ShadowOffset = Vector3.right;

		[System.NonSerialized]
		private MeshFilter cachedMeshFilter;

		[System.NonSerialized]
		private bool cachedMeshFilterSet;

		[System.NonSerialized]
		private Mesh mesh;

		private static List<Vector3> positions = new List<Vector3>();

		private static List<Vector3> normals = new List<Vector3>();

		private static List<Color> colors = new List<Color>();

		private static List<Vector2> coords = new List<Vector2>();

		private static List<int> indices = new List<int>();

		private static List<Node> nodes = new List<Node>();

		[ContextMenu("Update Mesh")]
		public void UpdateMesh()
		{
			if (cachedMeshFilterSet == false)
			{
				cachedMeshFilter    = GetComponent<MeshFilter>();
				cachedMeshFilterSet = true;
			}

			if (mesh == null)
			{
				mesh = new Mesh();
#if UNITY_EDITOR
				mesh.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
#endif
				mesh.name = "Circuit";

				cachedMeshFilter.sharedMesh = mesh;
			}

			positions.Clear();
			normals.Clear();
			colors.Clear();
			coords.Clear();
			indices.Clear();
			nodes.Clear();

			if (Paths != null)
			{
				Populate();
			}

			mesh.Clear();
			mesh.SetVertices(positions);
			mesh.SetColors(colors);
			mesh.SetNormals(normals);
			mesh.SetUVs(0, coords);
			mesh.SetTriangles(indices, 0);
		}

		private void Populate()
		{
			// Write shadows
			foreach (var path in Paths)
			{
				if (path.Points != null)
				{
					for (var j = 1; j < path.Points.Count; j++)
					{
						var pointA = path.Points[j - 1];
						var pointB = path.Points[j];

						AddNode(pointA);
						AddNode(pointB);

						AddLine(ShadowOffset + pointA, ShadowOffset + pointB, ShadowColor);
					}
				}
			}

			foreach (var node in nodes)
			{
				if (node.Count == 1)
				{
					AddPoint(node.Point + ShadowOffset, PointRadius, ShadowColor);
				}
				else
				{
					AddPoint(node.Point + ShadowOffset, LineRadius, ShadowColor);
				}
			}

			// Write main
			foreach (var path in Paths)
			{
				if (path.Points != null)
				{
					for (var j = 1; j < path.Points.Count; j++)
					{
						var pointA = path.Points[j - 1];
						var pointB = path.Points[j];

						AddLine(pointA, pointB, Color.white);
					}
				}
			}

			foreach (var node in nodes)
			{
				if (node.Count == 1)
				{
					AddPoint(node.Point, PointRadius, Color.white);
				}
				else
				{
					AddPoint(node.Point, LineRadius, Color.white);
				}
			}
		}

		protected virtual void Start()
		{
			UpdateMesh();
		}
#if UNITY_EDITOR
		protected virtual void OnValidate()
		{
			if (mesh != null)
			{
				UpdateMesh();
			}
		}
#endif
		private void AddLine(Vector3 a, Vector3 b, Color color)
		{
			if (a != b)
			{
				var right = Vector3.Cross(a - b, Vector3.up).normalized * LineRadius;
				var index = positions.Count;

				positions.Add(a - right);
				positions.Add(a + right);
				positions.Add(b + right);
				positions.Add(b - right);

				colors.Add(color);
				colors.Add(color);
				colors.Add(color);
				colors.Add(color);

				normals.Add(Vector3.up);
				normals.Add(Vector3.up);
				normals.Add(Vector3.up);
				normals.Add(Vector3.up);

				coords.Add(Vector2.zero);
				coords.Add(Vector2.one);
				coords.Add(Vector2.one);
				coords.Add(Vector2.zero);

				indices.Add(index + 2);
				indices.Add(index + 1);
				indices.Add(index    );
				
				indices.Add(index + 3);
				indices.Add(index + 2);
				indices.Add(index    );
			}
		}

		private void AddPoint(Vector3 a, float radius, Color color)
		{
			var index = positions.Count;
			var count = 36;
			var step  = Mathf.PI * 2.0f / count;

			for (var i = 0; i < count; i++)
			{
				var angle = i * step;

				positions.Add(a + new Vector3(Mathf.Sin(angle) * radius, 0.0f, Mathf.Cos(angle) * radius));

				colors.Add(color);

				normals.Add(Vector3.up);

				coords.Add(new Vector2(0.5f, 0.5f));
			}

			for (var i = 2; i < count; i++)
			{
				indices.Add(index    );
				indices.Add(index + i - 1);
				indices.Add(index + i);
			}
		}

		private void AddNode(Vector3 point)
		{
			for (var i = nodes.Count - 1; i >= 0; i--)
			{
				var node = nodes[i];

				if (node.Increment(point) == true)
				{
					return;
				}
			}

			var addNode = new Node();

			addNode.Point = point;
			addNode.Count = 1;

			nodes.Add(addNode);
		}
	}
}