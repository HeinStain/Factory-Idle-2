using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Belt : Machine
{
    
    [Header("Grid")]
    private Grid<GridMap.GridTile> gridArray;


    [Header("Neighbours")]
    private GridMap.GridTile nextAvailableTile, rearTile, rightTile, leftTile;
    [SerializeField] private Belt nextAvailableBelt, rearBelt, rightBelt, leftBelt;


    [Header("Item Spots")]
    [SerializeField] private Transform frontSpot, rearSpot, rightSpot, leftSpot;


    [Header("Visual")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Sprite straight, rightTurn, leftTurn, straightWithRight, straightWithLeft, bothSides, straightWithSides, end, none, start;


    [Header("State")]
    private BeltVisualState visualState;
    private enum BeltVisualState { Straight, RightTurn, LeftTurn, StraightWithRight, StraightWithLeft, BothSides, StraightWithSides, End, None, Start, }
    

    public override string ToString()
    {
        return "Belt";
    }

#region Main

    // Called on creation
    protected override void MachineSetup()
    {
        //References
        gridArray = GameManager.Instance.GetGrid();

        // Set all layers to Belt layer
        SetLayerRecursive(this.gameObject, 3);
        void SetLayerRecursive(GameObject targetObj, int layer)
        {
            targetObj.layer = layer;
            foreach (Transform child in targetObj.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }  

        //Update Path
        GetNeighbours();
        BeltManager.Instance.UpdateBeltPath(this);
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

            // if check for neighbours is a machine but not a belt
            // tell it to update
           // if (nextAvailableTile != null) { if(nextAvailableTile.GetMachine() != null && nextAvailableTile.GetMachine().GetMachineName() != "Belt") { nextAvailableTile.GetMachine().DispenseUpdateCycle(); }}
           // if (rearTile != null)          { if(rearTile.GetMachine() != null && rearTile.GetMachine().GetMachineName() != "Belt") { rearTile.GetMachine().DispenseUpdateCycle(); }}
           // if (rightTile != null)         { if(rightTile.GetMachine() != null && rightTile.GetMachine().GetMachineName() != "Belt") { rightTile.GetMachine().DispenseUpdateCycle(); }}
            //if (leftTile != null)          { if(leftTile.GetMachine() != null && leftTile.GetMachine().GetMachineName() != "Belt") { leftTile.GetMachine().DispenseUpdateCycle(); }}
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

            int stateKey = (NextAvailable() ? 1 : 0) << 3 |
                           (    RearInput() ? 1 : 0) << 2 |
                           (   RightInput() ? 1 : 0) << 1 |
                           (    LeftInput() ? 1 : 0);

            switch (stateKey)
            {
                case 0b0000: visualState = BeltVisualState.None;               break;
                case 0b1000: visualState = BeltVisualState.Start;              break;
                case 0b0100: visualState = BeltVisualState.End;                break;
                case 0b0010: visualState = BeltVisualState.RightTurn;          break;
                case 0b0001: visualState = BeltVisualState.LeftTurn;           break;
                case 0b1100: visualState = BeltVisualState.Straight;           break;
                case 0b1010: visualState = BeltVisualState.RightTurn;          break;
                case 0b1001: visualState = BeltVisualState.LeftTurn;           break;
                case 0b0110: visualState = BeltVisualState.StraightWithRight;  break;
                case 0b0101: visualState = BeltVisualState.StraightWithLeft;   break;
                case 0b0011: visualState = BeltVisualState.BothSides;          break;
                case 0b1110: visualState = BeltVisualState.StraightWithRight;  break;
                case 0b1101: visualState = BeltVisualState.StraightWithLeft;   break;
                case 0b1011: visualState = BeltVisualState.BothSides;          break;
                case 0b0111: visualState = BeltVisualState.StraightWithSides;  break;
                case 0b1111: visualState = BeltVisualState.StraightWithSides;  break;
            }

            switch (visualState)
            {
                case BeltVisualState.Straight:           spriteRenderer.sprite = straight;           break;
                case BeltVisualState.RightTurn:          spriteRenderer.sprite = rightTurn;          break;
                case BeltVisualState.LeftTurn:           spriteRenderer.sprite = leftTurn;           break;
                case BeltVisualState.StraightWithRight:  spriteRenderer.sprite = straightWithRight;  break;
                case BeltVisualState.StraightWithLeft:   spriteRenderer.sprite = straightWithLeft;   break;
                case BeltVisualState.BothSides:          spriteRenderer.sprite = bothSides;          break;
                case BeltVisualState.StraightWithSides:  spriteRenderer.sprite = straightWithSides;  break;
                case BeltVisualState.End:                spriteRenderer.sprite = end;                break;
                case BeltVisualState.None:               spriteRenderer.sprite = none;               break;
                case BeltVisualState.Start:              spriteRenderer.sprite = start;              break;
            }
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


    //Inputs   --->   Return Belt if facing correct direction
    public bool NextAvailable() { return GetNextBelt()  != null && (GetNextBelt()?.GetDirection())  != GetNegativeDirection(this) ? true : false; }
    public bool RearInput()     { return GetRearBelt()  != null && (GetRearBelt()?.GetDirection())  == GetDirection() ? true : false; }
    public bool RightInput()    { return GetRightBelt() != null && (GetRightBelt()?.GetDirection()) == Check_Correct_Input_Direction_From_Right(this) ? true : false; }
    public bool LeftInput()     { return GetLeftBelt()  != null && (GetLeftBelt()?.GetDirection())  == Check_Correct_Input_Direction_From_Left(this) ? true : false; }

    public MachineData.Direction Check_Correct_Input_Direction_From_Right(Belt belt)
    {
             if (belt.GetDirection() == MachineData.Direction.Up)    { return MachineData.Direction.Left;  }
        else if (belt.GetDirection() == MachineData.Direction.Right) { return MachineData.Direction.Up;    } 
        else if (belt.GetDirection() == MachineData.Direction.Down)  { return MachineData.Direction.Right; }
        else if (belt.GetDirection() == MachineData.Direction.Left)  { return MachineData.Direction.Down;  }
        else { return belt.GetDirection(); }
    }
    public MachineData.Direction Check_Correct_Input_Direction_From_Left(Belt belt)
    {
             if (belt.GetDirection() == MachineData.Direction.Up)    { return MachineData.Direction.Right; }
        else if (belt.GetDirection() == MachineData.Direction.Right) { return MachineData.Direction.Down;  } 
        else if (belt.GetDirection() == MachineData.Direction.Down)  { return MachineData.Direction.Left;  }
        else if (belt.GetDirection() == MachineData.Direction.Left)  { return MachineData.Direction.Up;    }
        else { return belt.GetDirection(); }
    }

    public Vector3 GetBeltCentre()
    {
        Vector3 skibidi = new Vector3(transform.position.x, transform.position.y, 0);
        skibidi = direction switch
        {
            MachineData.Direction.Up     =>   new Vector3(skibidi.x += 0.5f, skibidi.y += 0.5f),   
            MachineData.Direction.Right  =>   new Vector3(skibidi.x += 0.5f, skibidi.y -= 0.5f),
            MachineData.Direction.Down   =>   new Vector3(skibidi.x -= 0.5f, skibidi.y -= 0.5f),  
            MachineData.Direction.Left   =>   new Vector3(skibidi.x -= 0.5f, skibidi.y += 0.5f),
            _ => skibidi
            
        };
        return skibidi;
    }

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
