using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lean.Common
{
	/// <summary>This class contains useful methods used in almost all of my code.</summary>
	public static class LeanHelper
	{
		public const string HelpUrlPrefix = "http://carloswilkes.github.io/Documentation/";

		public const string ComponentPathPrefix = "Lean/";

		/// <summary>This gives you the time-independent 't' value for lerp when used for dampening. This returns 1 in edit mode, or if dampening is less than 0.</summary>
		public static float DampenFactor(float dampening, float elapsed)
		{
			if (dampening < 0.0f)
			{
				return 1.0f;
			}
#if UNITY_EDITOR
			if (Application.isPlaying == false)
			{
				return 1.0f;
			}
#endif
			return 1.0f - Mathf.Exp(-dampening * elapsed);
		}

		/// <summary>This allows you to destroy the target object in game and in edit mode, and it returns null.</summary>
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
#if UNITY_EDITOR
		/// <summary>This gives you the actual object behind a SerializedProperty given to you by a property drawer.</summary>
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
#endif
	}
}