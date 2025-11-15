using UnityEngine;
using System.Collections.Generic;

public class Machine_Grabber : Machine
{
    public override string ToString() => "Grabber";


    [Header("Grid")]
    private GridMap gridMap;
    private GridMap.GridTile sourceTile;
    private GridMap.GridTile destinationTile;


    [Header("Belts")]
    [SerializeField] private Belt sourceBelt;
    [SerializeField] private Belt destinationBelt;


    [Header("Hand")]
    [SerializeField] private Transform handSlot;
    private Item heldItem;

    
    [Header("Timing (ticks)")]
    private int cycleDurationTicks = 90;      // full grab -> drop -> return
    [Range(0f, 1f)] private float dropPointPercent = 0.5f; // when in cycle to attempt drop


    [Header("State")]
    [SerializeField] private GrabberState currentState = GrabberState.Idle;
    private enum GrabberState { Idle, Moving, Blocked }
    private int cycleTimer = 0;
    private bool hasDroppedThisCycle = false;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip moveClip;

    [SerializeField] private string idleStateName    = "Grabber Idle";
    [SerializeField] private string moveStateName    = "Grabber Move";
    [SerializeField] private string blockedStateName = "Grabber Blocked";

    private float moveAnimSpeed = 1f;
    private const float TicksPerSecond = 30f; // you said 30 TPS

#region Main

    protected override void MachineSetup()
    {
        gridMap = GameManager.Instance.GetGrid();

        RefreshConnections();
        SetupAnimation();

        TickManager.OnTick += HandleTick;
        BuildManager.Instance.OnMachinePlaced += HandleMachinePlaced;
    }

    private void OnDestroy()
    {
        TickManager.OnTick -= HandleTick;

        BuildManager.Instance.OnMachinePlaced -= HandleMachinePlaced;
    }

    private void HandleTick(object sender, TickManager.OnTickEventArgs e)
    {
        switch (currentState)
        {
            case GrabberState.Idle:    Idle();    break;

            case GrabberState.Moving:  Moving();  break;
            
            case GrabberState.Blocked: Blocked(); break;
        }

        // While we're holding an item, force it to follow the hand position
    if (heldItem != null && handSlot != null)
    {
        heldItem.transform.position = handSlot.position;
        // Keep item upright / unrotated in world space
        heldItem.transform.rotation = Quaternion.identity;
    }
    }    
    
    private void HandleMachinePlaced(Machine machine)
    {
        if (machine is Belt) RefreshConnections();
    }

#endregion

#region Animation
    private void SetupAnimation()
    {
        if (animator == null || moveClip == null) return;

        // cycleDurationTicks ticks at 30 TPS = seconds per stroke
        float cycleSeconds = cycleDurationTicks / TicksPerSecond;
        if (cycleSeconds <= 0f) cycleSeconds = 0.0001f;

        // Speed so that one full Move clip = one full logical cycle
        moveAnimSpeed = moveClip.length / cycleSeconds;

        // Start in idle pose
        PlayIdleAnimation();
    }

    private void PlayIdleAnimation()
    {
        if (animator == null) return;
        animator.speed = 1f; // let idle fidget at normal speed
        animator.Play(idleStateName, 0, 0f);
    }

    private void PlayMoveAnimation()
    {
        if (animator == null) return;
        animator.speed = moveAnimSpeed;
        animator.Play(moveStateName, 0, 0f);
    }

    private void PlayBlockedAnimation()
    {
        if (animator == null) return;
        animator.speed = 1f;
        animator.Play(blockedStateName, 0, 0f);
    }

#endregion

#region Connections

    private void RefreshConnections()
    {
        if (gridMap == null) return;

        Vector2Int dir = MachineData.GetNextTileXY(direction);

        Vector2Int sourceXY = originXY - dir;
        Vector2Int dropXY   = originXY + dir;

        sourceTile      = gridMap.GetGridObject(sourceXY.x, sourceXY.y);
        destinationTile = gridMap.GetGridObject(dropXY.x, dropXY.y);

        sourceBelt = sourceTile?.GetBelt();
        destinationBelt = destinationTile?.GetBelt();
    }
    
    
#endregion


#region State Logic

    private void Idle()
    {
        if (sourceBelt == null) return;
        
        Item candidate = FindItemToGrab();
        if (candidate == null) return;

        sourceBelt.RemoveItem(candidate);
        candidate.GrabToHand(handSlot);

        heldItem = candidate;
        currentState = GrabberState.Moving;
        cycleTimer = 0;
        hasDroppedThisCycle = false;
        
        PlayMoveAnimation();

        Item FindItemToGrab()
        {
            List<Item> items = sourceBelt.GetItemsOnBelt();
            if (items == null || items.Count == 0) return null;

            Transform rear  = sourceBelt.rearSlot;
            Transform left  = sourceBelt.leftSlot;
            Transform right = sourceBelt.rightSlot;
            Transform front = sourceBelt.frontSlot; // just for clarity

            Item best = null;
            int bestArrivalTick = int.MaxValue;

            foreach (Item item in items)
            {
                if (item == null) continue;

                Transform slot = item.GetCurrentSlot();
                if (slot == null) continue;
                
                // Pickup window: item is in rear/left/right slot.
                // Once it's targeting frontSlot, it's "past" the window.
                if ( slot != front)
                    continue;

                int arrival = item.GetArrivalTick();
                if (arrival < bestArrivalTick)
                {
                    bestArrivalTick = arrival;
                    best = item;
                }
            }

            return best;
        }
    }

    private void Moving()
    {
        cycleTimer++;
        
        int dropTick = Mathf.Max(1, Mathf.RoundToInt(cycleDurationTicks * dropPointPercent));

        // Try to drop at the configured point in the cycle
        if (!hasDroppedThisCycle && cycleTimer >= dropTick)
        {
            if (!TryDropHeldItem())
            {
                // Destination is full / invalid, park in blocked state holding the item
                currentState = GrabberState.Blocked;
                PlayBlockedAnimation();
                return;
            }

            hasDroppedThisCycle = true;
        }

        // Cycle finished
        if (cycleTimer >= cycleDurationTicks)
        {
            // Clean cycle: back to idle at source side
            currentState = GrabberState.Idle;
            cycleTimer = 0;
            hasDroppedThisCycle = false;
            PlayIdleAnimation();
        }
    }

    private void Blocked()
    {
        // We’re parked over the destination holding an item
        if (heldItem == null)
        {
            // Somehow lost the item, just reset
            currentState = GrabberState.Idle;
            cycleTimer = 0;
            hasDroppedThisCycle = false;
            return;
        }

        // Keep retrying until the destination accepts the item
        if (TryDropHeldItem())
        {
            currentState = GrabberState.Idle;
            cycleTimer = 0;
            hasDroppedThisCycle = false;
            PlayIdleAnimation();
        }
    }

#endregion





#region Grabbing / Dropping
    
    private bool TryDropHeldItem()
    {
        if (heldItem == null) return true;           // nothing to drop
        if (destinationBelt == null) return false;   // nowhere to drop
        if (!destinationBelt.HasSpace()) return false;

        // Where are we going to place it? (rear slot by default)
        Transform slot = GetDropSlotForDestinationBelt();
        if (slot == null) return false;
        
        // Belt.TryAddItem(item, null) falls back to rearSlot
        if (!destinationBelt.TryAddItemAtSlot(heldItem, slot)) return false;
        
        // Tell the item it's now controlled by this belt again
        heldItem.AttachToBeltFromGrab(destinationBelt, slot);
        heldItem = null;
        return true;
    }

    private Transform GetDropSlotForDestinationBelt()
    {
        
        if (destinationBelt == null) return null;
        
        // Where is the grabber relative to the belt, in grid space?
        Vector2Int beltXY    = destinationBelt.GetMachineOrigin();
        Vector2Int grabberXY = originXY; // inherited from Machine
        Vector2Int offset    = grabberXY - beltXY; // from belt -> grabber

        // Belt's local directions
        var beltDir    = destinationBelt.direction;
        Vector2Int fwd = MachineData.GetNextTileXY(beltDir);
        Vector2Int right = MachineData.GetRightTileXY(beltDir);
        Vector2Int left  = new Vector2Int(-right.x, -right.y);
        Vector2Int back  = new Vector2Int(-fwd.x, -fwd.y);

        // Compare offset to those directions
        if (offset == back)       return destinationBelt.rearSlot;
        if (offset == right)      return destinationBelt.rightSlot;
        if (offset == left)       return destinationBelt.leftSlot;

        // Fallback: rear if something is weird (diagonal etc.)
        return destinationBelt.rearSlot;
    }


#endregion


#region Cleanup

    protected override void ClearMachine()
    {
        base.ClearMachine();

        if (heldItem != null)
        {
            ItemManager.ReturnObjectToPool(heldItem.gameObject);
            heldItem = null;
        }
    }

#endregion


}
