using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public static class ExecutionOrderAttributeEditor
{
	static class Graph
	{
		public struct Edge
		{
			public MonoScript node;
			public int weight;
		}

		public static Dictionary<MonoScript, List<Edge>> Create(List<ScriptExecutionOrderDefinition> definitions, List<ScriptExecutionOrderDependency> dependencies)
		{
			var graph = new Dictionary<MonoScript, List<Edge>>();

			foreach(var dependency in dependencies)
			{
				var source = dependency.firstScript;
				var dest = dependency.secondScript;
				List<Edge> edges;
				if(!graph.TryGetValue(source, out edges))
				{
					edges = new List<Edge>();
					graph.Add(source, edges);
				}
				edges.Add(new Edge() { node = dest, weight = dependency.orderDelta });
				if(!graph.ContainsKey(dest))
				{
					graph.Add(dest, new List<Edge>());
				}
			}

			foreach(var definition in definitions)
			{
				var node = definition.script;
				if(!graph.ContainsKey(node))
				{
					graph.Add(node, new List<Edge>());
				}
			}

			return graph;
		}

		static bool IsCyclicRecursion(Dictionary<MonoScript, List<Edge>> graph, MonoScript node, Dictionary<MonoScript, bool> visited, Dictionary<MonoScript, bool> inPath)
		{
			if(!visited[node])
			{
				visited[node] = true;
				inPath[node] = true;

				foreach(var edge in graph[node])
				{
					var succ = edge.node;
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

		public static bool IsCyclic(Dictionary<MonoScript, List<Edge>> graph)
		{
			var visited = new Dictionary<MonoScript, bool>();
			var inPath = new Dictionary<MonoScript, bool>();
			foreach(var node in graph.Keys)
			{
				visited.Add(node, false);
				inPath.Add(node, false);
			}

			foreach(var node in graph.Keys)
				if(IsCyclicRecursion(graph, node, visited, inPath))
					return true;
			
			return false;
		}

		public static List<MonoScript> GetRoots(Dictionary<MonoScript, List<Edge>> graph)
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
				foreach(var edge in edges)
				{
					var succ = edge.node;
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

		public static void PropagateValues(Dictionary<MonoScript, List<Edge>> graph, Dictionary<MonoScript, int> values)
		{
			var queue = new Queue<MonoScript>();

			foreach(var node in values.Keys)
				queue.Enqueue(node);

			while(queue.Count > 0)
			{
				var node = queue.Dequeue();
				int currentValue = values[node];

				foreach(var edge in graph[node])
				{
					var succ = edge.node;
					var newValue = currentValue + edge.weight;
					int prevValue;
					bool hasPrevValue = values.TryGetValue(succ, out prevValue);
					bool newValueBeyond = (edge.weight > 0) ? (newValue > prevValue) : (newValue < prevValue);
					if(!hasPrevValue || newValueBeyond)
					{
						values[succ] = newValue;
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
		public int orderDelta { get; set; }
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
			bool hasExecutionOrderAttribute = Attribute.IsDefined(type, typeof(ExecutionOrderAttribute));
			bool hasExecuteAfterAttribute = Attribute.IsDefined(type, typeof(ExecuteAfterAttribute));
			bool hasExecuteBeforeAttribute = Attribute.IsDefined(type, typeof(ExecuteBeforeAttribute));

			if(hasExecuteAfterAttribute)
			{
				if(hasExecutionOrderAttribute)
				{
					Debug.LogError(string.Format("Script {0} has both [ExecutionOrder] and [ExecuteAfter] attributes. Ignoring the [ExecuteAfter] attribute.", script.name), script);
					continue;
				}

				var attributes = (ExecuteAfterAttribute[])Attribute.GetCustomAttributes(type, typeof(ExecuteAfterAttribute));
				foreach(var attribute in attributes)
				{
					if(attribute.orderIncrease < 0)
					{
						Debug.LogError(string.Format("Script {0} has an [ExecuteAfter] attribute with a negative orderIncrease. Use the [ExecuteBefore] attribute instead. Ignoring this [ExecuteAfter] attribute.", script.name), script);
						continue;
					}

					if(!attribute.targetType.IsSubclassOf(typeof(MonoBehaviour)) && !attribute.targetType.IsSubclassOf(typeof(ScriptableObject)))
					{
						Debug.LogError(string.Format("Script {0} has an [ExecuteAfter] attribute with targetScript={1} which is not a MonoBehaviour nor a ScriptableObject. Ignoring this [ExecuteAfter] attribute.", script.name, attribute.targetType.Name), script);
						continue;
					}

					MonoScript targetScript = types[attribute.targetType];
					ScriptExecutionOrderDependency dependency = new ScriptExecutionOrderDependency() { firstScript = targetScript, secondScript = script, orderDelta = attribute.orderIncrease };
					list.Add(dependency);
				}
			}

			if (hasExecuteBeforeAttribute)
			{
				if(hasExecutionOrderAttribute)
				{
					Debug.LogError(string.Format("Script {0} has both [ExecutionOrder] and [ExecuteBefore] attributes. Ignoring the [ExecuteBefore] attribute.", script.name), script);
					continue;
				}

				if(hasExecuteAfterAttribute)
				{
					Debug.LogError(string.Format("Script {0} has both [ExecuteAfter] and [ExecuteBefore] attributes. Ignoring the [ExecuteBefore] attribute.", script.name), script);
					continue;
				}

				var attributes = (ExecuteBeforeAttribute[])Attribute.GetCustomAttributes(type, typeof(ExecuteBeforeAttribute));
				foreach(var attribute in attributes)
				{
					if(attribute.orderDecrease < 0)
					{
						Debug.LogError(string.Format("Script {0} has an [ExecuteBefore] attribute with a negative orderDecrease. Use the [ExecuteAfter] attribute instead. Ignoring this [ExecuteBefore] attribute.", script.name), script);
						continue;
					}

					if(!attribute.targetType.IsSubclassOf(typeof(MonoBehaviour)) && !attribute.targetType.IsSubclassOf(typeof(ScriptableObject)))
					{
						Debug.LogError(string.Format("Script {0} has an [ExecuteBefore] attribute with targetScript={1} which is not a MonoBehaviour nor a ScriptableObject. Ignoring this [ExecuteBefore] attribute.", script.name, attribute.targetType.Name), script);
						continue;
					}

					MonoScript targetScript = types[attribute.targetType];
					ScriptExecutionOrderDependency dependency = new ScriptExecutionOrderDependency() { firstScript = targetScript, secondScript = script, orderDelta = -attribute.orderDecrease };
					list.Add(dependency);
				}
			}
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

	static Dictionary<MonoScript, int> GetInitalExecutionOrder(List<ScriptExecutionOrderDefinition> definitions, List<MonoScript> graphRoots)
	{
		var orders = new Dictionary<MonoScript, int>();
		foreach(var definition in definitions)
		{
			var script = definition.script;
			var order = definition.order;
			orders.Add(script, order);
		}

		foreach(var script in graphRoots)
		{
			if(!orders.ContainsKey(script))
			{
				int order = MonoImporter.GetExecutionOrder(script);
				orders.Add(script, order);
			}
		}

		return orders;
	}

	static void UpdateExecutionOrder(Dictionary<MonoScript, int> orders)
	{
		bool startedEdit = false;

		foreach(var kvp in orders)
		{
			var script = kvp.Key;
			var order = kvp.Value;

			if(MonoImporter.GetExecutionOrder(script) != order)
			{
				if(!startedEdit)
				{
					AssetDatabase.StartAssetEditing();
					startedEdit = true;
				}
				MonoImporter.SetExecutionOrder(script, order);
			}
		}

		if(startedEdit)
		{
			AssetDatabase.StopAssetEditing();
		}
	}

	[UnityEditor.Callbacks.DidReloadScripts]
	static void OnDidReloadScripts()
	{
		var types = GetTypeDictionary();
		var definitions = GetExecutionOrderDefinitions(types);
		var dependencies = GetExecutionOrderDependencies(types);
		var graph = Graph.Create(definitions, dependencies);

		if(Graph.IsCyclic(graph))
		{
			Debug.LogError("Circular script execution order definitions");
			return;
		}

		var roots = Graph.GetRoots(graph);
		var orders = GetInitalExecutionOrder(definitions, roots);
		Graph.PropagateValues(graph, orders);

		UpdateExecutionOrder(orders);
	}
}
