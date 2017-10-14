using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

public static class ExecutionOrderAttributeEditor
{
	[UnityEditor.Callbacks.DidReloadScripts]
	static void OnDidReloadScripts()
	{
		AssetDatabase.StartAssetEditing();
		UpdateExecutionOrder();
		AssetDatabase.StopAssetEditing();
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
