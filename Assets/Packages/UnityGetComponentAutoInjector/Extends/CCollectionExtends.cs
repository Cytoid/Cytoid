#region Header
/* ============================================ 
 *	작성자 : KJH
   ============================================ */
#endregion Header

using System.Collections.Generic;

public static partial class CCollectionExtends
{
	public static T[] ToArray<T>(this ICollection<T> collection)
	{
		int count = collection.Count;

		T[] array = new T[count];

		int i = 0;

		var iter = collection.GetEnumerator();
		while (iter.MoveNext())
			array[i++] = iter.Current;

		return array;
	}
}