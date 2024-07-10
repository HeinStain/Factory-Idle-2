using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Belt : Machine
{
    
    [Header("References")]
    private Grid<GridMap.GridTile> gridArray;
    private LineRenderer lineRenderer;

    [Header("Neighbours")]
    [SerializeField] private GridMap.GridTile nextTile;
    [SerializeField] private GridMap.GridTile rearTile;
    [SerializeField] private GridMap.GridTile rightTile;
    [SerializeField] private GridMap.GridTile leftTile;

    
    [SerializeField] private Belt nextBelt;
    [SerializeField] private Belt rearBelt;
    [SerializeField] private Belt rightBelt;
    [SerializeField] private Belt leftBelt;


    [Header("Data")]
    private int timeToNextTile = 3;
    private bool occupiedByItem;
    private ItemData item;

    protected override void MachineSetup()
    {
        gridArray = GameManager.Instance.GetGrid();
        lineRenderer = GetComponentInChildren<LineRenderer>();

        GetNeighbourTiles();
        GetNeighbourBelts();

        UpdateNeighbour(nextBelt);
        UpdateNeighbour(rearBelt);
        UpdateNeighbour(rightBelt);
        UpdateNeighbour(leftBelt);

        void UpdateNeighbour(Belt belt)
        {
            if (belt != null) 
            { 
                belt.GetNeighbourBelts(); 
            }
        }
    }

    private void Update()
    {
        if (tickTimer >= timeToNextTile)
        {
            tickTimer = 0;
            // AtNextTile();
        }

        //DebugVisuals();
    }

    #region Neighbour Checks

    private void GetNeighbourTiles()
    {
        nextTile  = Get(originXY + MachineData.GetNextTileXY(direction));
        rearTile  = Get(originXY + MachineData.GetNextTileXY(direction) * -1);
        rightTile = Get(originXY + MachineData.GetRightTileXY(direction));
        leftTile  = Get(originXY + MachineData.GetRightTileXY(direction) * -1);

        GridMap.GridTile Get(Vector2Int neighbourXY)
        {
            return gridArray.ValidateGridPosition(neighbourXY) ? gridArray.GetGridTile(neighbourXY.x, neighbourXY.y) : null; 
        }
    }

    public void GetNeighbourBelts()
    {
        nextBelt = Get(nextTile);
        rearBelt = Get(rearTile);
        rightBelt = Get(rightTile);
        leftBelt = Get(leftTile);

        Belt Get(GridMap.GridTile tile)
        {
            if (CheckForNeighbourTile(tile))
            {
                if (tile.GetMachine() != null)
                {
                    Belt name = tile.GetMachine().GetMachineName() == "Belt" ? (Belt)tile.GetMachine() : null;
                    Debug.Log(name);
                    return name;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

    }


    private bool CheckForNeighbourTile(GridMap.GridTile tile)
    {
        return tile != null ? true : false;
    }

    #endregion
    
    #region Visuals

    /* 
    private void DebugVisuals()
    {
        if (CheckForNeighbourTile(nextTile))  { Spawn(1, nextTile);  }
        if (CheckForNeighbourTile(rearTile))  { Spawn(0, rearTile);  }
        if (CheckForNeighbourTile(rightTile)) { Spawn(0, rightTile); }
        if (CheckForNeighbourTile(leftTile))  { Spawn(0, leftTile);  }

        void Spawn(int type, GridMap.GridTile tile)
        {
            if (tile != null)
            {
                ItemManager.SpawnObject(ItemManager.Instance.itemPrefab, ItemManager.Instance.itemList[type], tile.GetTileWorldPos(), Quaternion.identity);
            }
        }
    }
    */
    #endregion
}
