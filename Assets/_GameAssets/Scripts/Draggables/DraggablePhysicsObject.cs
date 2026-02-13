using UnityEngine;

public class DraggablePhysicsObject : DraggableElement
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider coll;
    [SerializeField] private float targetDistanceAboveGround = 10f;
    [SerializeField] private LayerMask groundRaycastMask;
    [SerializeField] private float moveSpeed = 1f;

    private const float MinDistFromCamera = 1f;
    private const float MaxRaycastDist = 100f;
    private const int MaxRaycastHits = 20;
    private RaycastHit[] raycastHits = new RaycastHit[MaxRaycastHits];

    protected override void OnEnable()
    {
        base.OnEnable();

        if (!rb)
        {
            Debug.LogError($"No Rigidbody set for this {nameof(DraggablePhysicsObject)}!");
        }

        if(!coll)
        {
            Debug.LogError($"No Collider set for this {nameof(DraggablePhysicsObject)}");
        }
    }

    protected override void OnStartDrag()
    {
        base.OnStartDrag();
        
        if(rb)
        {
            rb.isKinematic = true;
        }

        if(coll)
        {
            coll.enabled = false;
        }
    }

    protected override void OnEndDrag()
    {
        base.OnEndDrag();

        if(rb)
        {
            rb.isKinematic = false;
        }

        if(coll)
        {
            coll.enabled = true;
        }
    }

    private void FixedUpdate()
    {
        if(rb && isDragging)
        {
            var cam = Cursor.Inst.Cam;
            var cursorPos = Cursor.Inst.ClampedPosition_SS;

            //get distance above ground/other objects
            var distFromCamera = targetDistanceAboveGround;
            if(Physics.Raycast(
                cam.ScreenPointToRay(cursorPos), out var hit, MaxRaycastDist, groundRaycastMask, QueryTriggerInteraction.Ignore))
            {
                var hitPointWithHeightOffset = hit.point + (Vector3.up * targetDistanceAboveGround);
                distFromCamera = Mathf.Max(MinDistFromCamera, Vector3.Distance(cam.transform.position, hitPointWithHeightOffset));
                var targetPos_WS = hit.point + ((cam.transform.position - hit.point).normalized * targetDistanceAboveGround);

                rb.MovePosition(targetPos_WS);
            }
        }
    }
}
