using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecutionOrder(1)]
[ExecuteAfter(typeof(AttributeTest2))]
[ExecuteAfter(typeof(AttributeTest3))]
public class AttributeTest1 : MonoBehaviour {
	void Start () {
		Debug.Log(this.GetType().Name);
	}
}
