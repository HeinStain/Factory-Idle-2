using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Belt : Machine
{
    
    [Header("References")]
    private Grid<GridMap.GridTile> gridArray;
    private LineRenderer lineRenderer;

    [Header("Neighbours")]
    private GridMap.GridTile nextTile;
    private GridMap.GridTile rearTile;
    private GridMap.GridTile rightTile;
    private GridMap.GridTile leftTile;

    [SerializeField] private Belt nextBelt;
    [SerializeField] private Belt rearBelt;
    [SerializeField] private Belt rightBelt;
    [SerializeField] private Belt leftBelt;

    [Header("Item Spots")]
    [SerializeField] private Transform frontSpot;
    [SerializeField] private Transform middleSpot;
    [SerializeField] private Transform rearSpot;
    [SerializeField] private Transform rightSpot;
    [SerializeField] private Transform leftSpot;

    [Header("Data")]
    private int timeToNextTile = 3;
    private bool occupiedByItem;
    private ItemData item;

    // Called on creation
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
    }

#region Neighbour Checks

    public void GetNeighbours()
    {
        GetNeighbourTiles();
        GetNeighbourBelts();

        StartCoroutine(UpdateNeighbours());

        IEnumerator UpdateNeighbours()
        {   
            yield return new WaitForEndOfFrame();
            if (nextBelt != null)  { nextBelt.GetNeighbourBelts();  }
            if (rearBelt != null)  { rearBelt.GetNeighbourBelts();  }
            if (rightBelt != null) { rightBelt.GetNeighbourBelts(); }
            if (leftBelt != null)  { leftBelt.GetNeighbourBelts();  }
        }
    }

    // Get and Set surrounding tiles internally 
    private void GetNeighbourTiles()
    {
        //Uses parent class originXY and machineData to get neighour positions
        nextTile  = GetTile(originXY + MachineData.GetNextTileXY(direction));
        rearTile  = GetTile(originXY + MachineData.GetNextTileXY(direction) * -1);
        rightTile = GetTile(originXY + MachineData.GetRightTileXY(direction));
        leftTile  = GetTile(originXY + MachineData.GetRightTileXY(direction) * -1);

        // Access gridArray and validate tile exists
        GridMap.GridTile GetTile(Vector2Int neighbourXY)
        {
            // Return the tile
            return gridArray.ValidateGridPosition(neighbourXY) ? gridArray.GetGridTile(neighbourXY.x, neighbourXY.y) : null; 
       }
    }

    // Get and Set surrounding Belts internally
    public void GetNeighbourBelts()
    {
        nextBelt = GetBelt(nextTile);
        rearBelt = GetBelt(rearTile);
        rightBelt = GetBelt(rightTile);
        leftBelt = GetBelt(leftTile);

        Belt GetBelt(GridMap.GridTile tile)
        {
            // Check if the tile exists
            if (tile != null)
            {
                // Check for a machine on the tile
                if (tile.GetMachine() != null)
                {
                    // Check if the machine is a belt
                    if (tile.GetMachine().GetMachineName() == "Belt")
                    {
                        //Get the belt's MachineData
                        return (Belt)tile.GetMachine();
                    }
                    else { return null; }
                }
                else { return null; }
            }
            else { return null; }
        }
    }

#endregion

#region Visual

    private void Visuals()
    {
        
    }


#endregion 

#region External Access

    public Belt GetNextBelt() { return nextBelt != null ? nextBelt : null; }
    public Belt GetRearBelt() { return rearBelt != null ? rearBelt : null; }
    public Belt GetRightBelt() { return rightBelt != null ? rightBelt : null; }
    public Belt GetLeftBelt() { return leftBelt != null ? leftBelt : null; }

    public MachineData.Direction GetDirection() { return direction; }

    public MachineData.Direction GetNegativeDirection(Belt belt) 
    { 
        if (belt.GetDirection() == MachineData.Direction.Up) { return MachineData.Direction.Down; }
        else if (belt.GetDirection() == MachineData.Direction.Right) { return MachineData.Direction.Left; } 
        else if (belt.GetDirection() == MachineData.Direction.Down) { return MachineData.Direction.Up; }
        else if (belt.GetDirection() == MachineData.Direction.Left) { return MachineData.Direction.Right; }
        else { return belt.GetDirection(); }
    }

    public MachineData.Direction GetCorrectRightInputDirection(Belt belt)
    {
        if (belt.GetDirection() == MachineData.Direction.Up) { return MachineData.Direction.Left; }
        else if (belt.GetDirection() == MachineData.Direction.Right) { return MachineData.Direction.Up; } 
        else if (belt.GetDirection() == MachineData.Direction.Down) { return MachineData.Direction.Right; }
        else if (belt.GetDirection() == MachineData.Direction.Left) { return MachineData.Direction.Down; }
        else { return belt.GetDirection(); }
    }
    public MachineData.Direction GetCorrectLeftInputDirection(Belt belt)
    {
        if (belt.GetDirection() == MachineData.Direction.Up) { return MachineData.Direction.Right; }
        else if (belt.GetDirection() == MachineData.Direction.Right) { return MachineData.Direction.Down; } 
        else if (belt.GetDirection() == MachineData.Direction.Down) { return MachineData.Direction.Left; }
        else if (belt.GetDirection() == MachineData.Direction.Left) { return MachineData.Direction.Up; }
        else { return belt.GetDirection(); }
    }

    public Transform GetFrontSpot() { return frontSpot; }
    public Transform GetRearSpot() { return rearSpot; }
    public Transform GetLeftSpot() { return leftSpot; }
    public Transform GetRightSpot() { return rightSpot; }
    public Transform GetMiddleSpot() { return middleSpot; }

#endregion
    
#region On Delete
    protected override void ClearMachine()
    {
        // clear self from any belt lists
        BeltManager.Instance.HandleDeletedBelt(this);
        BeltManager.Instance.UpdateDebugVisual();
        Debug.Log("Cleared Belt at " + originXY);
    }
#endregion
}
