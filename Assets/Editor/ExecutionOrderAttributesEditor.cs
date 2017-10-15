using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public static class ExecutionOrderAttributeEditor
{
	static class Graph
	{
		public static Dictionary<MonoScript, List<MonoScript>> Create(List<ScriptExecutionOrderDependency> dependencies)
		{
			var graph = new Dictionary<MonoScript, List<MonoScript>>();
			foreach(var dependency in dependencies)
			{
				var source = dependency.firstScript;
				var dest = dependency.secondScript;
				List<MonoScript> edges;
				if(!graph.TryGetValue(source, out edges))
				{
					edges = new List<MonoScript>();
					graph[source] = edges;
				}
				edges.Add(dest);
				if(!graph.ContainsKey(dest))
				{
					graph[dest] = new List<MonoScript>();
				}
			}

			return graph;
		}

		static bool IsCyclicRecursion(Dictionary<MonoScript, List<MonoScript>> graph, MonoScript node, Dictionary<MonoScript, bool> visited, Dictionary<MonoScript, bool> inPath)
		{
			if(!visited[node])
			{
				visited[node] = true;
				inPath[node] = true;

				foreach(var succ in graph[node])
				{
					if(IsCyclicRecursion(graph, succ, visited, inPath))
					{
						inPath[node] = false;
						return true;
					}
				}

				inPath[node] = false;
				return false;
			}
			else if(inPath[node])
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static bool IsCyclic(Dictionary<MonoScript, List<MonoScript>> graph)
		{
			var visited = new Dictionary<MonoScript, bool>();
			var inPath = new Dictionary<MonoScript, bool>();
			foreach(var node in graph.Keys)
			{
				visited[node] = false;
				inPath[node] = false;
			}

			foreach(var node in graph.Keys)
				if(IsCyclicRecursion(graph, node, visited, inPath))
					return true;
			
			return false;
		}

		public static List<MonoScript> GetRoots(Dictionary<MonoScript, List<MonoScript>> graph)
		{
			var degrees = new Dictionary<MonoScript, int>();
			foreach(var node in graph.Keys)
			{
				degrees.Add(node, 0);
			}

			foreach(var kvp in graph)
			{
				var node = kvp.Key;
				var edges = kvp.Value;
				foreach(var succ in edges)
				{
					degrees[succ]++;
				}
			}

			var roots = new List<MonoScript>();
			foreach(var kvp in degrees)
			{
				var node = kvp.Key;
				int degree = kvp.Value;
				if(degree == 0)
					roots.Add(node);
			}
			return roots;
		}

		public static void PropagateValues(Dictionary<MonoScript, List<MonoScript>> graph, Dictionary<MonoScript, int> values, int valueIncrement)
		{
			var queue = new Queue<MonoScript>();

			foreach(var node in values.Keys)
				queue.Enqueue(node);

			while(queue.Count > 0)
			{
				var node = queue.Dequeue();
				int value = values[node] + valueIncrement;

				foreach(var succ in graph[node])
				{
					int prevValue;
					if(!values.TryGetValue(succ, out prevValue) || value > prevValue)
					{
						values[succ] = value;
						queue.Enqueue(succ);
					}
				}
			}
		}
	}

	struct ScriptExecutionOrderDefinition
	{
		public MonoScript script { get; set; }
		public int order { get; set; }
	}

	struct ScriptExecutionOrderDependency
	{
		public MonoScript firstScript { get; set; }
		public MonoScript secondScript { get; set; }
		// public int orderDiff { get; set; }
	}

	static Dictionary<Type, MonoScript> GetTypeDictionary()
	{
		var types = new Dictionary<Type, MonoScript>();

		var scripts = MonoImporter.GetAllRuntimeMonoScripts();
		foreach(var script in scripts)
		{
			var type = script.GetClass();
			if(IsTypeValid(type))
			{
				if(!types.ContainsKey(type))
					types.Add(type, script);
			}
		}

		return types;
	}

	static bool IsTypeValid(Type type)
	{
		if(type != null)
			return type.IsSubclassOf(typeof(MonoBehaviour)) || type.IsSubclassOf(typeof(ScriptableObject));
		else
			return false;
	}

	static List<ScriptExecutionOrderDependency> GetExecutionOrderDependencies(Dictionary<Type, MonoScript> types)
	{
		var list = new List<ScriptExecutionOrderDependency>();

		foreach(var kvp in types)
		{
			var type = kvp.Key;
			var script = kvp.Value;
			if(Attribute.IsDefined(type, typeof(ExecuteAfterAttribute)))
			{
				var attributes = (ExecuteAfterAttribute[])Attribute.GetCustomAttributes(type, typeof(ExecuteAfterAttribute));
				foreach(var attribute in attributes)
				{
					MonoScript targetScript = types[attribute.targetType];
					ScriptExecutionOrderDependency dependency = new ScriptExecutionOrderDependency() { firstScript = targetScript, secondScript = script/*, orderDiff = attribute.orderDiff*/ };
					list.Add(dependency);
				}
			}
			// if(Attribute.IsDefined(type, typeof(ExecuteBeforeAttribute)))
			// {
			// 	var attributes = (ExecuteBeforeAttribute[])Attribute.GetCustomAttributes(type, typeof(ExecuteBeforeAttribute));
			// 	foreach(var attribute in attributes)
			// 	{
			// 		MonoScript targetScript = types[attribute.targetType];
			// 		ScriptExecutionOrderDependency dependency = new ScriptExecutionOrderDependency() { firstScript = script, secondScript = targetScript, orderDiff = attribute.orderDiff };
			// 		list.Add(dependency);
			// 	}
			// }
		}

		return list;
	}

	static List<ScriptExecutionOrderDefinition> GetExecutionOrderDefinitions(Dictionary<Type, MonoScript> types)
	{
		var list = new List<ScriptExecutionOrderDefinition>();

		foreach(var kvp in types)
		{
			var type = kvp.Key;
			var script = kvp.Value;
			if(Attribute.IsDefined(type, typeof(ExecutionOrderAttribute)))
			{
				var attribute = (ExecutionOrderAttribute)Attribute.GetCustomAttribute(type, typeof(ExecutionOrderAttribute));
				ScriptExecutionOrderDefinition definition = new ScriptExecutionOrderDefinition() { script = script, order = attribute.order };
				list.Add(definition);
			}
		}

		return list;
	}

	[UnityEditor.Callbacks.DidReloadScripts]
	static void OnDidReloadScripts()
	{
		var stopwatch = new System.Diagnostics.Stopwatch();
		Debug.Log(">>>>> start");
		try
		{
		stopwatch.Start();

		var types = GetTypeDictionary();

		// var definitions = GetExecutionOrderDefinitions(types);
		// foreach(var definition in definitions)
		// 	Debug.LogFormat("{0} {1}", definition.script.name, definition.order);

		var dependencies = GetExecutionOrderDependencies(types);
		foreach(var dependency in dependencies)
			Debug.LogFormat("{0} -> {1}", dependency.firstScript.name, dependency.secondScript.name/*, dependency.orderDiff*/);

		var graph = Graph.Create(dependencies);
		if(Graph.IsCyclic(graph))
		{
			Debug.LogError("Circular script execution order definitions");
			return;
		}

		var roots = Graph.GetRoots(graph);
		foreach(var root in roots)
		{
			Debug.Log("root " + root.name);
		}

		var orders = new Dictionary<MonoScript, int>();
		foreach(var script in roots)
		{
			int order = MonoImporter.GetExecutionOrder(script);
			orders.Add(script, order);
		}

		Graph.PropagateValues(graph, orders, 10);

		foreach(var kvp in orders)
		{
			var script = kvp.Key;
			var order = kvp.Value;
			Debug.LogFormat("Order {0} {1}", script.name, order);
		}

		// AssetDatabase.StartAssetEditing();
		// UpdateExecutionOrder();
		// AssetDatabase.StopAssetEditing();

		}
		finally
		{
			stopwatch.Stop();
			Debug.LogFormat(">>>>> end {0} ms", stopwatch.Elapsed.TotalSeconds * 1000);
		}
	}



	static void UpdateExecutionOrder()
	{
		var scripts = MonoImporter.GetAllRuntimeMonoScripts();
		foreach(var script in scripts)
		{
			var type = script.GetClass();
			if(type != null && Attribute.IsDefined(type, typeof(ExecutionOrderAttribute)))
			{
				var attribute = (ExecutionOrderAttribute)Attribute.GetCustomAttribute(type, typeof(ExecutionOrderAttribute));
				int order = attribute.order;
				if(MonoImporter.GetExecutionOrder(script) != order)
					MonoImporter.SetExecutionOrder(script, order);
			}
		}
	}
}
