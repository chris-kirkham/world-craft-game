using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

//base class for click-and-draggable items
public abstract class DraggableElement : MonoBehaviour, ICursorEventListener
{
    protected bool isHovered;
    protected bool isDragging;

    protected virtual Sprite OnHoverDragSprite { get; set; }

    protected Cursor.SpriteOverride cursorSpriteOverride;

    public UnityEvent DragStarted;
    public UnityEvent DragEnded;

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

        BreakDrag();
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

    public void EndDrag()
    {
        if (!isHovered)
        {
            Cursor.Inst.RemoveSpriteOverride(cursorSpriteOverride);
        }

        if (!isDragging)
        {
            return;
        }

        isDragging = false;
        OnEndDrag();
        DragEnded?.Invoke();
    }

    public void TryStartDrag()
    {
        if(Cursor.InstExists())
        {
            Cursor.Inst.AddToDragList(this);
        }
    }

    public void BreakDrag()
    {
        if(Cursor.InstExists())
        {
            Cursor.Inst.RemoveFromDragList(this);
        }
    }

    protected virtual void OnStartDrag()
    {
    }

    protected virtual void OnEndDrag()
    {
    }

    //TODO: When over multiple drag handles, sometimes it does both at once (e.g. moves AND resizes)!!
    //Only do the "topmost" one - work out a priority/layer/Z system for raycasts
    
    //ICursorEventListener
    public virtual void OnCursorEvent(Cursor.CursorEvent e)
    {
        switch(e)
        {
            case Cursor.CursorEvent.EnterElement:
                isHovered = true;
                Cursor.Inst.AddSpriteOverride(cursorSpriteOverride);
                break;
            case Cursor.CursorEvent.ExitElement:
                isHovered = false;
                if (!isDragging)
                {
                    Cursor.Inst.RemoveSpriteOverride(cursorSpriteOverride);
                }
                break;
            case Cursor.CursorEvent.LeftClickDown:
                if(isHovered)
                {
                    TryStartDrag();
                }
                break;
            case Cursor.CursorEvent.LeftClickUp:
                BreakDrag();
                break;
            default:
                break;
        }
    }
}
