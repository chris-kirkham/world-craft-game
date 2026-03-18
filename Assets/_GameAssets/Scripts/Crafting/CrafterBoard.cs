using System.Collections.Generic;
using UnityEngine;

public class CrafterBoard : SingletonMonoBehaviour<CrafterBoard>
{
    [SerializeField] private Vector3 centre;
    [SerializeField] private Vector2 size;
    [SerializeField] private List<CrafterPlacementZone> placementPoints;

    [Header("Grid")]
    [SerializeField] private Transform gridOrigin;
    [SerializeField] private Vector2Int gridCells;
    [SerializeField] private Vector2 gridCellSize;
    [SerializeField] private CraftingItemDeck gridCellDeckPrefab;

    [Header("Crafting transforms")]
    [SerializeField] private Transform craftIngredientFusionPoint;
    [SerializeField] private Transform craftResultSpawnPoint;

    private CraftingItemDeck[,] grid;

    public List<CrafterPlacementZone> PlacementPoints => placementPoints;

    public Vector3 CraftIngredientFusionPos
    {
        get
        {
            if(craftIngredientFusionPoint)
            {
                return craftIngredientFusionPoint.position;
            }

            Debug.LogError($"No craft ingredient fusion Transform set! Returning (0,0,0).");
            return Vector3.zero;
        }
    }

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

    private void OnEnable()
    {
        PopulateGrid();
    }

    private void Start()
    {
        if(CraftingManager.InstExists())
        {
            CraftingManager.Inst.SetBoard(this);
        }
    }

    private void PopulateGrid()
    {
        if (grid != null)
        {
            foreach (var deck in grid)
            {
                Destroy(deck.gameObject);
            }
        }

        grid = new CraftingItemDeck[gridCells.x, gridCells.y];

        for(int x = 0; x < grid.GetLength(0); x++)
        {
            for(int y = 0; y < grid.GetLength(1); y++)
            {
                grid[x, y] = Instantiate<CraftingItemDeck>(
                    gridCellDeckPrefab, GetGridCellPosition(x, y), Quaternion.identity, transform);
            }
        }
    }

    //TODO: prototype, optimise!!
    public void MoveItemToGrid(CraftingItem item, bool stackSameItems = true)
    {
        var firstEmpty = Vector2Int.zero;

        //loop through decks in grid; add item to stack of same items if desired and if one exists,
        //otherwise add item to the first empty deck
        for(int x = 0; x < grid.GetLength(0); x++)
        {
            for(int y = 0; y < grid.GetLength(1); y++)
            {
                var deck = grid[x, y];
                if (deck.IsEmpty())
                {
                    if (firstEmpty == Vector2Int.zero)
                    {
                        firstEmpty = new Vector2Int(x, y);
                        continue;
                    }
                }
                else if (stackSameItems && deck.PeekTopItem().Data == item.Data)
                {
                    deck.AddItemToTopDeck(item);
                    return;
                }
            }
        }

        if (!grid[firstEmpty.x, firstEmpty.y].IsEmpty())
        {
            Debug.LogError($"No empty cell found in deck! What to do here???");
        }

        grid[firstEmpty.x, firstEmpty.y].AddItemToTopDeck(item);
    }

    private Vector3 GetGridCellPosition(int x, int y)
    {
        var offset = new Vector3(gridCellSize.x * x, 0f, gridCellSize.y * y);
        return gridOrigin.position + offset;
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
        if(gridOrigin)
        {
            //draw board borders and centre
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
            Gizmos.DrawSphere(centre, 0.5f);
            Gizmos.DrawWireCube(centre, new Vector3(size.x, 0f, size.y));

            for (int x = 0; x < gridCells.x; x++)
            {
                for (int y = 0; y < gridCells.y; y++)
                {
                    var pos = GetGridCellPosition(x, y);
                    Gizmos.DrawSphere(pos, 0.5f);
                    Gizmos.DrawWireCube(pos, new Vector3(gridCellSize.x, 0f, gridCellSize.y));
                }
            }
        }
    }
}
