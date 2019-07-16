#region Header
/* ============================================ 
 *	작성자 : KJH
   ============================================ */
#endregion Header

using System;

public interface IAutoInjectable { }

namespace AutoInjector
{
	public class BoolBase : Attribute, IAutoInjectable
	{
		public readonly bool @bool;

		public BoolBase(bool @bool)
		{
			this.@bool = @bool;
		}
	}

	public class NameBase : Attribute, IAutoInjectable
	{
		public readonly string name;

		public NameBase(string componentName)
		{
			this.name = componentName;
		}

		public string Trim(string varName)
		{
			string trim = name;
			if (string.IsNullOrEmpty(trim))
				trim = varName.TrimMemberVarName();

			return trim;
		} 
	}
}


[AttributeUsage(AttributeTargets.Field)] public class GetComponentAttribute : Attribute, IAutoInjectable { }

[AttributeUsage(AttributeTargets.Field)] public class GetComponentInParentAttribute : AutoInjector.BoolBase {
	public GetComponentInParentAttribute(bool includeInActive = false) : base(includeInActive) { }
}
[AttributeUsage(AttributeTargets.Field)] public class GetComponentInChildrenAttribute : AutoInjector.BoolBase {
	public GetComponentInChildrenAttribute(bool includeInActive = false) : base(includeInActive) { }
}

[AttributeUsage(AttributeTargets.Field)] public class GetComponentInChildrenNameAttribute : AutoInjector.NameBase {
	public GetComponentInChildrenNameAttribute(string componentName = null) : base(componentName) { }
}

[AttributeUsage(AttributeTargets.Field)] public class GetComponentInChildrenOnlyAttribute : AutoInjector.BoolBase {
	public GetComponentInChildrenOnlyAttribute(bool includeInDepth = true) : base(includeInDepth) { }
}


[AttributeUsage(AttributeTargets.Field)] public class FindGameObjectAttribute : AutoInjector.NameBase {
	public FindGameObjectAttribute(string gameObjectName) : base(gameObjectName) { }
}
[AttributeUsage(AttributeTargets.Field)] public class FindGameObjectWithTagAttribute : AutoInjector.NameBase {
	public FindGameObjectWithTagAttribute(string tagName) : base(tagName) { }
}
[AttributeUsage(AttributeTargets.Field)] public class FindObjectOfTypeAttribute : Attribute, IAutoInjectable { }