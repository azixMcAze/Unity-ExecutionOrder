using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecutionOrder(2)]
[ExecuteAfter(typeof(AttributeTest1))]
[ExecuteAfter(typeof(AttributeTest4))]
public class AttributeTest3 : MonoBehaviour {
	void Start () {
		Debug.Log(this.GetType().Name);
	}
}
