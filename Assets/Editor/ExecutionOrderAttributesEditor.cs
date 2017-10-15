using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

public static class ExecutionOrderAttributeEditor
{
	static Dictionary<Type, MonoScript> s_typeScriptDictionary = new Dictionary<Type, MonoScript>();

	static class Graph
	{
		public static Dictionary<MonoScript, List<MonoScript>> Create(List<ScriptExecutionOrderDependency> dependencies)
		{
			Dictionary<MonoScript, List<MonoScript>> graph = new Dictionary<MonoScript, List<MonoScript>>();
			foreach(var dependency in dependencies)
			{
				var firstScript = dependency.firstScript;
				var secondScript = dependency.secondScript;
				List<MonoScript> edges;
				if(!graph.TryGetValue(firstScript, out edges))
				{
					edges = new List<MonoScript>();
					graph[firstScript] = edges;
				}
				edges.Add(secondScript);
				if(!graph.ContainsKey(secondScript))
				{
					graph[secondScript] = new List<MonoScript>();
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
			Dictionary<MonoScript, bool> visited = new Dictionary<MonoScript, bool>();
			Dictionary<MonoScript, bool> inPath = new Dictionary<MonoScript, bool>();
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
			Dictionary<MonoScript, int> degrees = new Dictionary<MonoScript, int>();
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

			List<MonoScript> roots = new List<MonoScript>();
			foreach(var kvp in degrees)
			{
				var node = kvp.Key;
				int degree = kvp.Value;
				if(degree == 0)
					roots.Add(node);
			}
			return roots;
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

	static void FillTypeScriptDictionary()
	{
		s_typeScriptDictionary.Clear();

		var scripts = MonoImporter.GetAllRuntimeMonoScripts();
		foreach(var script in scripts)
		{
			var type = script.GetClass();
			if(IsTypeValid(type))
			{
				if(!s_typeScriptDictionary.ContainsKey(type))
					s_typeScriptDictionary.Add(type, script);
			}
		}
	}

	static bool IsTypeValid(Type type)
	{
		if(type != null)
			return type.IsSubclassOf(typeof(MonoBehaviour)) || type.IsSubclassOf(typeof(ScriptableObject));
		else
			return false;
	}

	static List<ScriptExecutionOrderDependency> GetExecutionOrderDependencies()
	{
		List<ScriptExecutionOrderDependency> list = new List<ScriptExecutionOrderDependency>();

		foreach(var kvp in s_typeScriptDictionary)
		{
			var type = kvp.Key;
			var script = kvp.Value;
			if(Attribute.IsDefined(type, typeof(ExecuteAfterAttribute)))
			{
				var attributes = (ExecuteAfterAttribute[])Attribute.GetCustomAttributes(type, typeof(ExecuteAfterAttribute));
				foreach(var attribute in attributes)
				{
					MonoScript targetScript = s_typeScriptDictionary[attribute.targetType];
					ScriptExecutionOrderDependency dependency = new ScriptExecutionOrderDependency() { firstScript = script, secondScript = targetScript/*, orderDiff = attribute.orderDiff*/ };
					list.Add(dependency);
				}
			}
			// if(Attribute.IsDefined(type, typeof(ExecuteBeforeAttribute)))
			// {
			// 	var attributes = (ExecuteBeforeAttribute[])Attribute.GetCustomAttributes(type, typeof(ExecuteBeforeAttribute));
			// 	foreach(var attribute in attributes)
			// 	{
			// 		MonoScript targetScript = s_typeScriptDictionary[attribute.targetType];
			// 		ScriptExecutionOrderDependency dependency = new ScriptExecutionOrderDependency() { firstScript = targetScript, secondScript = script, orderDiff = attribute.orderDiff };
			// 		list.Add(dependency);
			// 	}
			// }
		}

		return list;
	}

	static List<ScriptExecutionOrderDefinition> GetExecutionOrderDefinitions()
	{
		List<ScriptExecutionOrderDefinition> list = new List<ScriptExecutionOrderDefinition>();

		foreach(var kvp in s_typeScriptDictionary)
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
		System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
		Debug.Log(">>>>> start");
		try
		{
		stopwatch.Start();

		FillTypeScriptDictionary();

		// var definitions = GetExecutionOrderDefinitions();
		// foreach(var definition in definitions)
		// 	Debug.LogFormat("{0} {1}", definition.script.name, definition.order);

		var dependencies = GetExecutionOrderDependencies();
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
