# Unity Execution Order

A collection of attributes to control the execution order of your scripts in Unity from your source code.

## Use
Add one of these attribute to your script's class definition to change its script execution order :
- `[ExecutionOrder(int order)]` : The script execution order is set to `order`
- `[ExecuteAfter(Type type)]` : The script execution order is set to a value greater than the one of the script `type`. This will ensure that your script will be executed after the script `type`. By default, the script execution order is increased by 10.

A script can have multiple `ExecuteAfter` attributes and will be executed after all the scripts given in parameters.
A script cannot have both an `ExecutionOrder` and an `ExecuteAfter` attribute.

Changing the execution order of a script with either an `ExecutionOrder` or an `ExecuteAfter` attribute from the *Script Execution Order* inspector in Unity will have no effect. The order will be reset to the one defined by the attributes.

Changing a script execution order, either from the inspector or with an attribute, will cause a recompilation of the scripts.

## Example
```csharp
// Let's say that this script has an execution order of 50
// set from the Script Execution Order inspector in Unity
public class Script1 : MonoBehaviour {
    void Start () {
        Debug.Log("First");
    }
}
```
```csharp
// this script will have an execution order of 60 (50+10)
[ExecuteAfter(typeof(Script1))]
public class Script2 : MonoBehaviour {
    void Start () {
        Debug.Log("Second");
    }
}
```
```csharp
// this script will have an execution order of 100
[ExecutionOrder(100)]
public class Script3 : MonoBehaviour {
    void Start () {
        Debug.Log("Third");
    }
}
```
```csharp
// this script will have an execution order of 110 (max(60+10, 100+10))
[ExecuteAfter(typeof(Script2)), ExecuteAfter(typeof(Script3))]
public class Script4 : MonoBehaviour {
    void Start () {
        Debug.Log("Fourth");
    }
}
```
The resulting script execution orders in the inspector:

![Script Execution Order inspector screenshot](/docs/screenshot1.png)
