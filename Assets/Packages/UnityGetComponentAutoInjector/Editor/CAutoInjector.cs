#region Header
/* ============================================ 
 *	작성자 : KJH
 *	기  능 : 자동으로 스크립트 변수를 주입합니다.
   ============================================ */
#endregion Header

#if UNITY_EDITOR

namespace UnityEditor
{
	using System;
	using UnityEngine;
	using System.Reflection;
	using System.Collections;
	using System.Collections.Generic;

	public static class CAutoInjector
	{
		private static readonly BindingFlags _bindingFlags = (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

		public static void Inject(SerializedObject serializedObject, UnityEngine.Object obj, bool forceInject)
		{
			FieldInfo[] fields = obj.GetType().GetFieldInfoWithBaseClass(_bindingFlags);

			bool isInjected = false;

			int len = fields.Length;
			for (int i = 0; i < len; i++)
			{
				FieldInfo fieldInfo = fields[i];

				Type fieldType = fieldInfo.FieldType;
				Type elementType = fieldType.GetElementType();

				object[] attributes = fieldInfo.GetCustomAttributes(true);

				int lenAttributes = attributes.Length;
				for (int j = 0; j < lenAttributes; j++)
				{
					object attribute = attributes[j];
					if ((attribute is IAutoInjectable) == false) continue;

					string variableName = fieldInfo.Name;
					SerializedProperty property = serializedObject.FindProperty(variableName);

					object componentOut = null;

					if (fieldType.IsArray)
					{
						if (forceInject == false && property.arraySize > 0) continue;

						if (IsGetComponentsAttribute(obj, attribute, fieldInfo, elementType, out componentOut))
						{
							Array array = (componentOut as Array);

							int length = array.Length;
							property.arraySize = length;

							for (int k = 0; k < length; k++)
							{
								SerializedProperty prop = property.GetArrayElementAtIndex(k);
								prop.objectReferenceValue = (array.GetValue(k) as UnityEngine.Object);
							}

							if (length == 0)
								componentOut = null;
						}
						else
							LogToInjectionFailed(obj, attribute, fieldInfo);
					}
					else if (fieldType.IsGenericType)
					{
						if (fieldType.GetGenericTypeDefinition() == typeof(List<>))
						{
							if (forceInject == false && property.arraySize > 0) continue;

							if (IsGetComponentsAttribute(obj, attribute, fieldInfo, fieldType.GetGenericArguments()[0], out componentOut))
							{
								ICollection collection = (componentOut as ICollection);

								property.arraySize = collection.Count;

								int length = 0;
								var iter = collection.GetEnumerator();
								while (iter.MoveNext())
								{
									object current = iter.Current;

									SerializedProperty prop = property.GetArrayElementAtIndex(length);
									prop.objectReferenceValue = (current as UnityEngine.Object);

									length++;
								}

								if (length == 0)
									componentOut = null;
							}
							else
								LogToInjectionFailed(obj, attribute, fieldInfo);
						}
					}
					else
					{
						if (forceInject == false && property.objectReferenceValue.HasValue()) continue;

						property.objectReferenceValue = null;

						if (IsGetComponentAttribute(obj, attribute, fieldInfo, fieldType, out componentOut))
						{
							property.objectReferenceValue = (componentOut as UnityEngine.Object);

							if (property.objectReferenceValue.HasValue() == false)
								LogToInjectionFailed(obj, attribute, fieldInfo);
						}
						else
							LogToInjectionFailed(obj, attribute, fieldInfo);
					}

					if (isInjected == false && componentOut.HasValue())
						isInjected = true;
				}
			}

			if (isInjected) LogToInjectionComplete(obj);
		}

		private static bool IsGetComponentsAttribute(UnityEngine.Object obj, object attribute, FieldInfo fieldInfo, Type elementType, out object componentsOut)
		{
			componentsOut = null;

			if (attribute is GetComponentAttribute)
				componentsOut = typeof(MonoBehaviour).InvokeGeneric(obj, "GetComponents", new Type[0], new[] { elementType });

			else if (attribute is GetComponentInChildrenAttribute)
				componentsOut = typeof(MonoBehaviour).InvokeGeneric(obj, "GetComponentsInChildren", new[] { typeof(bool) }, new[] { elementType },
					(attribute as GetComponentInChildrenAttribute).@bool);

			else if (attribute is GetComponentInChildrenOnlyAttribute)
				componentsOut = typeof(CGetComponentExtends).InvokeGeneric(obj, "GetComponentsInChildrenOnly", new[] { typeof(Component), typeof(bool) }, new[] { elementType }, obj,
					(attribute as GetComponentInChildrenOnlyAttribute).@bool);


			else if (attribute is GetComponentInParentAttribute)
				componentsOut = typeof(MonoBehaviour).InvokeGeneric(obj, "GetComponentsInParent", new[] { typeof(bool) }, new[] { elementType },
					(attribute as GetComponentInParentAttribute).@bool);

			else if (attribute is FindGameObjectWithTagAttribute)
				componentsOut = typeof(GameObject).Invoke(obj, "FindGameObjectsWithTag", new[] { typeof(string) },
					(attribute as FindGameObjectWithTagAttribute).Trim(fieldInfo.Name));

			else if (attribute is FindObjectOfTypeAttribute)
				componentsOut = typeof(UnityEngine.Object).Invoke(obj, "FindObjectsOfType", new[] { typeof(Type) }, elementType);

			return componentsOut.HasValue();
		}

		private static bool IsGetComponentAttribute(UnityEngine.Object obj, object attribute, FieldInfo fieldInfo, Type fieldType, out object componentOut)
		{
			componentOut = null;

			if (attribute is GetComponentAttribute)
				componentOut = typeof(CGetComponentExtends).Invoke(obj, "GetComponent", new[] { typeof(Component), typeof(Type) }, obj, fieldType);

			else if (attribute is GetComponentInParentAttribute)
				componentOut = typeof(MonoBehaviour).Invoke(obj, "GetComponentInParent", new[] { typeof(Type) }, fieldType);

			else if (attribute is GetComponentInChildrenAttribute)
				componentOut = typeof(MonoBehaviour).Invoke(obj, "GetComponentInChildren", new[] { typeof(Type), typeof(bool) }, fieldType, true);

			else if (attribute is GetComponentInChildrenOnlyAttribute)
				componentOut = typeof(CGetComponentExtends).Invoke(obj, "GetComponentInChildrenOnly", new[] { typeof(Component), typeof(Type), typeof(bool) }, obj, fieldType,
					(attribute as GetComponentInChildrenOnlyAttribute).@bool);

			else if (attribute is GetComponentInChildrenNameAttribute)
				componentOut = typeof(CGetComponentExtends).Invoke(obj, "GetComponentInChildrenName", new[] { typeof(Component), typeof(Type), typeof(string) }, obj, fieldType,
					(attribute as GetComponentInChildrenNameAttribute).Trim(fieldInfo.Name));


			else if (attribute is FindGameObjectAttribute)
				componentOut = typeof(GameObject).Invoke(obj, "Find", new[] { typeof(string) },
					(attribute as FindGameObjectAttribute).Trim(fieldInfo.Name));

			else if (attribute is FindGameObjectWithTagAttribute)
				componentOut = typeof(GameObject).Invoke(obj, "FindGameObjectWithTag", new[] { typeof(string) },
					(attribute as FindGameObjectWithTagAttribute).Trim(fieldInfo.Name));

			else if (attribute is FindObjectOfTypeAttribute)
				componentOut = typeof(UnityEngine.Object).Invoke(obj, "FindObjectOfType", new[] { typeof(Type) }, fieldType);
			
			return componentOut.HasValue();
		}

		private static void LogToInjectionFailed(UnityEngine.Object obj, object attribute, FieldInfo fieldInfo)
		{
			CDebug.Log(obj, "<b><i>", obj, " <color=red>Auto Injection Failed!</color></i></b>\n",
					  "Click here for more details.\n\n",
					  "<color=#569cd6>class</color> <b><color=#40a591>", obj.GetType(), "</color></b> or base class\n",
					  "{\n",
					  "      <b><color=#40a591>[", attribute.ToString().Replace("Attribute", ""), "]</color></b>\n",
					  "      <b><color=#40a591>", fieldInfo.FieldType.Name, "</color></b> ", fieldInfo.Name, ";   <b><color=red>Failed!</color></b>\n",
					  "}\n");
		}

		private static void LogToInjectionComplete(UnityEngine.Object obj)
		{
			CDebug.Log(obj, "<b><i>", obj, " <color=green>Auto injection complete.</color></i></b>");
		}

		private static bool HasValue(this object referenceType)
		{
			if (referenceType == null) return false;

			if (referenceType.ToString().Equals("null")) return false;

			return true;
		}
	}
}

#endif