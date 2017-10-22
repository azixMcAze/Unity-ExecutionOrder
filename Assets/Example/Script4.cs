using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// this script will have an execution order of 120 (max(40+10, 100+20))
[ExecuteAfter(typeof(Script2)), ExecuteAfter(typeof(Script3), orderIncrease = 20)]
public class Script4 : MonoBehaviour {
    void Start () {
        Debug.Log("Fourth");
    }
}