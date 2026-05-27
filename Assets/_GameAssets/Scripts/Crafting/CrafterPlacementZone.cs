using DG.Tweening;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class CrafterPlacementZone : DraggablePlacementPoint, ICursorEventListener
{
    private enum State
    {
        Empty,
        PlacementPreview,
        ItemPlaced
    }

    [SerializeField] private Transform zonePivot;
    [SerializeField] private Vector2 zoneSize;
    [SerializeField] private float animateItemToPlacementPointTime = 0.5f;
    [Header("VFX")]
    [SerializeField] private GameObject placementPreviewVFX;

    private Cursor cursor;
    private CraftingItem currentItem;
    private State state;

    public CraftingItem CurrentItem => currentItem;

    public event Action ItemPlaced;
    public event Action ItemRemoved;

    private void Start()
    {   
        if(!zonePivot)
        {
            zonePivot = transform;
        }
     
        if(Cursor.InstExists())
        {
            cursor = Cursor.Inst;
            Cursor.Inst.AddCursorEventListener(this);
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
            SetState(State.Empty);
        }

        if(cursor && placementPreviewVFX)
        {
            placementPreviewVFX.SetActive(cursor.IsHovered(this) && cursor.CurrentDragTarget);
        }
    }

    private void SetState(State newState)
    {
        if(newState == state)
        {
            return;
        }
        
        if (state == State.PlacementPreview)
        {
            StopPlacementPreview();
        }

        switch (newState)
        {
            case State.Empty:
                SetItem(null);
                ItemRemoved?.Invoke();
                break;
            case State.PlacementPreview:
                StartPlacementPreview();
                break;
            case State.ItemPlaced:
                StartCoroutine(PlaceItemRoutine());
                break;
            default:
                break;
        }

        state = newState;
    }

    private IEnumerator PlaceItemRoutine()
    {
        if (!currentItem)
        {
            Debug.Assert(currentItem);
            yield break;
        }

        if(CanPlace(currentItem))
        {
            yield return Tweening.DoTransform(
                currentItem.transform, zonePivot.position, zonePivot.rotation, animateItemToPlacementPointTime).WaitForCompletion();

            ItemPlaced?.Invoke();
        }
        else
        {
            Debug.LogError($"Failed to place item! This shouldn't happen.");
            SetState(State.PlacementPreview);
        }
    }

    private void SetItem(CraftingItem item)
    {
        if(currentItem && item != currentItem)
        {
            currentItem.OnUsedInSuccessfulCraft -= OnItemUsedInCraft;
        }

        currentItem = item;

        if(currentItem)
        {
            currentItem.OnUsedInSuccessfulCraft += OnItemUsedInCraft;
        }
    }

    private void OnItemUsedInCraft()
    {
        SetState(State.Empty);
    }

    private void GrabCurrentItem()
    {
        Debug.Assert(currentItem);
        if(!currentItem)
        {
            return;
        }

        currentItem.SetState(CraftingItem.State.Active);
        currentItem.RequestDrag();
    }

    private void StartPlacementPreview()
    {
    }

    private void StopPlacementPreview()
    {
    }

    protected override bool CanPlace(DraggableObject obj)
    {
        return state != State.ItemPlaced;
    }

    protected override void PlaceObject(DraggableObject obj)
    {
        SetState(State.ItemPlaced);
    }

    protected override bool CanRemovePlacedObj(DraggableObject obj)
    {
        return state == State.ItemPlaced && currentItem == obj;
    }

    protected override void OnPlacedObjRemoved(DraggableObject obj)
    {
        if(currentItem && currentItem == obj)
        {
            SetState(State.Empty);
        }
        else
        {
            Debug.LogError($"Removed {obj} from {nameof(CrafterPlacementZone)}, but it wasn't this zone's current item! This shouldn't happen!");
        }
    }

    protected override void OnDraggableEnterPlacementArea(DraggableObject obj)
    {
        base.OnDraggableEnterPlacementArea(obj);

        if (!currentItem && Cursor.Inst.CurrentDragTarget is CraftingItem)
        {
            SetItem((CraftingItem)Cursor.Inst.CurrentDragTarget);
            SetState(State.PlacementPreview);
        }
    }

    protected override void OnDraggableExitPlacementArea(DraggableObject obj)
    {
        base.OnDraggableExitPlacementArea(obj);

        //dragged item moved away from placement zone - go back to empty state 
        if (state == State.PlacementPreview)
        {
            SetState(State.Empty);
        }
    }

    public override void OnCursorEvent(Cursor.EventID e)
    {
        base.OnCursorEvent(e);

        if(e == Cursor.EventID.LeftClickDown && state == State.ItemPlaced && Cursor.Inst.IsHovered(this))
        {
            //pick up placed item
            GrabCurrentItem();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = state == State.ItemPlaced ? Color.cyan : state == State.PlacementPreview ? Color.green : Color.white;
        Gizmos.DrawWireCube(zonePivot ? zonePivot.position : transform.position, new Vector3(zoneSize.x, 0.1f, zoneSize.y));
    }
}
