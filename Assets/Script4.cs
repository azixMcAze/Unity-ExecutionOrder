using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// this script will have an execution order of 110 (max(60+10, 100+10))
[ExecuteAfter(typeof(Script2)), ExecuteAfter(typeof(Script3))]
public class Script4 : MonoBehaviour {
    void Start () {
        Debug.Log("Fourth");
    }
}