using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance { get; private set; }

    [SerializeField] private GridPrefabVisual gridPrefabVisual;
    
    private GridMap gridMap;
    private Grid<GridMap.GridTile> gridArray;

    [Header("Grid Size")]
    [SerializeField] int gridSizeX = 12;
    [SerializeField] int gridSizeY = 8;
    
    [SerializeField] int lowX = 5;
    [SerializeField] int highX = 10;
    [SerializeField] int lowY = 3;
    [SerializeField] int highY = 5;

    //Returned from GridMap.cs
    public void SetGrid(Grid<GridMap.GridTile> gridArray)
    {
        this.gridArray = gridArray;
    }

    //Called by other managers
    public Grid<GridMap.GridTile> GetGrid()
    {
        return gridArray;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {   
        //Create Grid
        gridMap = new GridMap(gridSizeX, gridSizeY, 1f, new Vector2 (-(gridSizeX/2f),-(gridSizeY/2f)));
        gridMap.SetGameManager(this);

        SetBeginningGridStates();
        gridPrefabVisual.Setup(gridArray);
        
        MoneyManager.Instance.IncreaseMoney(1000000);
    }


    private void SetBeginningGridStates()
    {
        int baseCost = 34;

        // Lock all tiles and clear beginning area.
        for (int x = 0; x < gridSizeX; x++) 
        {
            for (int y = 0; y < gridSizeY; y++) 
            {
                if (x >= lowX && x <= highX && y >= lowY && y <= highY) 
                {
                    gridMap.SetTileState(x, y, GridMap.GridTile.TileState.Open);
                    gridMap.SetUnlockCost(x, y, 0);
                } 
                else 
                {
                    gridMap.SetTileState(x, y, GridMap.GridTile.TileState.Locked);
                    gridMap.SetUnlockCost(x, y, baseCost);
                }
            }
        }
    }

    #region Buy Mode

    [SerializeField] private GridBuyManager gridBuyManager;
    [SerializeField] private TextMeshProUGUI gridBuyModeText; 
    private bool buyMode;
    
    public static event Action<bool> OnBuyModeTriggered;

    public void BuyModeTrigger()
    { 
        buyMode = !buyMode;
        if (buyMode == false)
        {
            gridBuyManager.BuyMode(buyMode);
            gridBuyModeText.text = "OFF";
        }
        else if (buyMode == true)
        {
            gridBuyManager.BuyMode(buyMode);
            gridBuyModeText.text = "ON";
            if (buildMode) { BuildModeTrigger(); }
        }
        OnBuyModeTriggered?.Invoke(buyMode);
    }

    #endregion 

    #region Build Mode

    [SerializeField] private BuildManager buildManager;
    [SerializeField] private TextMeshProUGUI buildModeText;
    private bool buildMode;

    public static event Action<bool> OnBuildModeTriggered;
    
    public void BuildModeTrigger()
    { 
        buildMode = !buildMode;
        if (buildMode == false)
        {
            buildManager.BuildMode(buildMode);
            buildModeText.text = "OFF";
        }
        else if (buildMode == true) 
        {
            buildManager.BuildMode(buildMode);
            buildModeText.text = "ON";
            if (buyMode) { BuyModeTrigger(); }
        }
        OnBuildModeTriggered?.Invoke(buildMode);
    }

    #endregion 

  

    #region Menu
 
    public event Action<bool> OnMenuTriggered;
    public void SetMenuOpen(bool status)
    {
        OnMenuTriggered?.Invoke(status);
    }
    #endregion 

}
