using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Let's say that this script has an execution order of 50
// set from the Script Execution Order inspector in Unity
public class Script1 : MonoBehaviour {
    void Start () {
        Debug.Log("First");
    }
}