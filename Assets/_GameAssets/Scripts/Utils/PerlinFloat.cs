using UnityEngine;

public class PerlinFloat : MonoBehaviour
{
    [SerializeField] private Vector3 positionFloatAmount;
    [SerializeField] private Vector3 rotationFloatAmount;
    [SerializeField] private float positionFloatSpeed;
    [SerializeField] private float rotationFloatSpeed;
    [Space]
    [SerializeField] private bool useLocalPosition;
    [SerializeField] private bool useLocalRotation;
    [Space]
    [SerializeField] private Transform pivotTransform;
    [Space]
    [SerializeField] private bool rampUpOnEnable;
    [SerializeField, Min(0.01f)] private float rampTime;
    [SerializeField] private AnimationCurve rampCurve;
    [SerializeField] private bool resetOnDisable = true;
    [Space]
    [SerializeField] private float positionSeed;
    [SerializeField] private float rotationSeed;
    [SerializeField] private bool setRandomSeedsOnEnable = true;
 
    private Vector3 initialPos;
    private Quaternion initialRotation;

    private float enableTime;
    private float rampAmt;

    private void OnEnable()
    {
        if(setRandomSeedsOnEnable)
        {
            positionSeed = Random.value * 256f;
            rotationSeed = Random.value * 256f;
        }

        enableTime = Time.time;
        rampAmt = 1f;
    
        UpdateRestingPosition(transform.position);
        UpdateRestingRotation(transform.rotation);
    }

    private void OnDisable()
    {
        if(resetOnDisable)
        {
            if(pivotTransform)
            {
                transform.position = pivotTransform.position;
                transform.rotation = pivotTransform.rotation;
            }
            else
            {
                transform.position = initialPos;
                transform.rotation = initialRotation;
            }
        }
    }

    private void LateUpdate()
    {
        if (rampUpOnEnable)
        {
            var timeSinceEnable = Time.time - enableTime;
            if(timeSinceEnable <= rampTime)
            {
                rampAmt = rampCurve.Evaluate(timeSinceEnable / rampTime);
            }
            else
            {
                rampAmt = 1f;
            }
        }

        var pivotPos = pivotTransform ? pivotTransform.position : initialPos; 
        var pivotRotation = pivotTransform ? pivotTransform.rotation : initialRotation;

        //position
        var posTime = (Time.time + positionSeed) * positionFloatSpeed;
        var posX = (Mathf.PerlinNoise(initialPos.x, posTime) - 0.5f) * positionFloatAmount.x;
        var posY = (Mathf.PerlinNoise(initialPos.y, posTime) - 0.5f) * positionFloatAmount.y;
        var posZ = (Mathf.PerlinNoise(initialPos.z, posTime) - 0.5f) * positionFloatAmount.z;
        var newPos = new Vector3(posX, posY, posZ) * rampAmt;
        if (useLocalPosition)
        {
            transform.localPosition = newPos;
        }
        else
        {
            transform.position = pivotPos + newPos;
        }

        //rotation
        var rotationTime = (Time.time + rotationSeed) * rotationFloatSpeed;
        var rotX = (Mathf.PerlinNoise(initialRotation.x, rotationTime) - 0.5f) * rotationFloatAmount.x;
        var rotY = (Mathf.PerlinNoise(initialRotation.y, rotationTime) - 0.5f) * rotationFloatAmount.y;
        var rotZ = (Mathf.PerlinNoise(initialRotation.z, rotationTime) - 0.5f) * rotationFloatAmount.z;
        var newRotation = Quaternion.Euler(rotX * rampAmt, rotY * rampAmt, rotZ * rampAmt);
        if (useLocalRotation)
        {
            transform.localRotation = newRotation;
        }
        else
        {
            transform.rotation = pivotRotation * newRotation;
        }
    }

    public void UpdateRestingPosition(Vector3 position)
    {
        initialPos = position;
    }

    public void UpdateRestingRotation(Quaternion rotation)
    {
        initialRotation = rotation;
    }

    public void UpdatePivotTransform(Transform pivot)
    {
        pivotTransform = pivot;
    }

}
