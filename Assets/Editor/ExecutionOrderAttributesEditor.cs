using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

public static class ExecutionOrderAttributeEditor
{
	static Dictionary<Type, MonoScript> s_typeScriptDictionary = new Dictionary<Type, MonoScript>();

	class ScriptExecutionOrderDependency
	{
		public MonoScript firstScript { get; set; }
		public MonoScript secondScript { get; set; }
		public int orderDiff { get; set; }
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

	static List<ScriptExecutionOrderDependency> GetExecuteAfter()
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
					ScriptExecutionOrderDependency dependency = new ScriptExecutionOrderDependency() { firstScript = script, secondScript = targetScript, orderDiff = attribute.orderDiff };
					list.Add(dependency);
				}
			}
			if(Attribute.IsDefined(type, typeof(ExecuteBeforeAttribute)))
			{
				var attributes = (ExecuteBeforeAttribute[])Attribute.GetCustomAttributes(type, typeof(ExecuteBeforeAttribute));
				foreach(var attribute in attributes)
				{
					MonoScript targetScript = s_typeScriptDictionary[attribute.targetType];
					ScriptExecutionOrderDependency dependency = new ScriptExecutionOrderDependency() { firstScript = targetScript, secondScript = script, orderDiff = attribute.orderDiff };
					list.Add(dependency);
				}
			}
		}

		return list;
	}

	[UnityEditor.Callbacks.DidReloadScripts]
	static void OnDidReloadScripts()
	{
		System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
		stopwatch.Start();

		FillTypeScriptDictionary();

		var afters = GetExecuteAfter();
		foreach(var after in afters)
			Debug.LogFormat("{0} after {1} {2}", after.firstScript.name, after.secondScript.name, after.orderDiff);
		// AssetDatabase.StartAssetEditing();
		// UpdateExecutionOrder();
		// AssetDatabase.StopAssetEditing();

		stopwatch.Stop();
		Debug.LogFormat("{0} ms", stopwatch.Elapsed.TotalSeconds * 1000);
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
