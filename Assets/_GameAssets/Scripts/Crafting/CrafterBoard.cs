using System.Collections.Generic;
using UnityEngine;

namespace Crafting
{
    public class CrafterBoard : SingletonMonoBehaviour<CrafterBoard>
    {
        [SerializeField] private Vector3 centre;
        [SerializeField] private Vector2 size;
        [SerializeField] private List<CrafterPlacementZone> placementPoints;

        [Header("Grid")]
        [SerializeField] private Transform gridCentre;
        [SerializeField] private Vector2Int gridCells;
        [SerializeField] private Vector2 gridCellSize;
        [SerializeField] private CraftingItemDeck gridCellDeckPrefab;

        private CraftingItemDeck[,] grid;

        private Dictionary<CraftingItemData, int> activeItemCounts = new Dictionary<CraftingItemData, int>(); //{ itemData, number active on board }

        public List<CrafterPlacementZone> PlacementPoints => placementPoints;

        public Dictionary<CraftingItemData, int> ActiveItemCounts => activeItemCounts;

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

        public void MoveItemToGrid(CraftingItem item, bool stackSameItems = true)
        {
            Vector2Int? firstEmpty = null;

            //loop through decks in grid; add item to stack of same items if desired and if one exists,
            //otherwise add item to the first empty deck
            for(int y = 0; y < gridCells.y; y++)
            {
                for(int x = 0; x < gridCells.x; x++)
                {
                    var deck = grid[x, y];
                    if (deck.IsEmpty())
                    {
                        if (firstEmpty == null)
                        {
                            firstEmpty = new Vector2Int(x, y);
                            continue;
                        }
                    }
                    else if (stackSameItems && deck.PeekTopItem().Data == item.Data)
                    {
                        PlaceItemInBoardDeck(item, deck);
                        return;
                    }
                }
            }

            if(firstEmpty.HasValue)
            {
                PlaceItemInBoardDeck(item, grid[firstEmpty.Value.x, firstEmpty.Value.y]);
            }
            else
            {
                Debug.LogError($"No empty cell found in deck! What to do here???");
            }
        }

        void PlaceItemInBoardDeck(CraftingItem item, CraftingItemDeck deck)
        {
            if (deck.TryPlaceObject(item))
            {
                AddToActiveItems(item);
            }
            else
            {
                Debug.LogError($"Tried to add item {item.Data} to grid, but it failed!");
            }
        }

        private Vector3 GetGridCellPosition(int x, int y)
        {
            var cellHalfSize = gridCellSize / 2f;
            var halfSize = new Vector2(cellHalfSize.x * gridCells.x, cellHalfSize.y * gridCells.y);
            var centrePos = gridCentre.position;
            var min = new Vector3(centrePos.x - halfSize.x, centrePos.y, centrePos.z - halfSize.y);
            var offset = new Vector3((gridCellSize.x * x) + cellHalfSize.x, 0f, (gridCellSize.y * y) + cellHalfSize.y);
            return min + offset;
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

        private void AddToActiveItems(CraftingItem item)
        {
            var itemData = item.Data;
            if(activeItemCounts.TryGetValue(itemData, out var count))
            {
                activeItemCounts[itemData] = count + 1;
            }
            else
            {
                activeItemCounts.Add(itemData, 1);
            }
        }

        private void RemoveFromActiveItems(CraftingItem item)
        {
            var itemData = item.Data;
            if (activeItemCounts.TryGetValue(itemData, out var count))
            {
                if(count == 1)
                {
                    activeItemCounts.Remove(itemData);
                }
                else
                {
                    activeItemCounts[itemData] = count - 1;
                }
            }
            else
            {
                Debug.LogError("Tried to remove an item from the active items dictionary, but it wasn't in there!");
            }
        }

        private void OnDrawGizmos()
        {
            if(gridCentre)
            {
                //draw board borders and centre
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
                Gizmos.DrawSphere(centre, 0.5f);
                Gizmos.DrawWireCube(centre, new Vector3(size.x, 0f, size.y));

                for (int y = 0; y < gridCells.y; y++)
                {
                    for (int x = 0; x < gridCells.x; x++)
                    {
                        Gizmos.color = new Color(x / (float)gridCells.x, y / (float)gridCells.y, 0f);
                        var pos = GetGridCellPosition(x, y);
                        Gizmos.DrawSphere(pos, 0.2f);
                        Gizmos.DrawWireCube(pos, new Vector3(gridCellSize.x, 0f, gridCellSize.y));
                    }
                }
            }
        }
    }
}
