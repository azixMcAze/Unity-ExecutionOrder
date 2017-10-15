using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecutionOrder(1)]
public class AttributeTest1 : MonoBehaviour {
	void Start () {
		Debug.Log(this.GetType().Name);
	}
}
