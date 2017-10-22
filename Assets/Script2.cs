using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// this script will have an execution order of 40 (50-10)
[ExecuteBefore(typeof(Script1))]
public class Script2 : MonoBehaviour {
    void Start () {
        Debug.Log("First");
    }
}