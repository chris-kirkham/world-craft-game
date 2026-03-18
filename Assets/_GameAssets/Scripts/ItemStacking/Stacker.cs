using UnityEngine;

public class Stacker : MonoBehaviour
{
    public static void Stack(IStackable bottom, IStackable top)
    {
        var bottomObj = bottom as MonoBehaviour;
        var topObj = top as MonoBehaviour;

        if(!bottomObj || !topObj)
        {
            Debug.Log($"Tried to stack non-MonoBehaviour objects!");
            return;
        }

        var bottomTform = bottomObj.transform;
        var topTform = topObj.transform;

        var stacker = bottom.CurrentStacker;
        if(!stacker)
        {
            stacker = InstantiateNewStack(bottomTform);
            bottomTform.SetParent(stacker.transform, worldPositionStays: true);
            bottom.CurrentStacker = stacker;
        }

        topTform.SetParent(stacker.transform, worldPositionStays: true);
        topTform.position = bottomTform.position + top.StackingOffset;
        top.CurrentStacker = stacker;
    }

    private static Stacker InstantiateNewStack(Transform rootObj)
    {
        var obj = new GameObject($"Stack ({rootObj.name})");
        obj.transform.position = rootObj.position;
        return obj.AddComponent<Stacker>();
    }
}
