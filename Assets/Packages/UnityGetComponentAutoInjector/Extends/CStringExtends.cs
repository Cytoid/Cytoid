#region Header
/* ============================================ 
 *	작성자 : KJH
   ============================================ */
#endregion Header

using System.Text;

public static partial class CStringExtends
{
	private static readonly StringBuilder _builder = new StringBuilder(1024);

	public static string GetGCSafeString(params string[] appends)
	{
		_builder.Length = 0;

		int len = appends.Length;
		for (int i = 0; i < len; i++)
			_builder.Append(appends[i]);

		return _builder.ToString();
	}

	public static string GetGCSafeString(params object[] appends)
	{
		_builder.Length = 0;

		int len = appends.Length;
		for (int i = 0; i < len; i++)
			_builder.Append(appends[i]);

		return _builder.ToString();
	}

	public static bool EqualsLower(this string x, string y)
	{
		return x.ToLower().Equals(y.ToLower());
	}

	public static string TrimMemberVarName(this string value)
	{
		if (value.StartsWith("m_"))
			value = value.TrimStart('m', '_');

		else if (value.StartsWith("_"))
			value = value.TrimStart('_');

		return value;
	}
}