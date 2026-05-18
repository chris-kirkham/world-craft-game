using UnityEngine;

public abstract class DraggablePlacementPoint : MonoBehaviour, ICursorEventListener
{
    [SerializeField] private bool returnable = true;

    public bool TryPlaceObject(DraggableObject obj)
    {
        if (CanPlace(obj))
        {
            PlaceObject(obj);
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
    protected abstract void PlaceObject(DraggableObject obj);

    protected virtual void OnDraggableEnterPlacementArea(DraggableObject obj)
    {
        if(obj)
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
        if(dragTarget)
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
