using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// this script will have an execution order of 60 (50+10)
[ExecuteAfter(typeof(Script1))]
public class Script2 : MonoBehaviour {
    void Start () {
        Debug.Log("Second");
    }
}