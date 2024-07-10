using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiningDrill : Machine
{
    
    [SerializeField] private int timeToMine;
    [SerializeField] private BoxCollider2D clickBox;

    [SerializeField] private GameObject menuCanvas;

    protected override void MachineSetup()
    {
        // Setup
    }

    private void Update()
    {
        if (tickTimer >= timeToMine)
        {
            tickTimer = 0;
            Mine();
        }

        HandleVisuals();
    }


    private void Mine()
    {
        //Debug.Log("Mine()");
        
        ItemManager.SpawnObject(ItemManager.Instance.itemPrefab, ItemManager.Instance.itemList[Random.Range(0, ItemManager.Instance.itemList.Count)], new Vector3(0,0,0), Quaternion.identity);
    }


    public void OpenMenu()
    {
        //Open menu
        menuCanvas.SetActive(true);
        GameManager.Instance.SetMenuOpen(true);
        Debug.Log("OPENINGGGGGGG");
    }

    #region Visuals


    [SerializeField] private GameObject frame;
    [SerializeField] private GameObject tunnel;
    [SerializeField] private GameObject equipment;

    private void HandleVisuals()
    {
        
    }



    #endregion
}
