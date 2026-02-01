using UnityEngine;

public class RotateGameObject : MonoBehaviour
{
    [SerializeField] private Vector3 rotationSpeed;

    private void LateUpdate()
    {
        gameObject.transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
