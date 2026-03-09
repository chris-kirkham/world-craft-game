using System;
using UnityEngine;
using UnityEngine.Events;

//base class for click-and-draggable items
public abstract class DraggableElement : MonoBehaviour, ICursorEventListener
{
    protected bool isDragging;

    protected virtual Sprite OnHoverDragSprite { get; set; }

    protected Cursor.SpriteOverride cursorSpriteOverride;

    public UnityEvent DragStarted;
    public UnityEvent DragEnded;

    private bool dragEnabled = true;

    protected virtual void OnEnable()
    {
        cursorSpriteOverride = new Cursor.SpriteOverride()
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

        CancelDrag();
        DragStarted.RemoveAllListeners();
        DragEnded.RemoveAllListeners();
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

    //"Try" because if multiple draggables want to start dragging on the same tick,
    //the cursor decides which is the best option (why does the cursor do this? maybe move it to a different class)
    public void TryStartDrag()
    {
        if(Cursor.InstExists())
        {
            Cursor.Inst.AddToDragList(this);
        }
    }

    public void CancelDrag()
    {
        var cursor = Cursor.Inst;
        if (cursor)
        {
            cursor.RemoveFromDragList(this);
            if(cursor.IsHovered(this))
            {
                cursor.RemoveSpriteOverride(cursorSpriteOverride);
            }
        }

        if (isDragging)
        {
            isDragging = false;
            OnEndDrag();
            DragEnded?.Invoke();
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
            CancelDrag();
        }
    }

    public virtual void OnCursorEvent(Cursor.EventID e)
    {
        switch(e)
        {
            case Cursor.EventID.EnterElement:
                Cursor.Inst.AddSpriteOverride(cursorSpriteOverride);
                break;
            case Cursor.EventID.ExitElement:
                if (!isDragging)
                {
                    Cursor.Inst.RemoveSpriteOverride(cursorSpriteOverride);
                    CancelDrag();
                }
                break;
            case Cursor.EventID.LeftClickDown:
                if(Cursor.Inst.IsHovered(this))
                {
                    TryStartDrag();
                }
                break;
            case Cursor.EventID.LeftClickUp:
                CancelDrag();
                break;
            default:
                break;
        }
    }
}
