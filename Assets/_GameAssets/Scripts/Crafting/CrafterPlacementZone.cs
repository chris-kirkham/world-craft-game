using System;
using Unity.VisualScripting;
using UnityEngine;

public class CrafterPlacementZone : MonoBehaviour, ICursorEventListener
{
    private enum State
    {
        Empty,
        PlacementPreview,
        ItemPlaced
    }

    [SerializeField] private Transform zonePivot;
    [SerializeField] private Vector2 zoneSize;
    [SerializeField] private Transform craftResultSpawnPoint;

    public event Action<CraftingItem> ItemPlaced;

    private CraftingItem currentItem;
    private State state;

    private void Start()
    {   
        if(!zonePivot)
        {
            zonePivot = transform;
        }
     
        if(Cursor.InstExists())
        {
            Cursor.Inst.AddCursorEventListener(this);
        }

        if(CraftingManager.InstExists())
        {
            CraftingManager.Inst.AddPlacementPoint(this);
            if(craftResultSpawnPoint)
            {

            }
        }
    }

    private void OnDisable()
    {
        if(Cursor.InstExists())
        {
            Cursor.Inst.RemoveCursorEventListener(this);
        }
    }

    private void LateUpdate()
    {
        if(!currentItem)
        {
            RemoveItem();
        }
    }

    private void SetState(State newState)
    {
        state = newState;
    }

    private void PlaceItem(CraftingItem item)
    {
        Debug.Assert(item);

        currentItem = item;
        ItemPlaced?.Invoke(item);
        SetState(State.ItemPlaced);
    }

    public void RemoveItem()
    {
        currentItem = null;
        SetState(State.Empty);
    }

    private void StartPlacementPreview()
    {
    }

    private void StopPlacementPreview()
    {
    }

    public void OnCursorEvent(Cursor.CursorEvent e)
    {
        if(e == Cursor.CursorEvent.EnterElement) //is cursor over this placement zone?
        {
            if(state != State.ItemPlaced)
            {
                if(Cursor.Inst.CurrentDragTarget
                    && Cursor.Inst.CurrentDragTarget.TryGetComponent<CraftingItem>(out var item)) //TODO: brittle! fails if item isn't on the same object as drag handler
                {
                    currentItem = item;
                    SetState(State.PlacementPreview);
                    StartPlacementPreview();
                }
            }
        }
        else if(e == Cursor.CursorEvent.ExitElement)
        {
            if(state == State.PlacementPreview)
            {
                StopPlacementPreview();
                SetState(State.Empty);
                currentItem = null;
            }
        }

        //if releasing a dragged crafting item over this placement zone
        if (e == Cursor.CursorEvent.LeftClickUp && state == State.PlacementPreview) 
        {
            PlaceItem(currentItem);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = state == State.ItemPlaced ? Color.cyan : state == State.PlacementPreview ? Color.green : Color.white;
        Gizmos.DrawCube(zonePivot ? zonePivot.position : transform.position, new Vector3(zoneSize.x, 1f, zoneSize.y));
    }
}
