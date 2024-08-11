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

        GetNeighbours();
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


    public void GetNeighbours()
    {
        GetNeighbourTiles();
        GetNeighbourBelts();


        StartCoroutine(UpdateNeighbour(nextBelt));
        StartCoroutine(UpdateNeighbour(rearBelt));
        StartCoroutine(UpdateNeighbour(rightBelt));
        StartCoroutine(UpdateNeighbour(leftBelt));


        IEnumerator UpdateNeighbour(Belt belt)
        {   
            yield return new WaitForEndOfFrame();
            if (belt != null) 
            { 
                //Debug.Log("Updating Neighbour: " + belt.originXY);
                belt.GetNeighbourBelts(); 
                //BeltManager.Instance.UpdateBeltPath(belt);
            }
        }

        //BeltManager.Instance.UpdateBeltPath(this);
    }

    private void GetNeighbourTiles()
    {
        nextTile  = GetTile(originXY + MachineData.GetNextTileXY(direction));
        rearTile  = GetTile(originXY + MachineData.GetNextTileXY(direction) * -1);
        rightTile = GetTile(originXY + MachineData.GetRightTileXY(direction));
        leftTile  = GetTile(originXY + MachineData.GetRightTileXY(direction) * -1);

        GridMap.GridTile GetTile(Vector2Int neighbourXY)
        {
            return gridArray.ValidateGridPosition(neighbourXY) ? gridArray.GetGridTile(neighbourXY.x, neighbourXY.y) : null; 
        }
    }

    public void GetNeighbourBelts()
    {
        nextBelt = GetBelt(nextTile);
        rearBelt = GetBelt(rearTile);
        rightBelt = GetBelt(rightTile);
        leftBelt = GetBelt(leftTile);

        Belt GetBelt(GridMap.GridTile tile)
        {
            if (CheckForNeighbourTile(tile))
            {
                if (tile.GetMachine() != null)
                {
                    Belt b = tile.GetMachine().GetMachineName() == "Belt" ? (Belt)tile.GetMachine() : null;
                    //Debug.Log("Tile " + originXY + " Returned belt at " + b.originXY);
                    return b;
                }
                else
                {
                    //Debug.Log("Tile " + originXY + " Returned no machine");
                    return null;
                }
            }
            else
            {
                //Debug.Log("Tile " + originXY + " Returned no tile");
                return null;
            }
        }

        // if (nextBelt != null) { Debug.Log("nextBelt Found"); }
        // if (rearBelt != null) { Debug.Log("rearBelt Found"); }
        // if (rightBelt != null) { Debug.Log("rightBelt Found"); }
        // if (leftBelt != null) { Debug.Log("leftBelt Found"); }
    }


    private bool CheckForNeighbourTile(GridMap.GridTile tile)
    {
        return tile != null ? true : false;
    }

#endregion

#region Neighour External Access

    public Belt GetNextBelt() { return nextBelt == null ? null : nextBelt; }
    public Belt GetRearBelt() { return rearBelt == null ? null : rearBelt; }
    public Belt GetRightBelt() { return rightBelt == null ? null : rightBelt; }
    public Belt GetLeftBelt() { return leftBelt == null ? null : leftBelt; }

    public MachineData.Direction GetDirection() { return direction; }
    

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

    protected override void ClearMachine()
    {
        // clear self from any belt lists
        List<Belt> path = BeltManager.Instance.GetBeltPath(this);
        BeltManager.Instance.RemoveFromPath(this, path);
        BeltManager.Instance.UpdateDebugPathVisual();
        BeltManager.Instance.UpdateDebugListVisual();
        Debug.Log("Cleared Belt at " + originXY);
    }
}
