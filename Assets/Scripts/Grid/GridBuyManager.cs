using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridBuyManager : MonoBehaviour
{   
    
    public static GridBuyManager Instance { get; private set; }
    
    public event Action<bool> OnBuyModeChanged;

    private GridMap gridMap;

    private bool buyMode;
    
    private int tileCost;
    private int tilesBought;

    private void Awake()
    {
        Instance = this;

        tileCost = 34;
        tilesBought = 0;

        buyMode = false;
    }

    private void Start()
    {
        gridMap = GameManager.Instance.GetGrid();

        UpdateWallTiles();

        gridMap.OnGridValueChanged += Grid_OnGridValueChanged;
    }

    private void Grid_OnGridValueChanged(object sender, GridMap.OnGridValueChangedEventArgs e)
    {
        UpdateWallTiles();
    }

    private void Update()
    {
        if (buyMode == true)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                gridMap.GetXY(mousePosition, out int x, out int y);
                TryToPurchase(x,y);
            }
        }
    }
    
    private void TryToPurchase(int x, int y)
    {
        if (gridMap.GetGridObject(x,y) != null)
        {
            if (gridMap.GetGridObject(x, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy)
            {
                //Try to purchase
                if (MoneyManager.Instance.CheckIfEnoughMoney(tileCost))
                {   
                    gridMap.GetGridObject(x, y).SetTileVisualState(GridMap.GridTile.TileVisualState.Open); 
                    MoneyManager.Instance.DecreaseMoney(tileCost);
                    tilesBought++;
                    tileCost = (int)CalculateIncrease(tilesBought);
                    for (int w = 0; w < gridMap.GetWidth(); w++) 
                    {
                        for (int h = 0; h < gridMap.GetHeight(); h++) 
                        {
                            gridMap.GetGridObject(w, h).SetUnlockCost(tileCost);
                        }
                    }
                    UpdateWallTiles(); 
                }
            }
        }
    }


    public int GetTileCost()
    {
        return tileCost;
    }


    public void BuyMode(bool b)
    {
        buyMode = b;
        
        OnBuyModeChanged?.Invoke(buyMode);
    }

    public static double CalculateIncrease(double tiles)
    {
        double result = 132728500 * (tiles + 2) / Math.Pow(142015, (163 / (122 + tiles)));
        return (int)Math.Floor(result); // Round down to the nearest integer and cast to int
    }

    public void UpdateWallTiles()
    {
        // Set Wall Tiles
        for (int x = 0; x < gridMap.GetWidth(); x++) 
        {
            for (int y = 0; y < gridMap.GetHeight(); y++) 
            {
                if (gridMap.GetGridObject(x, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.Locked || gridMap.GetGridObject(x, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.Corner)
                {
                    if (x > 0 && gridMap.GetGridObject(x - 1, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open)                       { gridMap.GetGridObject(x,y).SetTileVisualState(GridMap.GridTile.TileVisualState.CanBuy); }
                    if (x < gridMap.GetWidth() -1 && gridMap.GetGridObject(x + 1, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open)   { gridMap.GetGridObject(x,y).SetTileVisualState(GridMap.GridTile.TileVisualState.CanBuy); }
                    if (y > 0 && gridMap.GetGridObject(x, y - 1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open)                        { gridMap.GetGridObject(x,y).SetTileVisualState(GridMap.GridTile.TileVisualState.CanBuy); }
                    if (y < gridMap.GetHeight() -1 && gridMap.GetGridObject(x, y + 1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open)  { gridMap.GetGridObject(x,y).SetTileVisualState(GridMap.GridTile.TileVisualState.CanBuy); }
                }
            }
        }
        // Set Corner Tiles
        for (int x = 0; x < gridMap.GetWidth(); x++) 
        {
            for (int y = 0; y < gridMap.GetHeight(); y++) 
            {
                if (gridMap.GetGridObject(x, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.Locked)
                {
                    if (x > 0 && gridMap.GetGridObject(x - 1, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy 
                        && y > 0 && gridMap.GetGridObject(x, y - 1).GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy 
                        && gridMap.GetGridObject(x - 1, y - 1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open)  { gridMap.GetGridObject(x, y).SetTileVisualState(GridMap.GridTile.TileVisualState.Corner); }

                    if (x > 0 && gridMap.GetGridObject(x - 1, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy 
                        && y < gridMap.GetHeight() -1 && gridMap.GetGridObject(x, y + 1).GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy 
                        && gridMap.GetGridObject(x - 1, y + 1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open)  { gridMap.GetGridObject(x, y).SetTileVisualState(GridMap.GridTile.TileVisualState.Corner); }

                    if (x < gridMap.GetWidth() -1 && gridMap.GetGridObject(x + 1, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy 
                        && y < gridMap.GetHeight() -1 && gridMap.GetGridObject(x, y + 1).GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy 
                        && gridMap.GetGridObject(x + 1, y + 1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open)  { gridMap.GetGridObject(x, y).SetTileVisualState(GridMap.GridTile.TileVisualState.Corner); }

                    if (x < gridMap.GetWidth() -1 && gridMap.GetGridObject(x + 1, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy 
                        &&  y > 0 && gridMap.GetGridObject(x, y - 1).GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy 
                        && gridMap.GetGridObject(x + 1, y - 1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open)  { gridMap.GetGridObject(x, y).SetTileVisualState(GridMap.GridTile.TileVisualState.Corner); }

                }
            }
        }
    }
}
