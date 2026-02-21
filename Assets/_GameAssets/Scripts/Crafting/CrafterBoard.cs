using System.Collections.Generic;
using UnityEngine;

public class CrafterBoard : SingletonMonoBehaviour<CrafterBoard>
{
    public class GridCell
    {
        private Vector2Int coord;
        public CraftingItem item;
    
        public Vector2Int Coord => coord;
        public GridCell(Vector2Int coord)
        {
            this.coord = coord;
        }
    }

    [SerializeField] private Vector3 centre;
    [SerializeField] private Vector2 size;
    [SerializeField] private Vector2Int gridSize;
    [SerializeField] private Vector2 gridCellSize;

    private GridCell[,] grid;

    private Vector2 Min
    {
        get
        {
            var halfSize = size / 2f;
            return new Vector2(centre.x - halfSize.x, centre.z - halfSize.y);
        }
    }

    private void Start()
    {
        ConstructGrid();
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

    public bool TryFindClosestFreeCell(Vector3 pos_WS, out GridCell cell)
    {
        Debug.Log("Finding closest free cell!");
        cell = null;

        var coord = GetGridCoordFromPosition(pos_WS);
        if (!grid[coord.x, coord.y].item)
        {
            cell = grid[coord.x, coord.y];
        }

        int r = 1;
        int x = coord.x;
        int y = coord.y;
        do
        {
            for (x = Mathf.Max(0, coord.x - r); x <= Mathf.Min(coord.x + r, gridSize.x - 1); x++)
            {
                for (y = Mathf.Max(0, coord.y - r); y <= Mathf.Min(coord.y + r, gridSize.y - 1); y++)
                {
                    if (!grid[x, y].item)
                    {
                        cell = grid[x, y];
                        return true;
                    }
                }

                r++;
            }
        }
        while (coord.x - r >= 0 || coord.y - r >= 0 || coord.x + r < gridSize.x || coord.y + r < gridSize.y);

        return false;
    }

    private void ConstructGrid()
    {
        grid = new GridCell[gridSize.x, gridSize.y];
        for(int x = 0; x < gridSize.x; x++)
        {
            for(int y = 0; y < gridSize.y; y++)
            {
                grid[x, y] = new GridCell(new Vector2Int(x, y));
            }
        }
    }

    public void SetGridItem(CraftingItem item, Vector2Int coord)
    {
        if(coord.x < 0 || coord.x >= gridSize.x)
        {
            Debug.LogError($"{nameof(SetGridItem)}: x coord {coord.x} is out of range!");
            return;
        }

        if (coord.y < 0 || coord.y >= gridSize.y)
        {
            Debug.LogError($"{nameof(SetGridItem)}: y coord {coord.y} is out of range!");
            return;
        }

        grid[coord.x, coord.y].item = item;
    }

    public Vector3 GetCellWorldPosFromCoord(Vector2Int coord)
    {
        var cellHalfSize = gridCellSize / 2f;
        return new Vector3(
            (coord.x * gridCellSize.x) + Min.x + cellHalfSize.x,
            centre.y,
            (coord.y * gridCellSize.y) + Min.y + cellHalfSize.y); 
    }

    private Vector2Int GetGridCoordFromPosition(Vector3 posWS)
    {
        var min = Min;
        var pos2DOffset = new Vector2(posWS.x - min.x, posWS.z - min.y); //offset world position so position 0 == grid 0
        var coord = pos2DOffset / gridCellSize; 
        return new Vector2Int((int)coord.x, (int)coord.y);
    }

    private void OnDrawGizmos()
    {
        //draw board borders and centre
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
        Gizmos.DrawSphere(centre, 0.5f);
        Gizmos.DrawWireCube(centre, new Vector3(size.x, 0f, size.y));

        //draw grid cells
        var cellSize = new Vector3(gridCellSize.x, 0f, gridCellSize.y);
        var gridConstructed = grid != null;
        for(int x = 0; x < gridSize.x; x++)
        {
            for(int y = 0; y < gridSize.y; y++)
            {
                if(gridConstructed)
                {
                    Gizmos.color = grid[x, y].item ? Color.yellow : new Color(1f, 1f, 1f, 0.2f);
                }
                
                Gizmos.DrawWireCube(GetCellWorldPosFromCoord(new Vector2Int(x,y)), cellSize); 
            }
        }
    }
}
