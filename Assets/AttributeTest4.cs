using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecutionOrder(3)]
[ExecuteAfter(typeof(AttributeTest2))]
// [ExecuteBefore(typeof(AttributeTest2))]
public class AttributeTest4 : MonoBehaviour {
	void Start () {
		Debug.Log(this.GetType().Name);
	}
}
