using System.Collections.Generic;
using UnityEngine;

public class DraggablesManager
{
    private HashSet<DraggableObject> DragRequests = new HashSet<DraggableObject>();

    public DraggableObject CurrentDragTarget { get; set; }

    private Cursor cursor;

    public DraggablesManager(Cursor cursor)
    {
        Debug.Assert(cursor);
        this.cursor = cursor;
    }

    public void RequestDrag(DraggableObject draggable)
    {
        DragRequests.Add(draggable);
    }

    public void EndDrag(DraggableObject draggable)
    {
        DragRequests.Remove(draggable);
    }

    public void UpdateDragTarget()
    {
        if (CurrentDragTarget)
        {
            //if we're already dragging something in the drag list, keep dragging that
            if (DragRequests.Contains(CurrentDragTarget))
            {
                return;
            }
            else //if current drag target was removed from the drag list, end that drag
            {
                CurrentDragTarget = null;
            }
        }

        if (DragRequests.Count == 0)
        {
            return;
        }

        var bestDraggable = FindBestDragTarget();
        if (bestDraggable)
        {
            CurrentDragTarget = bestDraggable;
            CurrentDragTarget.StartDrag();
        }
    }

    private DraggableObject FindBestDragTarget()
    {
        //find best drag option by y distance to camera - TODO: think of a smarter way to do this
        DraggableObject bestDraggable = null;
        var minDist = Mathf.Infinity;
        foreach (var draggable in DragRequests)
        {
            if (!draggable)
            {
                continue;
            }

            var dist = Mathf.Abs(draggable.transform.position.y - cursor.Cam.transform.position.y);
            if (dist < minDist)
            {
                bestDraggable = draggable;
                minDist = dist;
            }
        }

        return bestDraggable;
    }
}
