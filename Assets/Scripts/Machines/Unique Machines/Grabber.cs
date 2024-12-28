using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grabber : Machine
{
    
    [SerializeField] private int timeToGrab;
    [SerializeField] private Vector2Int grabPosition;
    [SerializeField] private Vector2Int dropPosition;

    protected override void MachineSetup()
    {
        grabPosition = originXY + MachineData.GetNextTileXY(direction) * -1;
        dropPosition = originXY + MachineData.GetNextTileXY(direction);

        Debug.Log(grabPosition + " : " + dropPosition);
    }

    private void Update()
    {
        if (tickTimer >= timeToGrab)
        {
            tickTimer = 0;
            
            GridMap.GridTile grabPosTile = BuildManager.Instance.GetGridTile(grabPosition);
            GridMap.GridTile dropPosTile = BuildManager.Instance.GetGridTile(dropPosition);

            Machine grabPosMachine = grabPosTile != null ? grabPosTile.GetMachine() : null;
            Machine dropPosMachine = dropPosTile != null ? dropPosTile.GetMachine() : null;

            if (grabPosMachine != null && dropPosMachine != null)
            {
                Debug.Log("Grab()");
            }
        }
    }
}
