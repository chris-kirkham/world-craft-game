using System.Collections.Generic;
using UnityEngine;

public class CrafterBoard : SingletonMonoBehaviour<CrafterBoard>
{
    [SerializeField] private Vector3 centre;
    [SerializeField] private Vector2 size;
    [SerializeField] private List<CrafterPlacementZone> placementPoints;
    [SerializeField] private Transform cardStackGridOrigin;
    [SerializeField] private Vector2Int stackGridCells;
    [SerializeField] private Vector2 stackGridCellSize;
    [SerializeField] private Transform craftResultSpawnPoint;

    private List<CraftingItemDeck> cardStacks = new List<CraftingItemDeck>();

    public List<CrafterPlacementZone> PlacementPoints => placementPoints;
    
    public Vector3 CraftResultSpawnPos
    {
        get
        {
            if (craftResultSpawnPoint)
            {
                return craftResultSpawnPoint.position;
            }

            Debug.LogError($"No craft result spawn Transform set! Returning (0,0,0).");
            return Vector3.zero;
        }
    }

    private void Start()
    {
        if(CraftingManager.InstExists())
        {
            CraftingManager.Inst.SetBoard(this);
        }
    }

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
        //draw board borders and centre
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
        Gizmos.DrawSphere(centre, 0.5f);
        Gizmos.DrawWireCube(centre, new Vector3(size.x, 0f, size.y));

        for(int x = 0; x < stackGridCells.x; x++)
        {
            for(int y = 0; y < stackGridCells.y; y++)
            {
                var offset = new Vector3(stackGridCellSize.x * x, 0f, stackGridCellSize.y * y);
                Gizmos.DrawSphere(cardStackGridOrigin.position + offset, 0.5f);
                Gizmos.DrawWireCube(cardStackGridOrigin.position + offset, new Vector3(stackGridCellSize.x, 0f, stackGridCellSize.y));
            }
        }
    }
}
