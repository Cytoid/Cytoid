#region Header
/* ============================================ 
 *	작성자 : KJH
   ============================================ */
#endregion Header

#if UNITY_EDITOR

using System;
using System.Reflection;
using System.Collections.Generic;

public static partial class CReflectExtends
{
	private class FieldInfoComparer : IEqualityComparer<FieldInfo>
	{
		public bool Equals(FieldInfo x, FieldInfo y)
		{
			return (x.DeclaringType == y.DeclaringType) && (x.Name == y.Name);
		}

		public int GetHashCode(FieldInfo fieldInfo)
		{
			return (fieldInfo.Name.GetHashCode() ^ fieldInfo.DeclaringType.GetHashCode());
		}
	}

	private static readonly FieldInfoComparer _fieldInfoComparer = new FieldInfoComparer();

	public static FieldInfo[] GetFieldInfoWithBaseClass(this Type type, BindingFlags flags)
	{
		FieldInfo[] fieldInfos = type.GetFields(flags);

		Type objectType = typeof(object);

		if (type.BaseType == objectType)
			return fieldInfos;

		Type currentType = type;

		HashSet<FieldInfo> fieldInfosHash = new HashSet<FieldInfo>(fieldInfos, _fieldInfoComparer);
		while (currentType != objectType)
		{
			fieldInfos = currentType.GetFields(flags);

			fieldInfosHash.UnionWith(fieldInfos);

			currentType = currentType.BaseType;
		}

		return fieldInfosHash.ToArray();
	}

	public static object InvokeGeneric(this Type type, object obj, string methodName, Type[] parameterTypes, Type[] argumentTypes, params object[] parameters)
	{
		MethodInfo methodInfo = type.GetMethod(methodName, parameterTypes)
								.MakeGenericMethod(argumentTypes);

		return methodInfo.Invoke(obj, parameters);
	}

	public static object Invoke(this Type type, object obj, string methodName, Type[] parameterTypes, params object[] parameters)
	{
		MethodInfo methodInfo = type.GetMethod(methodName, parameterTypes);

		return methodInfo.Invoke(obj, parameters);
	}
}

#endif