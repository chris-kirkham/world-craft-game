using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class CameraMovement : MonoBehaviour, ICursorEventListener
{
    [SerializeField] private Camera cam;
    [SerializeField] private float minZoom = 0.1f;
    [SerializeField] private float maxZoom = 10f;
    [SerializeField] private float defaultZoom = 1f;
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private Vector3 viewCentre;
    [SerializeField] private Vector3 viewSize;
    [SerializeField] private float moveSpeed = 0.2f;

    [Header("Camera movement by cursor position")]
    [SerializeField] private bool moveCameraWhenCursorAtScreenEdge;
    [SerializeField, Range(0f, 1f)] private float edgeZoneStartX;
    [SerializeField, Range(0f, 1f)] private float edgeZoneStartY;
    [SerializeField] private AnimationCurve edgeDragSpeedCurve;

    private bool movementEnabled = true;
    private bool isDragging;
    private Vector2 mouseDelta;
    private float zoomDelta;
    private float initialOrthoCamSize;
    private float orthoZoomLevel;
    private Vector2 cursorPos_SS;

    public Vector3 ViewCentre => viewCentre;
    private Vector3 ViewMin => viewCentre - (viewSize / 2f);
    private Vector3 ViewMax => viewCentre + (viewSize / 2f);

    private void Start()
    {
        initialOrthoCamSize = cam.orthographicSize;
        orthoZoomLevel = cam.orthographicSize;

        if(Cursor.InstExists())
        {
            Cursor.Inst.AddCursorEventListener(this);
        }
    }

    private void OnDisable()
    {
        if (Cursor.InstExists())
        {
            Cursor.Inst.RemoveCursorEventListener(this);
        }
    }

    private void Update()
    {
        if(isDragging)
        {
            mouseDelta = Cursor.Inst.RawPositionDelta;
        }
    }

    private void LateUpdate()
    {
        if(movementEnabled)
        {
            UpdatePan();
            UpdateZoom(zoomDelta);
        }
        
        ClampCameraPos();
        
        if(!Cursor.Inst.IsRightClickPressed)
        {
            SetDragging(false);
        }
    }

    private void ClampCameraPos()
    {
        var camPos = cam.transform.position;
        var min = ViewMin;
        var max = ViewMax;
        var x = Mathf.Clamp(camPos.x, min.x, max.x);
        var y = Mathf.Clamp(camPos.y, min.y, max.y);
        var z = Mathf.Clamp(camPos.z, min.z, max.z);

        cam.transform.position = new Vector3(x, y, z);
    }

    private void SetDragging(bool dragging)
    {
        isDragging = dragging;
        Cursor.Inst.FreezeCursorPos(dragging);
    }

    private void UpdatePan()
    {
        if (isDragging) //right-click (or whatever) to drag camera
        {
            //cam.transform.position += cam.transform.TransformDirection((Vector3)(mouseDelta * moveSpeed));
            cam.transform.position += new Vector3(mouseDelta.x, 0f, mouseDelta.y) * moveSpeed;
        }
        else if(moveCameraWhenCursorAtScreenEdge) //cursor screen edge dragging
        {
            //get normalised cursor screen position
            var normalisedCursorPosX = cursorPos_SS.x / Screen.width;
            var normalisedCursorPosY = cursorPos_SS.y / Screen.height;

            //remap position to range [-1, 1] where 0 is centre of screen
            normalisedCursorPosX = (normalisedCursorPosX - 0.5f) * 2f;
            normalisedCursorPosY = (normalisedCursorPosY - 0.5f) * 2f;

            var dragSpeedX = GetDragSpeedRatio(normalisedCursorPosX, edgeZoneStartX);
            var dragSpeedY = GetDragSpeedRatio(normalisedCursorPosY, edgeZoneStartY);

            cam.transform.position += new Vector3(dragSpeedX, 0f, dragSpeedY) * moveSpeed;

            float GetDragSpeedRatio(float normalisedCursorPos, float edgeZoneStart)
            {
                if(edgeZoneStart <= 0f)
                {
                    return 0f;
                }

                var dragAmount = 0f;
                var edgeZoneMult = 1f / (1f - edgeZoneStart);
                var directionMult = normalisedCursorPos > 0f ? 1f : -1f;

                dragAmount = Mathf.Clamp01((Mathf.Abs(normalisedCursorPos) - edgeZoneStart) * edgeZoneMult);

                //map drag amount to speed curve
                dragAmount = edgeDragSpeedCurve.Evaluate(dragAmount);
                dragAmount *= directionMult; 

                return dragAmount;
            }
        }
    }

    private void UpdateZoom(float zoomDelta)
    {
        if(zoomDelta == 0f)
        {
            return;
        }

        //var zoomLevel = zoom;
        if (cam.orthographic)
        {
            throw new System.NotImplementedException();
            //var zoom = Mathf.Clamp(zoomLevel + (scrollDelta * zoomSpeed), minZoom * initialCamSize, maxZoom * initialCamSize);
            //cam.orthographicSize = zoom;
        }
        else //perspective camera - move camera forward and backward
        {
            //TODO: stop cam from sliding along rear clamp when trying to zoom out past min
            cam.transform.position += cam.transform.forward * zoomDelta * zoomSpeed;
        }

        zoomDelta = 0f;
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }

    public void OnCursorEvent(Cursor.CursorEvent e)
    {
        if(e == Cursor.CursorEvent.RightClickDown)
        {
            SetDragging(true);
        }

        if(e == Cursor.CursorEvent.MouseMove)
        {
            cursorPos_SS = Cursor.Inst.ClampedPosition_SS;
        }
    }

    #region Input
    private void OnMouseScrollWheel(InputValue value)
    {
        zoomDelta = value.Get<float>();
        //zoomDelta = value.Get<float>() * -1f; // * -1 so scroll up zooms in in orthographpic mode - TODO: make an option?
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = new Color(1f, 1f, 1f, 0.8f);
        var size = ViewMax - ViewMin;
        Gizmos.DrawWireCube(ViewMin + (size / 2f), size); //camera bounds

        if(moveCameraWhenCursorAtScreenEdge)
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
            Gizmos.DrawWireCube(ViewMin + (size / 2f), new Vector3(size.x * edgeZoneStartX, size.y, size.z * edgeZoneStartX)); 
        }
    }
}
