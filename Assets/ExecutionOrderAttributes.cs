using System;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public class ExecutionOrderAttribute : System.Attribute
{
	public int order;

	public ExecutionOrderAttribute(int order)
	{
		this.order = order;
	}
}

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public class ExecuteAfterAttribute : System.Attribute
{
	public Type targetType;
	public int orderDiff;

	public ExecuteAfterAttribute(Type targetType)
	{
		this.targetType = targetType;
		this.orderDiff = 10;
	}
}

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public class ExecuteBeforeAttribute : System.Attribute
{
	public Type targetType;
	public int orderDiff;

	public ExecuteBeforeAttribute(Type targetType)
	{
		this.targetType = targetType;
		this.orderDiff = 10;
	}
}
