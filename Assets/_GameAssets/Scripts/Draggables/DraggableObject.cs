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
    private DraggablePlacementPoint placedPoint;

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
    public void RequestDrag()
    {
        if (dragEnabled && Cursor.InstExists())
        {
            Cursor.Inst.RequestDrag(this);
        }
    }

    public bool TryStartDrag()
    {
        if(isDragging) //skip if already dragging
        {
            return false;
        }

        if(placedPoint && !placedPoint.TryRemovePlacedObj(this))
        {
            return false;
        }

        isDragging = true;
        OnStartDrag();
        DragStarted?.Invoke();

        return true;    
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
            if (!TryReturn())
            {
                Debug.Log($"Dropped draggable that requires a placement point," +
                        $" but none found and no return point set! What to do here?");
            }
        }

        hoveredPlacementPoints.Clear(); //TODO: shouldn't be necessary if the hover events worked properly!!
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

    public bool TryRemoveFromCurrentPlacementPoint()
    {
        if(!placedPoint)
        {
            return true; //hmm
        }

        return placedPoint.TryRemovePlacedObj(this);
    }

    public void OnPlacedAtPlacementPoint(DraggablePlacementPoint point)
    {
        placedPoint = point;
    }

    public void OnRemovedFromPlacementPoint(DraggablePlacementPoint point)
    {
        if(placedPoint == point)
        {
            placedPoint = null;
        }
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
                    RequestDrag();
                }
                break;
            case Cursor.EventID.LeftClickUp:
                EndDrag();
                break;
            default:
                break;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.identity;
        if(placedPoint)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, placedPoint.transform.position);
        }

        if(returnPoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, returnPoint.transform.position);
        }

        if(hoveredPlacementPoints.Count > 0)
        {
            Gizmos.color = Color.yellow;
            foreach(var point in hoveredPlacementPoints)
            {
                if(point)
                {
                    Gizmos.DrawLine(transform.position, point.transform.position);
                }
            }
        }
    }
}
