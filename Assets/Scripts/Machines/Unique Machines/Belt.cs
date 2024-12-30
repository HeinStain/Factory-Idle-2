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
    private GridMap.GridTile nextAvailableTile;
    private GridMap.GridTile rearTile;
    private GridMap.GridTile rightTile;
    private GridMap.GridTile leftTile;

    [SerializeField] private Belt nextAvailableBelt;
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
    private int timeTonextAvailableTile = 3;
    private bool occupiedByItem;
    private ItemData item;

    [Header("Visual")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Sprite straight;
    [SerializeField] Sprite rightTurn;
    [SerializeField] Sprite leftTurn;
    [SerializeField] Sprite straightWithRight;
    [SerializeField] Sprite straightWithLeft;
    [SerializeField] Sprite bothSides;
    [SerializeField] Sprite straightWithSides;
    [SerializeField] Sprite end;
    [SerializeField] Sprite none;
    [SerializeField] Sprite start;

    private BeltVisualState visualState;
    private enum BeltVisualState
    {
        Straight,
        RightTurn,
        LeftTurn,
        StraightWithRight,
        StraightWithLeft,
        BothSides,
        StraightWithSides,
        End,
        None,
        Start,
    }
#region Main

    // Called on creation
    protected override void MachineSetup()
    {
        gridArray = GameManager.Instance.GetGrid();
        lineRenderer = GetComponentInChildren<LineRenderer>();

        Transform visuals = this.transform.Find("Visual");
        if (visuals != null)
        {
            SpriteRenderer[] spriteRenderers = visuals.GetComponentsInChildren<SpriteRenderer>();

            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                spriteRenderer.sortingLayerName = "Belts";
            }
        }


        GetNeighbours();
        BeltManager.Instance.UpdateBeltPath(this);
    }

    private void Update()
    {
        if (tickTimer >= timeTonextAvailableTile)
        {
            tickTimer = 0;
            // AtnextAvailableTile();
        }
    }

#endregion

#region Neighbour Checks

    public void GetNeighbours()
    {
        GetNeighbourTiles();
        GetNeighbourBelts();

        StartCoroutine(UpdateNeighbours());

        IEnumerator UpdateNeighbours()
        {   
            yield return new WaitForEndOfFrame();
            if (nextAvailableBelt != null)  { nextAvailableBelt.GetNeighbourBelts();  }
            if (rearBelt != null)  { rearBelt.GetNeighbourBelts();  }
            if (rightBelt != null) { rightBelt.GetNeighbourBelts(); }
            if (leftBelt != null)  { leftBelt.GetNeighbourBelts();  }
        }
    }

    // Get and Set surrounding tiles internally 
    private void GetNeighbourTiles()
    {
        //Uses parent class originXY and machineData to get neighour positions
        nextAvailableTile  = GetTile(originXY + MachineData.GetNextTileXY(direction));
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
        nextAvailableBelt = GetBelt(nextAvailableTile);
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

    public void UpdateVisuals()
    {
        StartCoroutine(Execute());
        IEnumerator Execute()
        {
            yield return new WaitForEndOfFrame();
            if      (!NextAvailable() && !RearInput() && !RightInput() && !LeftInput()) { visualState = BeltVisualState.None; }
            else if ( NextAvailable() && !RearInput() && !RightInput() && !LeftInput()) { visualState = BeltVisualState.Start; }
            else if (!NextAvailable() &&  RearInput() && !RightInput() && !LeftInput()) { visualState = BeltVisualState.End; }
            else if (!NextAvailable() && !RearInput() &&  RightInput() && !LeftInput()) { visualState = BeltVisualState.RightTurn; }
            else if (!NextAvailable() && !RearInput() && !RightInput() &&  LeftInput()) { visualState = BeltVisualState.LeftTurn; }
            else if ( NextAvailable() &&  RearInput() && !RightInput() && !LeftInput()) { visualState = BeltVisualState.Straight; }
            else if ( NextAvailable() && !RearInput() &&  RightInput() && !LeftInput()) { visualState = BeltVisualState.RightTurn; }
            else if ( NextAvailable() && !RearInput() && !RightInput() &&  LeftInput()) { visualState = BeltVisualState.LeftTurn; }
            else if (!NextAvailable() &&  RearInput() &&  RightInput() && !LeftInput()) { visualState = BeltVisualState.StraightWithRight; }
            else if (!NextAvailable() &&  RearInput() && !RightInput() &&  LeftInput()) { visualState = BeltVisualState.StraightWithLeft; }
            else if (!NextAvailable() && !RearInput() &&  RightInput() &&  LeftInput()) { visualState = BeltVisualState.BothSides; }
            else if ( NextAvailable() &&  RearInput() &&  RightInput() && !LeftInput()) { visualState = BeltVisualState.StraightWithRight; }
            else if ( NextAvailable() &&  RearInput() && !RightInput() &&  LeftInput()) { visualState = BeltVisualState.StraightWithLeft; }
            else if ( NextAvailable() && !RearInput() &&  RightInput() &&  LeftInput()) { visualState = BeltVisualState.BothSides; }
            else if (!NextAvailable() &&  RearInput() &&  RightInput() &&  LeftInput()) { visualState = BeltVisualState.StraightWithSides; }
            else if ( NextAvailable() &&  RearInput() &&  RightInput() &&  LeftInput()) { visualState = BeltVisualState.StraightWithSides; }

            if      (visualState == BeltVisualState.Straight)          { spriteRenderer.sprite = straight; }
            else if (visualState == BeltVisualState.RightTurn)         { spriteRenderer.sprite = rightTurn; }
            else if (visualState == BeltVisualState.LeftTurn)          { spriteRenderer.sprite = leftTurn; }
            else if (visualState == BeltVisualState.StraightWithRight) { spriteRenderer.sprite = straightWithRight; }
            else if (visualState == BeltVisualState.StraightWithLeft)  { spriteRenderer.sprite = straightWithLeft; }
            else if (visualState == BeltVisualState.BothSides)         { spriteRenderer.sprite = bothSides; }
            else if (visualState == BeltVisualState.StraightWithSides) { spriteRenderer.sprite = straightWithSides; }
            else if (visualState == BeltVisualState.End)               { spriteRenderer.sprite = end; }
            else if (visualState == BeltVisualState.None)              { spriteRenderer.sprite = none; }
            else if (visualState == BeltVisualState.Start)             { spriteRenderer.sprite = start; }
        }
    }

#endregion 

#region External Access

    //Neighbors
    public Belt GetNextBelt()  { return nextAvailableBelt != null ? nextAvailableBelt : null; }
    public Belt GetRearBelt()  { return rearBelt != null ? rearBelt : null; } 
    public Belt GetRightBelt() { return rightBelt != null ? rightBelt : null; }
    public Belt GetLeftBelt()  { return leftBelt != null ? leftBelt : null; }

    //Directions
    public MachineData.Direction GetDirection() { return direction; }
    public MachineData.Direction GetNegativeDirection(Belt belt) 
    { 
        if (belt.GetDirection() == MachineData.Direction.Up) { return MachineData.Direction.Down; }
        else if (belt.GetDirection() == MachineData.Direction.Right) { return MachineData.Direction.Left; } 
        else if (belt.GetDirection() == MachineData.Direction.Down) { return MachineData.Direction.Up; }
        else if (belt.GetDirection() == MachineData.Direction.Left) { return MachineData.Direction.Right; }
        else { return belt.GetDirection(); }
    }
    
    //Inputs
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

    //Item Spots
    public Transform GetFrontSpot()  { return frontSpot;  }  
    public Transform GetRearSpot()   { return rearSpot;   }
    public Transform GetLeftSpot()   { return leftSpot;   }
    public Transform GetRightSpot()  { return rightSpot;  }
    public Transform GetMiddleSpot() { return middleSpot; }

    //Neighbour Directions
    public MachineData.Direction GetNextBeltDirection()  { return GetNextBelt()?.GetDirection()  ?? default(MachineData.Direction); }
    public MachineData.Direction GetRearBeltDirection()  { return GetRearBelt()?.GetDirection()  ?? default(MachineData.Direction); }
    public MachineData.Direction GetRightBeltDirection() { return GetRightBelt()?.GetDirection() ?? default(MachineData.Direction); }
    public MachineData.Direction GetLeftBeltDirection()  { return GetLeftBelt()?.GetDirection()  ?? default(MachineData.Direction); }

    //Inputs
    public bool NextAvailable() { return GetNextBelt() != null && GetNextBeltDirection() != GetNegativeDirection(this) ? true : false; }
    public bool RearInput() { return GetRearBelt() != null && GetRearBeltDirection() == GetDirection() ? true : false; }
    public bool RightInput() { return GetRightBelt() != null && GetRightBeltDirection() == GetCorrectRightInputDirection(this) ? true : false; }
    public bool LeftInput() { return GetLeftBelt() != null && GetLeftBeltDirection() == GetCorrectLeftInputDirection(this) ? true : false; }

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
