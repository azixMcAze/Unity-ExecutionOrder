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

// [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
// public class ExecuteBeforeAttribute : System.Attribute
// {
// 	public Type targetType { get; private set; }
// 	public int orderDiff { get; private set; }

// 	public ExecuteBeforeAttribute(Type targetType, int orderDiff = 10)
// 	{
// 		this.targetType = targetType;
// 		this.orderDiff = orderDiff;
// 	}
// }
