using System.Collections;
using UnityEngine;
using System.Linq;
using UnityEditor.Experimental.GraphView;

public class Item : MonoBehaviour
{
    private bool hasTransferredThisCycle;
    public SpriteRenderer spriteRenderer;
    public ItemData itemData;

    public enum BeltItemState
    {
        IdleOnSlot,
        MovingToExitSlot,
        MovingToNextBeltSlot
    }

    public BeltItemState state;

    private Belt currentBelt;
    private Transform targetSlot;
    private Vector3 movementStart;
    private float transitionProgress;

    private float transitionSpeed = 2 * (TickManager.beltsPerSecond / TickManager.ticksPer); 


    private int arrivalTick;

    private void Start()
    {
        TickManager.OnTick += MoveItem;
    }

    public void ItemSetup(ItemData data, Belt spawnBelt)
    {
        arrivalTick = TickManager.Instance.GetCurrentTick();
        // Set up item
        itemData = data;
        spriteRenderer = GetComponent<SpriteRenderer>();
        //spriteRenderer.sortingLayerName = "Items";
        spriteRenderer.sprite = itemData.icon;

        // Set start belt
        currentBelt = spawnBelt;
        currentBelt.TryAddItem(this, null);

        // Set movement 
        movementStart = currentBelt.rearSlot.position;
        targetSlot = currentBelt.rearSlot; 
        state = BeltItemState.MovingToNextBeltSlot;
        transitionProgress = 0f;
    }

    private void MoveItem(object sender, TickManager.OnTickEventArgs e)
    {
        
        if (currentBelt == null || targetSlot == null) return;

        if (state == BeltItemState.MovingToExitSlot || state == BeltItemState.MovingToNextBeltSlot)
        {
            transitionProgress += transitionSpeed;
            transform.position = Vector3.Lerp(movementStart, targetSlot.position, transitionProgress);
            UpdateSortingOrder();
            
            if (transitionProgress >= 1f)
            {
                transitionProgress = 0f;

                if (state == BeltItemState.MovingToExitSlot)
                {
                    if (!hasTransferredThisCycle)
                    {
                        TryStartTransfer();
                        // ✅ Only transfer once per cycle — moved to when item actually starts moving forward
                    }
                }
                else if (state == BeltItemState.MovingToNextBeltSlot)
                {
                    bool frontOccupied = currentBelt.GetItemsOnBelt().Exists(i => i.GetCurrentSlot() == currentBelt.frontSlot);
                    
                    if (!frontOccupied)
                    {
                        movementStart = transform.position;
                        targetSlot = currentBelt.frontSlot;
                        state = BeltItemState.MovingToExitSlot;
                    }
                    else
                    {
                        state = BeltItemState.IdleOnSlot;
                    }

                    hasTransferredThisCycle = false;
                }
            }
        }

        if (state == BeltItemState.IdleOnSlot)
        {
            bool frontOccupied = currentBelt.GetItemsOnBelt().Exists(i => i.GetCurrentSlot() == currentBelt.frontSlot);

            if (!frontOccupied)
            {
                var items = currentBelt.GetItemsOnBelt();
                bool iAmOldest = items
                    // Get the other items idling on the belt
                    .Where(i => i != this && i.state == BeltItemState.IdleOnSlot)
                    // Return true if they arrived after this item
                    .All(i => i.GetArrivalTick() > this.arrivalTick);
                if (iAmOldest)
                {
                    movementStart = transform.position;
                    targetSlot = currentBelt.frontSlot;
                    state = BeltItemState.MovingToExitSlot;
                    transitionProgress = 0f;
                    hasTransferredThisCycle = false; 
                }
            }
            else if (!hasTransferredThisCycle && targetSlot == currentBelt.frontSlot)
            {
                TryStartTransfer(); 
            }
        }
    }

    private void TryStartTransfer()
    {
        Belt nextBelt = currentBelt.GetNextBelt();
        if (nextBelt != null && nextBelt.HasSpace())
        {
            Belt oldBelt = currentBelt;

            if (nextBelt.TryAddItem(this, currentBelt))
            {
                currentBelt.RemoveItem(this);
                currentBelt = nextBelt;

                movementStart = transform.position;
                currentBelt.GetNeighbours();
                targetSlot = currentBelt.GetInputSlot(oldBelt);
                state = BeltItemState.MovingToNextBeltSlot;
                hasTransferredThisCycle = true;
                transitionProgress = 0f;
            }
            else
            {
                state = BeltItemState.IdleOnSlot;
                transitionProgress = 0f;
            }
        }
        else
        {
            arrivalTick = TickManager.Instance.GetCurrentTick();
            state = BeltItemState.IdleOnSlot;
            transitionProgress = 0f;
        }
    }

    public void AssignSlot(Transform slot) => targetSlot = slot;
    public Transform GetCurrentSlot() => targetSlot;
    
    public int GetArrivalTick() => arrivalTick;


    ////////////////////////////
        
    private void UpdateSortingOrder()
    {
        if (spriteRenderer == null || currentBelt == null) return;

        int baseOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        int offset = 0;

        switch (currentBelt.direction)
        {
            case MachineData.Direction.Right:
                offset = Mathf.RoundToInt(-transform.position.x * 10f);
                break;
            case MachineData.Direction.Left:
                offset = Mathf.RoundToInt(transform.position.x * 10f);
                break;
        }

        spriteRenderer.sortingOrder = baseOrder + offset;
    }
}
