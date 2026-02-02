using UnityEngine;
using UnityEngine.Serialization;

public class ResizeDragHandle : DraggableUIElement
{
    private enum Side
    {
        Left = 0,
        Right = 1,
        Top = 2,
        Bottom = 3,
        TopLeft = 4,
        TopRight = 5,
        BottomLeft = 6,
        BottomRight = 7
    }

    [SerializeField] private Side side;
    [SerializeField] private Window window;
    [SerializeField, FormerlySerializedAs("cursorSpriteOverride")] private Sprite cursorOverride;

    protected override Sprite OnHoverDragSprite => cursorOverride;

    private RectTransform canvasRect;

    //drag resize pivots
    //(0, 0) == bottom left
    //These pivots are the opposite to the side of the drag bars, so the canvas will move "with" the drag bar 
    private readonly Vector2 resizePivot_Left = new Vector2(1f, 0.5f);
    private readonly Vector2 resizePivot_Right = new Vector2(0f, 0.5f);
    private readonly Vector2 resizePivot_Top = new Vector2(0.5f, 0f);
    private readonly Vector2 resizePivot_Bottom = new Vector2(0.5f, 1f);
    private readonly Vector2 resizePivot_TopLeft = new Vector2(1f, 0f);
    private readonly Vector2 resizePivot_TopRight = new Vector2(0f, 0f);
    private readonly Vector2 resizePivot_BottomLeft = new Vector2(1f, 1f);
    private readonly Vector2 resizePivot_BottomRight = new Vector2(0f, 1f);
    private Vector2[] resizePivots;

    private void Awake()
    {
        resizePivots = new Vector2[]
        {
            resizePivot_Left,
            resizePivot_Right,
            resizePivot_Top,
            resizePivot_Bottom,
            resizePivot_TopLeft,
            resizePivot_TopRight,
            resizePivot_BottomLeft,
            resizePivot_BottomRight
        };

        if(canvas)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
        }
    }

    private void Update()
    {
        if(isDragging && canvasRect)
        {
            var canvasRect = this.canvasRect.rect;
            var mouseDelta = Cursor.Inst.ClampedPositionDelta_WS;
            var newWidth = canvasRect.width;
            var newHeight = canvasRect.height;
            var prevPivot = this.canvasRect.pivot;

            switch(side)
            {
                case Side.Left:
                    newWidth = canvasRect.width - mouseDelta.x;
                    break;
                case Side.Right:
                    newWidth = canvasRect.width + mouseDelta.x;
                    break;
                case Side.Top:
                    newHeight = canvasRect.height + mouseDelta.y;
                    break;
                case Side.Bottom:
                    newHeight = canvasRect.height - mouseDelta.y;
                    break;
                case Side.TopLeft:
                    newWidth = canvasRect.width - mouseDelta.x;
                    newHeight = canvasRect.height + mouseDelta.y;
                    break;
                case Side.TopRight:
                    newWidth = canvasRect.width + mouseDelta.x;
                    newHeight = canvasRect.height + mouseDelta.y;
                    break;
                case Side.BottomLeft:
                    newWidth = canvasRect.width - mouseDelta.x;
                    newHeight = canvasRect.height - mouseDelta.y;
                    break;
                case Side.BottomRight:
                    newWidth = canvasRect.width + mouseDelta.x;
                    newHeight = canvasRect.height - mouseDelta.y;
                    break;
                default:
                    break;
            }

            newWidth = Mathf.Max(newWidth, window.MinSize.x);
            newHeight = Mathf.Max(newHeight, window.MinSize.y);

            this.canvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
            this.canvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
        }
    }

    protected override void OnStartDrag()
    {
        //adjust pivot for each side and reposition window properly
        var prevPivot = canvasRect.pivot;
        canvasRect.pivot = resizePivots[(int)side];
        var pivotDelta = canvasRect.pivot - prevPivot;
        var newPos = canvasRect.position + (Vector3)(canvasRect.rect.size * pivotDelta);
        canvasRect.position = newPos;
    }
}
