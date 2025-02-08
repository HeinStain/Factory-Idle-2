using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{

    public SpriteRenderer spriteRenderer;
    public ItemData itemData;


    private Belt currentBelt;
    List<Belt> currentBeltPath;
    private int currentBeltIndex;
    private float positionAlongBelt;

    private float moveSpeed = 0.5f/30f; // Tiles per Ticks

    private bool transitioningToNextPath = false;
    private Vector3 transitionStart;
    private Vector3 transitionEnd;
    private float transitionProgress = 0f;


    private void Start()
    {
        TickManager.OnTick += MoveItem;
    }

    public void ItemSetup(ItemData data, Belt spawnBelt)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        itemData = data;
        spriteRenderer.sprite = itemData.icon;
    
        currentBelt = spawnBelt;
        currentBeltPath = BeltManager.Instance.GetBeltPath(spawnBelt);
        
        for (int i = 0; i < currentBeltPath.Count; i++)
        {
            if (currentBeltPath[i] == spawnBelt)
            {
                currentBeltIndex = i;
            }
        }

        positionAlongBelt = 0f;
    }

    
    private void MoveItem(object sender, TickManager.OnTickEventArgs e)
    {
        if (currentBelt == null) return;

        // Handle smooth transition between belt paths
        if (transitioningToNextPath)
        {
            transitionProgress += moveSpeed;
            transform.position = Vector3.Lerp(transitionStart, transitionEnd, transitionProgress);

            if (transitionProgress >= 1f)
            {
                transitioningToNextPath = false;

                // Carry over excess movement progress to the next belt
                float excessProgress = transitionProgress - 1f;
                positionAlongBelt = excessProgress;

                // Update the belt path and index
                currentBeltPath = BeltManager.Instance.GetBeltPath(currentBelt);
                currentBeltIndex = 0;

                // Immediately move the item along the new path
                MoveAlongCurrentPath();
            }
            return;
        }

        // Move forward along the belt
        positionAlongBelt += moveSpeed;

        // Handle movement along the current belt path
        if (positionAlongBelt >= 1f)
        {
            // Calculate excess movement progress
            float excessProgress = positionAlongBelt - 1f;

            // Move to the next belt in the path
            currentBeltIndex++;
            if (currentBeltIndex >= currentBeltPath.Count - 1)
            {
                // Prepare for a seamless transition to the next path
                Belt nextBelt = BeltManager.Instance.GetNextBeltPath(currentBeltPath[currentBeltIndex]);
                if (nextBelt != null)
                {
                    // Immediately start transitioning to the next belt
                    transitioningToNextPath = true;
                    transitionStart = currentBeltPath[currentBeltIndex].GetBeltCentre();
                    transitionEnd = nextBelt.GetBeltCentre();
                    transitionProgress = excessProgress; // Carry over excess movement progress

                    currentBelt = nextBelt;
                    // Call MoveItem recursively to continue movement without waiting for the next tick
                    MoveItem(sender, e);
                    return;
                }
                else
                {
                    // No more belts to move to
                    currentBelt = null;
                    return;
                }
            }
            else
            {
                // Move to the next belt in the current path
                currentBelt = currentBeltPath[currentBeltIndex];
                positionAlongBelt = excessProgress; // Carry over excess movement progress
            }
        }

        // Move the item along the current belt path
        MoveAlongCurrentPath();
    }

    private void MoveAlongCurrentPath()
    {
        if (currentBeltPath == null || currentBeltIndex >= currentBeltPath.Count - 1)
        {
            // If this is the only belt in the path or at the last belt, stay at its position
            transform.position = currentBelt.GetBeltCentre();
        }
        else
        {
            // Interpolate between the current belt and the next belt
            Vector3 startPos = currentBelt.GetBeltCentre();
            Vector3 endPos = currentBeltPath[currentBeltIndex + 1].GetBeltCentre();
            transform.position = Vector3.Lerp(startPos, endPos, positionAlongBelt);
        }
    }
}

