using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Belt : Machine
{
    public override string ToString() => "Belt";


    [Header("Data")]
    private GridMap gridMap;
    public bool isIntakeBelt;
    public bool isDispenseBelt;
    [SerializeField] private List<Item> itemsOnBelt = new();


    [Header("Neighbour Belts")]
    private GridMap.GridTile nextAvailableTile, rearTile, rightTile, leftTile;
    [SerializeField] private Belt nextAvailableBelt, rearBelt, rightBelt, leftBelt;


    [Header("Slot Transforms")]
    [SerializeField] public Transform frontSlot;
    [SerializeField] public Transform rearSlot;
    [SerializeField] public Transform leftSlot;
    [SerializeField] public Transform rightSlot;


    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;


    [Header("Shared Visual Library")]
    [SerializeField] private BeltVisualLibrary visuals;   // <-- new


    private BeltVisualState visualState;
    

    protected override void MachineSetup()
    {
        gridMap = GameManager.Instance.GetGrid();
        GetNeighbours();
        UpdateVisuals();
        StartCoroutine(DelayedRefreshNeighbours());
    }

    protected override void ClearMachine()
    {
        // Destroy all items on this belt
        foreach (var item in itemsOnBelt.ToArray())
        {
            if (item != null) Destroy(item.gameObject);
        }
        itemsOnBelt.Clear();

        gridMap.GetGridObject(originXY.x, originXY.y)?.ClearBelt();
        RefreshSurroundingBelts();
    }
    private void Update()
    {    
        float normalizedTime = (BeltSync.sharedTimer * TickManager.beltsPerSecond) % 1f;
        animator.Play(visualState.ToString(), 0, normalizedTime);
    }

    public void GetNeighbours()
    {
        GetNeighbourTiles();
        GetNeighbourBelts();
    }

    private void GetNeighbourTiles()
    {
        nextAvailableTile  = GetTile(originXY + MachineData.GetNextTileXY(direction));
        rearTile  = GetTile(originXY + MachineData.GetNextTileXY(direction) * -1);
        rightTile = GetTile(originXY + MachineData.GetRightTileXY(direction));
        leftTile  = GetTile(originXY + MachineData.GetRightTileXY(direction) * -1);

        GridMap.GridTile GetTile(Vector2Int neighbourXY) => gridMap.GetGridObject(neighbourXY.x, neighbourXY.y); 
    }

    public void GetNeighbourBelts()
    {
        nextAvailableBelt = CheckNextAvailable();
        rearBelt = GetBeltFromTile(rearTile);
        rightBelt = GetBeltFromTile(rightTile);
        leftBelt = GetBeltFromTile(leftTile);

        Belt GetBeltFromTile(GridMap.GridTile tile)
        {
            var candidate = tile?.GetBelt();
            if (candidate == null) return null;

            if (candidate.isIntakeBelt) return null;
            else return candidate;
        }

        Belt CheckNextAvailable()
        {
            Belt candidate = nextAvailableTile?.GetBelt();
            bool machineOnNext = nextAvailableTile.GetMachine() != null;

            // If this is an intake belt, always null
            if (this.isIntakeBelt) return null;     
            // No Next Belt Found
            if (candidate == null) return null;
            // Never connect to a dispense belt
            if (candidate.isDispenseBelt) return null;
            // Only connect to correct direction intake belt
            if (candidate.isIntakeBelt) return candidate.direction == direction ? candidate : null;
            // No machine, Connect if not facing opposite
            if (!machineOnNext && candidate.direction != GetNegativeDirection(this)) return candidate;

            return null;
        }

            
    }

    public void RefreshSurroundingBelts()
    {
        foreach (Belt neighbor in new[] { nextAvailableBelt, rearBelt, rightBelt, leftBelt })
        {
            if (neighbor != null)
            {
                neighbor.GetNeighbours();
                neighbor.UpdateVisuals();
            }
        }
    }

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

            visualState = stateKey switch
            {
                0b0000 => BeltVisualState.None,
                0b1000 => BeltVisualState.Start,
                0b0100 => BeltVisualState.End,
                0b0010 => BeltVisualState.RightTurn,
                0b0001 => BeltVisualState.LeftTurn,
                0b1100 => BeltVisualState.Straight,
                0b1010 => BeltVisualState.RightTurn,
                0b1001 => BeltVisualState.LeftTurn,
                0b0110 => BeltVisualState.StraightWithRight,
                0b0101 => BeltVisualState.StraightWithLeft,
                0b0011 => BeltVisualState.BothSides,
                0b1110 => BeltVisualState.StraightWithRight,
                0b1101 => BeltVisualState.StraightWithLeft,
                0b1011 => BeltVisualState.BothSides,
                0b0111 => BeltVisualState.StraightWithSides,
                0b1111 => BeltVisualState.StraightWithSides,
                _      => BeltVisualState.None
            };

            spriteRenderer.sortingLayerName = "Belts";
        }
    }


    #region Items
    public List<Item> GetItemsOnBelt() => itemsOnBelt;

    public bool HasSpace()
    {
        return !itemsOnBelt.Exists(i => i.GetCurrentSlot() == rearSlot) ||
               !itemsOnBelt.Exists(i => i.GetCurrentSlot() == leftSlot) ||
               !itemsOnBelt.Exists(i => i.GetCurrentSlot() == rightSlot);
    }

    public bool RearSlotAvailable()
    {
        return !itemsOnBelt.Exists(i => i.GetCurrentSlot() == rearSlot);
    }

    public bool TryAddItem(Item item, Belt source)
    {
        Transform inputSlot = GetInputSlot(source);
        if (itemsOnBelt.Exists(i => i.GetCurrentSlot() == inputSlot)) return false;

        item.AssignSlot(inputSlot);
        itemsOnBelt.Add(item);
        return true;
    }

    public void RemoveItem(Item item) => itemsOnBelt.Remove(item);



    public Item PeekFrontItem()
    {
        const float sqrThresh = 0.01f * 0.01f; // ~1 cm in world units; tweak as needed
        return itemsOnBelt.Find(i => i != null && i.GetCurrentSlot() == frontSlot 
                            && (i.transform.position - frontSlot.position).sqrMagnitude <= sqrThresh);
    }

    public Item RemoveFrontItem(bool despawn = false)
    {
        var item = PeekFrontItem();
        if (item == null) return null;

        itemsOnBelt.Remove(item);

        if (despawn && item.gameObject != null)
        {
            ItemManager.ReturnObjectToPool(item.gameObject);
        }

        return item;
    }

    #endregion

    private IEnumerator DelayedRefreshNeighbours()
    {
        yield return new WaitForEndOfFrame();
        RefreshSurroundingBelts();
    }



    #region Neighbours

    public Belt GetNextBelt()  => nextAvailableBelt;
    public Belt GetRearBelt()  => rearBelt; 
    public Belt GetRightBelt() => rightBelt; 
    public Belt GetLeftBelt()  => leftBelt; 

    public Transform GetInputSlot(Belt source)
    {
        if (source == rearBelt) return rearSlot;
        if (source == leftBelt) return leftSlot;
        if (source == rightBelt) return rightSlot;
        return rearSlot; // fallback
    }
    
    #endregion


    #region Nieghour Inputs
    
    public bool NextAvailable() => nextAvailableBelt != null && nextAvailableBelt.direction != GetNegativeDirection(this);
    public bool RearInput()     => rearBelt          != null && rearBelt.direction == direction;
    public bool RightInput()    => rightBelt         != null && rightBelt.direction == CorrectInputFromRight(this);
    public bool LeftInput()     => leftBelt          != null && leftBelt.direction == CorrectInputFromLeft(this);

    private MachineData.Direction CorrectInputFromRight(Belt belt) => belt.direction switch
    {
        MachineData.Direction.Up    => MachineData.Direction.Left,
        MachineData.Direction.Right => MachineData.Direction.Up,
        MachineData.Direction.Down  => MachineData.Direction.Right,
        MachineData.Direction.Left  => MachineData.Direction.Down,
        _ => belt.direction
    };
    
    private MachineData.Direction CorrectInputFromLeft(Belt belt) => belt.direction switch
    {
        MachineData.Direction.Up    => MachineData.Direction.Right,
        MachineData.Direction.Right => MachineData.Direction.Down,
        MachineData.Direction.Down  => MachineData.Direction.Left,
        MachineData.Direction.Left  => MachineData.Direction.Up,
        _ => belt.direction
    };
    #endregion


    #region Negative Direction
    private MachineData.Direction GetNegativeDirection(Belt belt) => belt.direction switch
    {
        MachineData.Direction.Up    => MachineData.Direction.Down,
        MachineData.Direction.Right => MachineData.Direction.Left,
        MachineData.Direction.Down  => MachineData.Direction.Up,
        MachineData.Direction.Left  => MachineData.Direction.Right,
        _ => belt.direction
    };
    #endregion


    #region Belt Info
    public Vector3 GetBeltCentre()
    {
        Vector3 basePos = new Vector3(transform.position.x, transform.position.y, 0);
        return direction switch
        {
            MachineData.Direction.Up     => basePos + new Vector3(0.5f, 0.5f),
            MachineData.Direction.Right  => basePos + new Vector3(0.5f, -0.5f),
            MachineData.Direction.Down   => basePos + new Vector3(-0.5f, -0.5f),
            MachineData.Direction.Left   => basePos + new Vector3(-0.5f, 0.5f),
            _ => basePos
        };
    }

    public override void AssignTag(int tag)
    {
        if (tag == 0) isIntakeBelt = true;
        if (tag == 1) isDispenseBelt = true;
    }
    
    #endregion
}
