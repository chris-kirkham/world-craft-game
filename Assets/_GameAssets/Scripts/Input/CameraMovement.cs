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
    [SerializeField] private Vector2 viewCentre;
    [SerializeField] private Vector2 viewSize;
    [SerializeField] private float moveSpeed = 0.2f;

    private bool isDragging;
    private Vector2 mouseDelta;
    private float initialCamSize;
    private float zoomLevel;

    public Vector2 ViewCentre => viewCentre;
    private Vector2 ViewMin => viewCentre - (viewSize / 2f);
    private Vector2 ViewMax => viewCentre + (viewSize / 2f);

    private void Start()
    {
        initialCamSize = cam.orthographicSize;
        zoomLevel = cam.orthographicSize;

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
        if(isDragging)
        {
            //cam.transform.position += cam.transform.TransformDirection((Vector3)(mouseDelta * moveSpeed));
            cam.transform.position += new Vector3(mouseDelta.x, 0f, mouseDelta.y) * moveSpeed;
        }

        ClampCameraPos();
        
        if(!Cursor.Inst.IsRightClickPressed)
        {
            SetDragging(false);
        }
    }

    private void ClampCameraPos()
    {
        var camHalfSize = cam.rect.size / 2f;
        var camMin = (Vector2)cam.transform.position - camHalfSize;
        var camMax = (Vector2)cam.transform.position + camHalfSize;

        if(camMin.x < ViewMin.x || camMin.y < ViewMin.y)
        {
            var minX = Mathf.Max(camMin.x, ViewMin.x);
            var minY = Mathf.Max(camMin.y, ViewMin.y);

            var centreOffset = camHalfSize;
            cam.transform.position = cam.transform.TransformPoint(
                new Vector3(minX + centreOffset.x, minY + centreOffset.y, cam.transform.localPosition.z));
        }
        else if(camMax.x > ViewMax.x || camMax.y > ViewMax.y)
        {
            var maxX = Mathf.Min(camMax.x, ViewMax.x);
            var maxY = Mathf.Min(camMax.y, ViewMax.y);

            var centreOffset = camHalfSize;
            cam.transform.position = cam.transform.TransformPoint(
                new Vector3(maxX - centreOffset.x, maxY - centreOffset.y, cam.transform.localPosition.z));
        }
    }

    private void SetDragging(bool dragging)
    {
        isDragging = dragging;
        Cursor.Inst.FreezeCursorPos(dragging);
    }

    private void SetZoom(float scrollDelta)
    {
        //var zoomLevel = zoom;
        if (cam.orthographic)
        {
            throw new System.NotImplementedException();
            //var zoom = Mathf.Clamp(zoomLevel + (scrollDelta * zoomSpeed), minZoom * initialCamSize, maxZoom * initialCamSize);
            //cam.orthographicSize = zoom;
        }
        else //perspective camera - move camera forward and backward
        {
            //TODO: clamp cam forward position range
            cam.transform.position += cam.transform.forward * scrollDelta * zoomSpeed;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.green;
        var size = ViewMax - ViewMin;
        Gizmos.DrawWireCube(ViewMin + (size / 2f), size);
    }

    public void OnCursorEvent(Cursor.CursorEvent e)
    {
        if(e == Cursor.CursorEvent.RightClickDown)
        {
            SetDragging(true);
        }
    }

    #region Input
    private void OnMouseScrollWheel(InputValue value)
    {
        var scrollDelta = value.Get<float>();
        //var scrollDelta = value.Get<float>() * -1f; // * -1 so scroll up zooms in in orthographpic mode - TODO: make an option?
        SetZoom(scrollDelta);
    }
    #endregion
}
