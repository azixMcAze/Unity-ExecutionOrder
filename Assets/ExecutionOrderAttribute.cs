using System;

[AttributeUsage(AttributeTargets.Class)]
public class ExecutionOrderAttribute : System.Attribute
{
	public int order
	{
		get;
		private set;
	}

	public ExecutionOrderAttribute(int order)
	{
		this.order = order;
	}
}
