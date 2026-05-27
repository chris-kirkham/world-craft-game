using System.Collections.Generic;
using UnityEngine;

public class DraggablesManager
{
    private HashSet<DraggableObject> dragRequests = new HashSet<DraggableObject>();

    public DraggableObject CurrentDragTarget { get; set; }

    public HashSet<DraggableObject> DragRequests => dragRequests;

    private Cursor cursor;

    public DraggablesManager(Cursor cursor)
    {
        Debug.Assert(cursor);
        this.cursor = cursor;
    }

    public void RequestDrag(DraggableObject draggable)
    {
        dragRequests.Add(draggable);
    }

    public void EndDrag(DraggableObject draggable)
    {
        dragRequests.Remove(draggable);
    }

    public void UpdateDragTarget()
    {
        if (CurrentDragTarget)
        {
            //if we're already dragging something in the drag list, keep dragging that
            if (dragRequests.Contains(CurrentDragTarget))
            {
                return;
            }
            else //if current drag target was removed from the drag list, end that drag (TODO: hmm)
            {
                CurrentDragTarget = null;
            }
        }

        if (dragRequests.Count == 0)
        {
            return;
        }

        var bestDraggable = FindBestDragTarget();
        if (bestDraggable)
        {
            if(bestDraggable.TryStartDrag())
            {
                CurrentDragTarget = bestDraggable;
            }
            else
            {
                dragRequests.Remove(bestDraggable);
                UpdateDragTarget(); //TODO: this will silently fail if there are no valid drag targets... potentially confusing
            }
        }
    }

    private DraggableObject FindBestDragTarget()
    {
        //find best drag option by y distance to camera - TODO: think of a smarter way to do this
        DraggableObject bestDraggable = null;
        var minDist = Mathf.Infinity;
        foreach (var draggable in dragRequests)
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
