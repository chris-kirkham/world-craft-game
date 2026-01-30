using System;
using UnityEngine;
using UnityEngine.Serialization;

//base class for click-and-draggable UI items
public abstract class DraggableElement : MonoBehaviour, ICursorEventListener
{
    protected bool isHovered;
    protected bool isDragging;

    protected virtual Sprite OnHoverDragSprite { get; set; }

    protected Cursor.SpriteOverride cursorSpriteOverride;

    public event Action DragStarted;
    public event Action DragEnded;

    protected virtual void Start()
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
        else
        {
            Debug.LogError($"Instance of {nameof(Cursor)} not found!");
        }
    }

    private void LateUpdate()
    {
        //only remove as drag target at end of frame to allow other scripts to use it before then
        //TODO: messy, refactor
        if(!isDragging && Cursor.Inst.CurrentDragTarget == this)
        {
            Cursor.Inst.CurrentDragTarget = null;
        }
    }

    private void StartDrag()
    {
        isDragging = true;
        Cursor.Inst.CurrentDragTarget = this;
        OnStartDrag();
        DragStarted?.Invoke();
    }

    private void EndDrag()
    {
        isDragging = false;
        OnEndDrag();
        DragEnded?.Invoke();

        if (!isHovered)
        {
            Cursor.Inst.RemoveSpriteOverride(cursorSpriteOverride);
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
                if(isHovered && !Cursor.Inst.CurrentDragTarget) //if element hovered and not currently dragging something
                {
                    StartDrag();
                }
                break;
            case Cursor.CursorEvent.LeftClickUp:
                EndDrag();
                break;
            default:
                break;
        }
    }
}
