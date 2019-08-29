using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Coffee.UIExtensions
{
	internal class UIVertexUtil
	{

		//################################
		// Const/Readonly Static Members.
		//################################
		public static readonly List<UIVertex> s_InputVerts = new List<UIVertex> ();
		public static readonly List<UIVertex> s_OutputVerts = new List<UIVertex> ();
		public static readonly UIVertex[] s_QuadVerts = new UIVertex[4];


		public static void AddQuadToStream(UIVertex[] quad, List<UIVertex> stream)
		{
			stream.Add(quad[0]);
			stream.Add(quad[1]);
			stream.Add(quad[2]);
			stream.Add(quad[2]);
			stream.Add(quad[3]);
			stream.Add(quad[0]);
		}

		public static UIVertex Lerp (UIVertex a, UIVertex b, float t)
		{
			UIVertex output = default(UIVertex);
			output.position = Vector3.Lerp (a.position, b.position, t);
			output.normal = Vector3.Lerp (a.normal, b.normal, t);
		
			output.tangent = Vector4.Lerp (a.tangent, b.tangent, t);
		
			output.uv0 = Vector2.Lerp (a.uv0, b.uv0, t);
			output.uv1 = Vector2.Lerp (a.uv1, b.uv1, t);
			output.color = Color.Lerp (a.color, b.color, t);
			return output;
		}

		public static UIVertex Bilerp (UIVertex bottomLeft, UIVertex topLeft, UIVertex topRight, UIVertex bottomRight, float a, float b)
		{
			UIVertex output = default(UIVertex);
			output.position = Bilerp (bottomLeft.position, topLeft.position, topRight.position, bottomRight.position, a, b);
			output.normal = Bilerp(bottomLeft.normal, topLeft.normal, topRight.normal, bottomRight.normal, a, b);
			output.tangent = Bilerp(bottomLeft.tangent, topLeft.tangent, topRight.tangent, bottomRight.tangent, a, b);
			output.uv0 = Bilerp (bottomLeft.uv0, topLeft.uv0, topRight.uv0, bottomRight.uv0, a, b);
			output.uv1 = Bilerp(bottomLeft.uv1, topLeft.uv1, topRight.uv1, bottomRight.uv1, a, b);
			output.color = Bilerp (bottomLeft.color, topLeft.color, topRight.color, bottomRight.color, a, b);
			return output;
		}

		public static Vector2 Bilerp (Vector2 bottomLeft, Vector2 topLeft, Vector2 topRight, Vector2 bottomRight, float x, float y)
		{
			Vector2 top = Vector2.Lerp (topLeft, topRight, x);
			Vector2 bottom = Vector2.Lerp (bottomLeft, bottomRight, x);
			return Vector2.Lerp (bottom, top, y);
		}

		public static Vector3 Bilerp (Vector3 bottomLeft, Vector3 topLeft, Vector3 topRight, Vector3 bottomRight, float a, float b)
		{
			Vector3 top = Vector3.Lerp (topLeft, topRight, a);
			Vector3 bottom = Vector3.Lerp (bottomLeft, bottomRight, a);
			return Vector3.Lerp (bottom, top, b);
		}

		public static Vector4 Bilerp (Vector4 bottomLeft, Vector4 topLeft, Vector4 topRight, Vector4 bottomRight, float a, float b)
		{
			Vector4 top = Vector4.Lerp (topLeft, topRight, a);
			Vector4 bottom = Vector4.Lerp (bottomLeft, bottomRight, a);
			return Vector4.Lerp (bottom, top, b);
		}

		public static Color Bilerp (Color bottomLeft, Color topLeft, Color topRight, Color bottomRight, float a, float b)
		{
			Color top = Color.Lerp (topLeft, topRight, a);
			Color bottom = Color.Lerp (bottomLeft, bottomRight, a);
			return Color.Lerp (bottom, top, b);
		}
	}
}