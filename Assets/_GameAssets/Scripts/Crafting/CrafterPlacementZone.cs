using DG.Tweening;
using System;
using System.Collections;
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

        var prevState = state;
        state = newState;

        if (prevState == State.PlacementPreview)
        {
            StopPlacementPreview();
        }

        switch (state)
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
    }

    private IEnumerator PlaceItemRoutine()
    {
        if (!currentItem)
        {
            Debug.Assert(currentItem);
            yield break;
        }

        yield return Tweening.DoTransform(
            currentItem.transform, zonePivot.position, zonePivot.rotation, animateItemToPlacementPointTime).WaitForCompletion();

        ItemPlaced?.Invoke();
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

        if(Cursor.Inst.IsHovered(this))
        {
            SetState(State.PlacementPreview);
        }
        else
        {
            SetState(State.Empty);
        }

        currentItem.SetState(CraftingItem.State.Active);
        currentItem.TryStartDrag();
    }

    private void StartPlacementPreview()
    {
    }

    private void StopPlacementPreview()
    {
    }

    public void OnCursorEvent(Cursor.EventID e)
    {
        if(e == Cursor.EventID.EnterElement) //is cursor over this placement zone?
        {
            if(!currentItem
                && Cursor.Inst.CurrentDragTarget
                && Cursor.Inst.CurrentDragTarget.TryGetComponent<CraftingItem>(out var item)) //TODO: brittle! fails if item isn't on the same object as drag handler)
            {
                SetItem(item);
                SetState(State.PlacementPreview);
            }
        }
        else if(e == Cursor.EventID.ExitElement)
        {
            //dragged item moved away from placement zone - go back to empty state 
            if (state == State.PlacementPreview)
            {
                SetState(State.Empty);
            }
        }
        else if(e == Cursor.EventID.LeftClickDown && state == State.ItemPlaced && Cursor.Inst.IsHovered(this))
        {
            //pick up placed item
            GrabCurrentItem();
        }
        else if (e == Cursor.EventID.LeftClickUp && state == State.PlacementPreview) 
        {
            //release a dragged crafting item over this placement zone
            SetState(State.ItemPlaced);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = state == State.ItemPlaced ? Color.cyan : state == State.PlacementPreview ? Color.green : Color.white;
        Gizmos.DrawWireCube(zonePivot ? zonePivot.position : transform.position, new Vector3(zoneSize.x, 0.1f, zoneSize.y));
    }
}
