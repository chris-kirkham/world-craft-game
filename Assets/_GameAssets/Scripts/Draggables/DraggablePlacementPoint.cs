using UnityEngine;

public abstract class DraggablePlacementPoint : MonoBehaviour, ICursorEventListener
{
    [SerializeField] private bool returnable = true;

    public bool TryPlaceObject(DraggableObject obj)
    {
        if (CanPlace(obj))
        {
            //try remove object from its current placement, if any
            if(!obj.TryRemoveFromCurrentPlacementPoint())
            {
                return false;
            }

            PlaceObject(obj);
            Debug.Log($"Placed object {obj.name} onto {this.name}");
            obj.OnPlacedAtPlacementPoint(this); //TODO: UGH!! REFACTOR
            if (returnable)
            {
                obj.SetReturnPoint(this);
            }

            return true;
        }

        return false;
    }

    //Can the given object be placed on this placement point?
    protected abstract bool CanPlace(DraggableObject obj);

    //place the object on this placement point
    protected virtual void PlaceObject(DraggableObject obj)
    {
    }

    public bool TryRemovePlacedObj(DraggableObject obj)
    {
        if (obj && CanRemovePlacedObj(obj))
        {
            OnPlacedObjRemoved(obj);
            Debug.Log($"Removed object {obj.name} from {this.name}");
            obj.OnRemovedFromPlacementPoint(this); //TODO: UGH: REFACTOR
            return true;
        }

        return false;
    }

    protected abstract bool CanRemovePlacedObj(DraggableObject obj);

    protected virtual void OnPlacedObjRemoved(DraggableObject obj)
    {
    }

    protected virtual void OnDraggableEnterPlacementArea(DraggableObject obj)
    {
        if (obj)
        {
            obj.AddHoveredPoint(this);
        }
    }

    protected virtual void OnDraggableExitPlacementArea(DraggableObject obj)
    {
        if (obj)
        {
            obj.RemoveHoveredPoint(this);
        }
    }

    public virtual void OnCursorEvent(Cursor.EventID e)
    {
        var dragTarget = Cursor.Inst.CurrentDragTarget;
        if (dragTarget)
        {
            if (e == Cursor.EventID.EnterElement)
            {
                OnDraggableEnterPlacementArea(dragTarget);
            }
            else if (e == Cursor.EventID.ExitElement)
            {
                OnDraggableExitPlacementArea(dragTarget);
            }
        }
    }
}
