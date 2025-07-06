using System.Collections;
using UnityEngine;

namespace Lean.Common
{
	/// <summary>This class contains some useful methods used by this asset.</summary>
	public static partial class LeanCommon
	{
		public const string HelpUrlPrefix = "https://carloswilkes.com/Documentation/LeanCommon#";

		public const string PlusHelpUrlPrefix = "https://carloswilkes.com/Documentation/LeanCommonPlus#";

		public const string ComponentPathPrefix = "Lean/Common/Lean ";

		public static Vector2 Hermite(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
		{
			var mu2 = t * t;
			var mu3 = mu2 * t;
			var x   = HermiteInterpolate(a.x, b.x, c.x, d.x, t, mu2, mu3);
			var y   = HermiteInterpolate(a.y, b.y, c.y, d.y, t, mu2, mu3);

			return new Vector2(x, y);
		}

		private static float HermiteInterpolate(float y0, float y1, float y2,float y3, float mu, float mu2, float mu3)
		{
			var m0 = (y1 - y0) * 0.5f + (y2 - y1) * 0.5f;
			var m1 = (y2 - y1) * 0.5f + (y3 - y2) * 0.5f;
			var a0 =  2.0f * mu3 - 3.0f * mu2 + 1.0f;
			var a1 =         mu3 - 2.0f * mu2 + mu;
			var a2 =         mu3 -        mu2;
			var a3 = -2.0f * mu3 + 3.0f * mu2;

			return a0 * y1 + a1 * m0 + a2 * m1 + a3 * y2;
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common
{
	using UnityEditor;

	public static partial class LeanCommon
	{
		/// <summary>This method gives you the actual object behind a SerializedProperty given to you by a property drawer.</summary>
		public static T GetObjectFromSerializedProperty<T>(object target, SerializedProperty property)
		{
			var tokens = property.propertyPath.Replace(".Array.data[", ".[").Split('.');

			for (var i = 0; i < tokens.Length; i++)
			{
				var token = tokens[i];
				var type  = target.GetType();

				if (target is IList)
				{
					var list  = (IList)target;
					var index = int.Parse(token.Substring(1, token.Length - 2));

					target = list[index];
				}
				else
				{
					while (type != null)
					{
						var field = type.GetField(token, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

						if (field != null)
						{
							target = field.GetValue(target);

							break;
						}

						type = type.BaseType;
					}
				}
			}

			return (T)target;
		}
	}
}
#endif