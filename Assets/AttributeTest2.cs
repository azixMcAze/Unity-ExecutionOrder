using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecutionOrder(2)]
[ExecuteAfter(typeof(AttributeTest1))]
public class AttributeTest2 : MonoBehaviour {
	void Start () {
		Debug.Log(this.GetType().Name);
	}
}
