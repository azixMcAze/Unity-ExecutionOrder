using System;

[AttributeUsage(AttributeTargets.Class)]
public class ExecutionOrderAttribute : System.Attribute
{
	public int order { get; private set; }

	public ExecutionOrderAttribute(int order)
	{
		this.order = order;
	}
}

[AttributeUsage(AttributeTargets.Class)]
public class ExecuteAfterAttribute : System.Attribute
{
	public Type type { get; private set; }
	public int orderIncrease { get; private set; }

	public ExecuteAfterAttribute(Type type, int orderIncrease = 10)
	{
		this.type = type;
		this.orderIncrease = orderIncrease;
	}
}

[AttributeUsage(AttributeTargets.Class)]
public class ExecuteBeforeAttribute : System.Attribute
{
	public Type type { get; private set; }
	public int orderDecrease { get; private set; }

	public ExecuteBeforeAttribute(Type type, int orderDecrease = 10)
	{
		this.type = type;
		this.orderDecrease = orderDecrease;
	}
}
