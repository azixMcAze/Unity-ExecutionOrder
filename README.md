# Unity Execution Order

A collection of attributes to control the execution order of your scripts in Unity from your source code.

## Use
Add one of these attribute to your script's class definition to change its script execution order :
- `[ExecutionOrder(int order)]` : The script execution order is set to `order`
- `[ExecuteAfter(Type type)]` : The script execution order is set to a value superior to the one of the script `type`. This will ensure that your script will be executed after the script `type`. By default, the script execution order is increased by 10.

A script can have multiple `ExecuteAfter` attributes and will be executed after all the scripts given in parameters.
A script cannot have both an `ExecutionOrder` and an `ExecuteAfter` attribute.


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

