using UnityEngine;

public class CrafterBoard : SingletonMonoBehaviour<CrafterBoard>
{
    [SerializeField] private Vector3 centre;
    [SerializeField] private Vector2 size;

    public Vector3 GetRandomPointOnBoard(float padding)
    {
        var halfSize = size / 2f;
        var minX = centre.x - halfSize.x;
        var maxX = centre.x + halfSize.x;
        var minY = centre.z - halfSize.y;
        var maxY = centre.z + halfSize.y;

        return new Vector3(
            Random.Range(minX + padding, maxX - padding),
            centre.y,
            Random.Range(minY + padding, maxY - padding));
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(centre, 0.5f);
        Gizmos.DrawWireCube(centre, new Vector3(size.x, 0.1f, size.y));
    }
}
