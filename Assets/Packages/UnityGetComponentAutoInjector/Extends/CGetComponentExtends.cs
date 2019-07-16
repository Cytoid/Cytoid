#region Header
/* ============================================ 
 *	작성자 : KJH
   ============================================ */
#endregion Header

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static partial class CGetComponentExtends
{
	public static UnityEngine.Object GetComponent(this Component component, Type type)
	{
		if (IsGameObjectType(type))
			return component.gameObject;

		return component.GetComponent(type);
	}

	public static bool GetComponentInChildrenOnly<T>(this Component component, out T objOut, bool includeInDepth = true)
		where T : UnityEngine.Object
	{
		objOut = component.GetComponentInChildrenOnly<T>();

		return (objOut != null);
	}

	public static T GetComponentInChildrenOnly<T>(this Component component, bool includeInDepth = true)
		where T : UnityEngine.Object
	{
		return (component.GetComponentInChildrenOnly(typeof(T), includeInDepth) as T);
	}

	public static UnityEngine.Object GetComponentInChildrenOnly(this Component component, Type type, bool includeInDepth = true)
	{
		if (IsGameObjectType(type))
			return component.GetGameObjectInChildrenName(null);

		Component[] components = null;

		if (includeInDepth)
			components = component.GetComponentsInChildren(type, true);
		else
		{
			Transform transform = component.transform;
			int childCount = transform.childCount;
			components = new Component[childCount];

			for (int i = 0; i < childCount; i++)
			{
				Component componentChild = transform.GetChild(i).GetComponent(type);
				if (componentChild != null)
					components[i] = componentChild;
				else
					return null;
			}
		}

		int len = components.Length;
		for (int i = 0; i < len; i++)
		{
			Component compo = components[i];
			if (compo.transform == component.transform) continue;

			return compo;
		}

		return null;
	}

	public static UnityEngine.Object GetComponentInChildrenName(this Component component, Type type, string objectName = null)
	{
		if (IsGameObjectType(type))
			return component.GetGameObjectInChildrenName(objectName);

		Component[] components = component.GetComponentsInChildren(type, true);
		if (components == null) return null;

		int len = components.Length;
		for (int i = 0; i < len; i++)
		{
			Component compo = components[i];

			string currentName = compo.name;

			if (string.IsNullOrEmpty(objectName))
				return compo;

			else if (currentName.EqualsLower(objectName))
				return compo;
		}

		return null;
	}

	public static GameObject GetGameObjectInChildrenName(this Component component, string objectName = null)
	{
		Transform[] transforms = component.GetComponentsInChildrenOnly<Transform>();
		if (transforms == null) return null;

		int len = transforms.Length;
		for (int i = 0; i < len; i++)
		{
			Transform transform = transforms[i];
			GameObject gameObject = transform.gameObject;

			string currentName = transform.name;

			if (string.IsNullOrEmpty(objectName))
				return gameObject;

			else if (currentName.EqualsLower(objectName))
				return gameObject;
		}

		return null;
	}

	public static T[] GetComponentsInChildrenOnly<T>(this Component component, bool includeInDepth = true)
		where T : Component
	{
		T[] components = component.GetComponentsInChildren<T>(true);
		if (components == null) return null;

		int len = components.Length;

		List<T> newComponentList = null;

		Transform componentTrans = component.transform;

		for (int i = 0; i < len; i++)
		{
			T currentComponent = components[i];
			Transform currentTrans = currentComponent.transform;

			if (currentTrans == componentTrans) continue;

			if (includeInDepth == false && currentTrans.parent != componentTrans) continue;

			if (newComponentList == null)
				newComponentList = new List<T>();

			newComponentList.Add(currentComponent);
		}

		if (newComponentList == null) return null;

		return newComponentList.ToArray();
	}

	private static bool IsGameObjectType(Type type)
	{
		return (type == typeof(GameObject));
	}
}