using UnityEngine;

public interface IStackable
{
    public bool CanStack { get; }
    public Vector3 StackingOffset { get; }
    public Stacker CurrentStacker { get;  set; }
    public Transform transform { get; }
    public GameObject gameObject => transform.gameObject;
    public void OnAddedToStack();
    public void OnRemovedFromStack();
}
