using UnityEngine;
using UnityEngine.Serialization;

public class DraggableUIElement : DraggableElement
{
    [SerializeField] protected Canvas canvas;
    [SerializeField] protected RectTransform handleRect;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (canvas)
        {
            if (canvas.renderMode != RenderMode.WorldSpace)
            {
                Debug.LogError("Canvas for this element is not world space! " +
                    "Draggable UI elements must use a world-space canvas.");
            }
            else if (!canvas.worldCamera)
            {
                var cam = FindFirstObjectByType<Camera>();
                if (cam)
                {
                    canvas.worldCamera = cam;
                    Debug.Log($"Using {cam.name} as event camera for {nameof(DraggableElement)} {name}'s Canvas.");
                }

                //this warning is for me because it took me too long to figure this out
                Debug.LogWarning($"No camera set for this element's canvas! " +
                    $"(if there is no camera set, graphic raycasts on this element will " +
                    $"use screen- rather than world-space and so will raycast in the wrong place.");
            }
        }
        else
        {
            Debug.LogError($"No Canvas set for {nameof(DraggableElement)} {name}! Dragging will not work properly.");
        }
    }

    private void OnDrawGizmos()
    {
        if (!handleRect)
        {
            return;
        }

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.white;
        var handleRectWorldCorners = new Vector3[4];
        handleRect.GetWorldCorners(handleRectWorldCorners);
        Gizmos.DrawLineStrip(new System.ReadOnlySpan<Vector3>(handleRectWorldCorners), looped: true);
        var size = handleRectWorldCorners[2] - handleRectWorldCorners[0];

        if (isDragging)
        {
            Gizmos.color = new Color(Color.magenta.r, Color.magenta.g, Color.magenta.b, 0.1f);
            Gizmos.DrawCube(handleRectWorldCorners[0] + size / 2, size);
        }
        else if (Cursor.Inst.IsHovered(this))
        {
            Gizmos.color = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.1f);
            Gizmos.DrawCube(handleRectWorldCorners[0] + size / 2, size);
        }
    }
}
