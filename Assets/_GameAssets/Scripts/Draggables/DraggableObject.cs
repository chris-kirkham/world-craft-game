using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//base class for click-and-draggable items
public abstract class DraggableObject : MonoBehaviour, ICursorEventListener
{
    [SerializeField] private bool dragEnabled = true;
    [SerializeField] private bool requiresPlacementPoint;
    
    protected bool isDragging;

    protected virtual Sprite OnHoverDragSprite { get; set; }

    protected Cursor.SpriteOverride dragPreviewCursorSprite;

    public UnityEvent DragStarted;
    public UnityEvent DragEnded;

    private HashSet<DraggablePlacementPoint> hoveredPlacementPoints = new HashSet<DraggablePlacementPoint>();
    private DraggablePlacementPoint returnPoint;

    protected virtual void OnEnable()
    {
        dragPreviewCursorSprite = new Cursor.SpriteOverride()
        {
            sprite = OnHoverDragSprite
        };

        if (Cursor.InstExists())
        {
            Cursor.Inst.AddCursorEventListener(this);
        }
        else
        {
            Debug.LogError($"Instance of {nameof(Cursor)} not found!");
        }    
    }

    protected virtual void OnDisable()
    {
        if (Cursor.InstExists())
        {
            Cursor.Inst.RemoveCursorEventListener(this);
        }

        EndDrag();
        DragStarted.RemoveAllListeners();
        DragEnded.RemoveAllListeners();
    }

    //"Try" because if multiple draggables want to start dragging on the same tick,
    //the cursor decides which is the best option (why does the cursor do this? maybe move it to a different class)
    public void TryStartDrag()
    {
        if (dragEnabled && Cursor.InstExists())
        {
            Cursor.Inst.RequestDrag(this);
        }
    }

    public void StartDrag()
    {
        if(isDragging) //skip if already dragging
        {
            return;
        }

        isDragging = true;
        OnStartDrag();
        DragStarted?.Invoke();
    }

    public void EndDrag()
    {
        CancelDragRequest();

        if (!isDragging)
        {
            return;
        }

        isDragging = false;

        var bestPlacementPoint = GetBestAvailablePlacementPoint();
        var placed = bestPlacementPoint && bestPlacementPoint.TryPlaceObject(this);
        if(!placed && requiresPlacementPoint)
        {
            if(!TryReturn())
            {
                Debug.Log($"Dropped draggable that requires a placement point," +
                        $" but none found and no return point set! What to do here?");
            }
        }
        
        AddHoveredPoint(null);
        OnEndDrag();
        DragEnded?.Invoke();
    }

    private void CancelDragRequest()
    {
        var cursor = Cursor.Inst;
        if (cursor)
        {
            cursor.RemoveDragRequest(this);
            if (cursor.IsHovered(this))
            {
                cursor.RemoveSpriteOverride(dragPreviewCursorSprite);
            }
        }
    }

    protected virtual void OnStartDrag()
    {
    }

    protected virtual void OnEndDrag()
    {
    }

    public void SetDragEnabled(bool enabled)
    {
        dragEnabled = enabled;
        if(!dragEnabled)
        {
            EndDrag();
        }
    }

    public void AddHoveredPoint(DraggablePlacementPoint placementPoint)
    {
        hoveredPlacementPoints.Add(placementPoint);
    }

    public void RemoveHoveredPoint(DraggablePlacementPoint placementPoint)
    {
        hoveredPlacementPoints.Remove(placementPoint);
    }

    public void SetReturnPoint(DraggablePlacementPoint placementPoint)
    {
        returnPoint = placementPoint;
    }

    public bool TryReturn()
    {
        return returnPoint && returnPoint.TryPlaceObject(this);
    }

    private DraggablePlacementPoint GetBestAvailablePlacementPoint()
    {
        if(hoveredPlacementPoints == null || hoveredPlacementPoints.Count == 0)
        {
            return null;
        }

        //get closest point - make this more sophisticated?
        DraggablePlacementPoint bestPoint = null;
        var minDist = Mathf.Infinity;
        foreach(var point in hoveredPlacementPoints)
        {
            if(!point) //TODO: CLEAN WAY OF REMOVING NULL POINTS
            {
                continue;
            }

            var sqrDist = Vector3.SqrMagnitude(transform.position - point.transform.position);
            if (sqrDist < minDist)
            {
                bestPoint = point;
                minDist = sqrDist;
            }
        }

        return bestPoint;
    }

    public virtual void OnCursorEvent(Cursor.EventID e)
    {
        switch(e)
        {
            case Cursor.EventID.EnterElement:
                Cursor.Inst.AddSpriteOverride(dragPreviewCursorSprite);
                break;
            case Cursor.EventID.ExitElement:
                if (!isDragging)
                {
                    Cursor.Inst.RemoveSpriteOverride(dragPreviewCursorSprite);
                    EndDrag();
                }
                break;
            case Cursor.EventID.LeftClickDown:
                if(Cursor.Inst.IsHovered(this))
                {
                    TryStartDrag();
                }
                break;
            case Cursor.EventID.LeftClickUp:
                EndDrag();
                break;
            default:
                break;
        }
    }
}
