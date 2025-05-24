using System.Collections;
using UnityEngine;

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
    private float transitionSpeed = 2f / 60f; // 2 seconds per belt

    private void Start()
    {
        TickManager.OnTick += MoveItem;
    }

    public void ItemSetup(ItemData data, Belt spawnBelt)
    {
        // Set up item
        itemData = data;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = "Items";
        spriteRenderer.sprite = itemData.icon;

        // Set start belt
        currentBelt = spawnBelt;
        currentBelt.TryAddItem(this, null);

        // Set movement 
        movementStart = transform.position;
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
                movementStart = transform.position;
                targetSlot = currentBelt.frontSlot;
                state = BeltItemState.MovingToExitSlot;
                transitionProgress = 0f;
                hasTransferredThisCycle = false; 
            }
            else if (!hasTransferredThisCycle)
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
            state = BeltItemState.IdleOnSlot;
            transitionProgress = 0f;
        }
    }

    public void AssignSlot(Transform slot) => targetSlot = slot;
    public Transform GetCurrentSlot() => targetSlot;

}
