using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Cursor : SingletonMonoBehaviour<Cursor>
{
    public struct CursorEvent
    {
        public CursorEvent(EventID id)
        {
            this.id = id;
            listener = null;
        }

        public CursorEvent(EventID id, ICursorEventListener listener)
        {
            this.id = id;
            this.listener = listener;
        }

        public EventID id;
        public ICursorEventListener listener; //if null, event will be sent to all tracked listeners
    }

    //high-level cursor event enum for use by other scripts. Necessary, or use InputSystem somehow?
    [Flags]
    public enum EventID
    {
        None = 0,
        MouseMove = 1 << 0,
        EnterElement = 1 << 1,
        ExitElement = 1 << 2,
        LeftClickDown = 1 << 3,
        LeftClickUp = 1 << 4,
        RightClickDown = 1 << 5,
        RightClickUp = 1 << 6,
        MiddleClickDown = 1 << 7,
        MiddleClickUp = 1 << 8,
        MAX = 1 << 9
    }

    public struct SpriteOverride
    {
        public Sprite sprite;
        public int priority;
    }

    [SerializeField] private LayerMask cursorRaycastLayerMask;
    [SerializeField] private Camera cam;
    [SerializeField] private Image cursorImage;
    [SerializeField, FormerlySerializedAs("defaultCursorSprite")] private Sprite defaultSprite;
    [Space]
    [SerializeField] private DebugDisplay debugDisplay;

    //mouse raycasting
    private EventSystem eventSystem;
    private PointerEventData pointerEventData;
    private List<RaycastResult> raycastResults = new List<RaycastResult>();
    private const int MaxRaycastHits = 100;
    private const float MaxRaycastDist = 100f;
    private RaycastHit[] raycastHits = new RaycastHit[MaxRaycastHits];
    private ICursorEventListener[] listenerRaycastHits = new ICursorEventListener[MaxRaycastHits];

    //input
    private Stack<CursorEvent> eventStack  = new Stack<CursorEvent>();
    private Vector2 rawMousePosition;
    private Vector2 prevRawMousePosition;
    private Vector2 clampedRawMousePos;
    private Vector3 prevClampedMousePos_WS;
    private bool inputEnabled = true;
    private bool isPositionFrozen;
    private bool[] mouseButtonsPressed = new bool[3]; //0 = left, 1 = right, 2 = middle

    private HashSet<ICursorEventListener> trackedListeners = new HashSet<ICursorEventListener>(); //event listeners
    //listeners flagged to add/remove at the end of a frame
    //- to avoid any modifications to the tracked listeners set caused by input events sent while looping over the set
    private HashSet<ICursorEventListener> listenersToAdd = new HashSet<ICursorEventListener>(); 
    private HashSet<ICursorEventListener> listenersToRemove = new HashSet<ICursorEventListener>(); 
    private List<ICursorEventListener> hoveredListeners = new List<ICursorEventListener>(); //event listeners the cursor is currently on top of
    private List<SpriteOverride> spriteOverrides = new List<SpriteOverride>();

    private DraggablesManager draggablesManager;

    public Vector2 RawPosition => rawMousePosition;
    public Vector2 RawPositionDelta => rawMousePosition - prevRawMousePosition;
    public Vector2 ClampedPosition_SS => clampedRawMousePos;
    public Vector3 ClampedPosition_WS 
        => cam.ScreenToWorldPoint(new Vector3(clampedRawMousePos.x, clampedRawMousePos.y, cursorImage.canvas.planeDistance));
    public Vector3 ClampedPositionDelta_WS => ClampedPosition_WS - prevClampedMousePos_WS;

    public bool IsLeftClickPressed => mouseButtonsPressed[0];
    public bool IsRightClickPressed => mouseButtonsPressed[1];
    public bool IsMiddleClickPressed => mouseButtonsPressed[2];

    public Camera Cam => cam;

    public DraggableObject CurrentDragTarget => draggablesManager.CurrentDragTarget;

    private void OnEnable()
    {
        draggablesManager = new DraggablesManager(this);
        eventSystem = FindFirstObjectByType<EventSystem>();
        UnityEngine.Cursor.visible = false; //hide default cursor (TODO: look at using Cursor.SetCursor instead?)
    }

    private void Update()
    {
        DoCursorRaycasts();
    }

    private void LateUpdate()
    {
        prevRawMousePosition = rawMousePosition;
        prevClampedMousePos_WS = ClampedPosition_WS;

        AddFlaggedCursorEventListeners();
        RemoveFlaggedCursorEventListeners();
        SendEvents();
        draggablesManager.UpdateDragTarget();

        if (debugDisplay)
        {
            debugDisplay.SetCursorDebugInfo(this, draggablesManager.DragRequests);
        }
    }

    //TODO: refactor and make more efficient! probably don't need both physics and UI (event system) raycasts?
    private void DoCursorRaycasts()
    {
        int listenerHitCount = 0;

        //physics raycast
        var raycastHitCount = Physics.RaycastNonAlloc(
            cam.ScreenPointToRay(clampedRawMousePos),
            raycastHits,
            MaxRaycastDist, 
            cursorRaycastLayerMask, 
            queryTriggerInteraction: QueryTriggerInteraction.Collide);
        for(int i = 0; i < raycastHitCount; i++)
        {
            UpdateHitListeners(raycastHits[i].collider.gameObject);
        }

        //UI raycast
        var uiRaycastResults = GetUIRaycastResults();
        foreach(var result in uiRaycastResults)
        {
            UpdateHitListeners(result.gameObject);
        }

        //check for elements the mouse is no longer hovering over
        for (int i = hoveredListeners.Count - 1; i >= 0; i--)
        {
            var listener = hoveredListeners[i];

            if (listener == null)
            {
                hoveredListeners.RemoveAt(i);
                continue;
            }

            var hitByRaycast = false;
            for (int j = 0; j < listenerHitCount; j++)
            {
                if (listenerRaycastHits[j] == listener)
                {
                    hitByRaycast = true;
                    break;
                }
            }

            if (!hitByRaycast)
            {
                if (trackedListeners.Contains(listener))
                {
                    AddEvent(EventID.ExitElement, listener);
                }

                hoveredListeners.RemoveAt(i);
            }
        }

        void UpdateHitListeners(GameObject hitObject)
        {
            //get all cursor event listeners in hierarchy of the hit component - TODO: think about best way
            //- require listener is actually on component? Parent of the hit component? Allow children too? 
            var hitListeners = hitObject.GetComponentsInParent<ICursorEventListener>();
            foreach (var hitListener in hitListeners)
            {
                if (hitListener == null)
                {
                    continue;
                }

                if (listenerHitCount >= MaxRaycastHits - 1)
                {
                    Debug.LogError($"Hit listener count exceeds size of raycast hit buffer! Some hits will not be registered.");
                    listenerHitCount = MaxRaycastHits - 1;
                    return;
                }

                listenerRaycastHits[listenerHitCount] = hitListener;

                //add to hovered elements
                if (trackedListeners.Contains(hitListener) && !hoveredListeners.Contains(hitListener))
                {
                    hoveredListeners.Add(hitListener);
                    AddEvent(EventID.EnterElement, hitListener);
                }

                listenerHitCount++;
            }
        }
    }

    private List<RaycastResult> GetUIRaycastResults()
    {
        pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = clampedRawMousePos;

        raycastResults.Clear();
        eventSystem.RaycastAll(pointerEventData, raycastResults);

        return raycastResults;
    }

    private void SetPosition(Vector2 rawMousePosition)
    {
        //N.B. raw mouse position (i.e. Windows cursor position) is in range (0, 0) to (screen width, screen height),
        //from bottom-left to top-right

        clampedRawMousePos = new Vector2(
            Mathf.Clamp(rawMousePosition.x, 0f, Screen.width),
            Mathf.Clamp(rawMousePosition.y, 0f, Screen.height));

        //N.B. image should be anchored to bottom-left, TODO: add validation for this?  
        cursorImage.rectTransform.anchoredPosition = new Vector2(clampedRawMousePos.x, clampedRawMousePos.y);
    }

    public void AddSpriteOverride(SpriteOverride spriteOverride)
    {
        if(spriteOverrides.Contains(spriteOverride))
        {
            return;
        }

        spriteOverrides.Add(spriteOverride);
        SetSpriteFromOverride(spriteOverride);
    }

    public void RemoveSpriteOverride(SpriteOverride spriteOverride)
    {
        spriteOverrides.Remove(spriteOverride);

        if(spriteOverrides.Count == 0)
        {
            cursorImage.sprite = defaultSprite;
        }
        else //use most recently-added override
        {
            SetSpriteFromOverride(spriteOverrides[spriteOverrides.Count - 1]);
        }
    }

    private void SetSpriteFromOverride(SpriteOverride spriteOverride)
    {
        if (spriteOverride.sprite)
        {
            cursorImage.sprite = spriteOverride.sprite;
        }
        else
        {
            Debug.LogWarning($"Cursor sprite override has null sprite!");
            cursorImage.sprite = Resources.Load<Sprite>("TX_Error_Sprite");
        }
    }

    public void AddCursorEventListener(ICursorEventListener listener)
    {
        listenersToAdd.Add(listener);
    }

    public void RemoveCursorEventListener(ICursorEventListener listener)
    {
        listenersToRemove.Add(listener);
    }

    private void AddFlaggedCursorEventListeners()
    {
        foreach (var listener in listenersToAdd)
        {
            trackedListeners.Add(listener);
        }

        listenersToAdd.Clear();
    }

    private void RemoveFlaggedCursorEventListeners()
    {
        foreach(var listener in listenersToRemove)
        {
            if(trackedListeners.Contains(listener))
            {
                trackedListeners.Remove(listener);
            }
        }

        listenersToRemove.Clear();
    }

    public void SetAllowInput(bool allowInput, bool alsoShowHideCursor = false)
    {
        inputEnabled = allowInput;
        if(alsoShowHideCursor)
        {
            SetCursorVisible(inputEnabled);
        }
    }

    public void SetCursorVisible(bool visible)
    {
        if(cursorImage)
        {
            cursorImage.enabled = visible;
        }
    }

    public void FreezeCursorPos(bool frozen)
    {
        isPositionFrozen = frozen;
    }

    //returns true if the cursor is hovering over the given listener
    public bool IsHovered(ICursorEventListener listener)
    {
        return hoveredListeners.Contains(listener);
    }
    
    private void AddEvent(EventID e, ICursorEventListener listener = null)
    {
        if(!inputEnabled)
        {
            return;
        }

        eventStack.Push(new CursorEvent(e, listener));
    }

    private void SendEvents()
    {
        while(eventStack.Count > 0)
        {
            var e = eventStack.Pop();
            if(e.listener != null)
            {
                e.listener.OnCursorEvent(e.id);
            }
            else
            {
                foreach(var listener in trackedListeners)
                {
                    listener.OnCursorEvent(e.id);
                }
            }
        }
    }

    public void RequestDrag(DraggableObject draggable)
    {
        draggablesManager.RequestDrag(draggable);
    }

    public void RemoveDragRequest(DraggableObject draggable)
    {
        draggablesManager.EndDrag(draggable);
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if(cam)
        {
            Handles.Label(transform.position + (Vector3.right * 10f),
                $"World: {ClampedPosition_WS.x.ToString("0000")}, {ClampedPosition_WS.y.ToString("0000")} \n"
                + $"Raw: {rawMousePosition.x.ToString("0000")}, {rawMousePosition.y.ToString("0000")}");
        }
#endif
    }

    #region InputActions
    private void OnMouseMove(InputValue value)
    {
        rawMousePosition = value.Get<Vector2>();

        if (!isPositionFrozen)
        {
            AddEvent(EventID.MouseMove);
            SetPosition(rawMousePosition);
        }
    }

    private void OnMouseLeftClick(InputValue value)
    {
        var floatVal = value.Get<float>();
        OnMouseButtonPressedOrUnpressed(0, floatVal > 0f);
    }

    private void OnMouseRightClick(InputValue value)
    {
        var floatVal = value.Get<float>();
        OnMouseButtonPressedOrUnpressed(1, floatVal > 0f);
    }

    private void OnMouseMiddleClick(InputValue value)
    {
        var floatVal = value.Get<float>();
        OnMouseButtonPressedOrUnpressed(2, floatVal > 0f);
    }

    private void OnMouseButtonPressedOrUnpressed(int button, bool pressed)
    {
        switch(button)
        {
            case 0:
                if (pressed && !mouseButtonsPressed[button])
                {
                    AddEvent(EventID.LeftClickDown);
                }

                if(!pressed && mouseButtonsPressed[button])
                {
                    AddEvent(EventID.LeftClickUp);
                }

                break;
            case 1:
                if (pressed && !mouseButtonsPressed[button])
                {
                    AddEvent(EventID.RightClickDown);
                }

                if (!pressed && mouseButtonsPressed[button])
                {
                    AddEvent(EventID.RightClickUp);
                }

                break;
            case 2:
                if (pressed && !mouseButtonsPressed[button])
                {
                    AddEvent(EventID.MiddleClickDown);
                }

                if (!pressed && mouseButtonsPressed[button])
                {
                    AddEvent(EventID.MiddleClickUp);
                }

                break;
        }

        mouseButtonsPressed[button] = pressed;
    }

    #endregion
}
