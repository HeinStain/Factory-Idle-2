using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridBuyManager : MonoBehaviour
{   
    
    public static GridBuyManager Instance { get; private set; }
    
    public event Action<bool> OnBuyModeChanged;

    private Grid<GridMap.GridTile> gridArray;

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
        gridArray = GameManager.Instance.GetGrid();

        UpdateWallTiles();

        gridArray.OnGridValueChanged += Grid_OnGridValueChanged;
    }

    private void Grid_OnGridValueChanged(object sender, Grid<GridMap.GridTile>.OnGridValueChangedEventArgs e)
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
                gridArray.GetXY(mousePosition, out int x, out int y);
                TryToPurchase(x,y);
            }
        }
    }
    
    private void TryToPurchase(int x, int y)
    {
        if (gridArray.ValidateGridPosition(new Vector2Int(x,y)))
        {
            if (gridArray.GetGridTile(x, y).GetTileState() == GridMap.GridTile.TileState.CanBuy)
            {
                //Try to purchase
                if (MoneyManager.Instance.CheckIfEnoughMoney(tileCost))
                {   
                    gridArray.GetGridTile(x, y).SetTileState(GridMap.GridTile.TileState.Open); 
                    MoneyManager.Instance.DecreaseMoney(tileCost);
                    tilesBought++;
                    tileCost = (int)CalculateIncrease(tilesBought);
                    for (int w = 0; w < gridArray.GetWidth(); w++) 
                    {
                        for (int h = 0; h < gridArray.GetHeight(); h++) 
                        {
                            gridArray.GetGridTile(w, h).SetUnlockCost(tileCost);
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
        for (int x = 0; x < gridArray.GetWidth(); x++) 
        {
            for (int y = 0; y < gridArray.GetHeight(); y++) 
            {
                if (gridArray.GetGridTile(x, y).GetTileState() == GridMap.GridTile.TileState.Locked || gridArray.GetGridTile(x, y).GetTileState() == GridMap.GridTile.TileState.Corner)
                {
                    if (x > 0 && gridArray.GetGridTile(x - 1, y).GetTileState() == GridMap.GridTile.TileState.Open)                       { gridArray.GetGridTile(x,y).SetTileState(GridMap.GridTile.TileState.CanBuy); }
                    if (x < gridArray.GetWidth() -1 && gridArray.GetGridTile(x + 1, y).GetTileState() == GridMap.GridTile.TileState.Open)   { gridArray.GetGridTile(x,y).SetTileState(GridMap.GridTile.TileState.CanBuy); }
                    if (y > 0 && gridArray.GetGridTile(x, y - 1).GetTileState() == GridMap.GridTile.TileState.Open)                        { gridArray.GetGridTile(x,y).SetTileState(GridMap.GridTile.TileState.CanBuy); }
                    if (y < gridArray.GetHeight() -1 && gridArray.GetGridTile(x, y + 1).GetTileState() == GridMap.GridTile.TileState.Open)  { gridArray.GetGridTile(x,y).SetTileState(GridMap.GridTile.TileState.CanBuy); }
                }
            }
        }
        // Set Corner Tiles
        for (int x = 0; x < gridArray.GetWidth(); x++) 
        {
            for (int y = 0; y < gridArray.GetHeight(); y++) 
            {
                if (gridArray.GetGridTile(x, y).GetTileState() == GridMap.GridTile.TileState.Locked)
                {
                    if (x > 0 && gridArray.GetGridTile(x - 1, y).GetTileState() == GridMap.GridTile.TileState.CanBuy 
                        && y > 0 && gridArray.GetGridTile(x, y - 1).GetTileState() == GridMap.GridTile.TileState.CanBuy 
                        && gridArray.GetGridTile(x - 1, y - 1).GetTileState() == GridMap.GridTile.TileState.Open)  { gridArray.GetGridTile(x, y).SetTileState(GridMap.GridTile.TileState.Corner); }

                    if (x > 0 && gridArray.GetGridTile(x - 1, y).GetTileState() == GridMap.GridTile.TileState.CanBuy 
                        && y < gridArray.GetHeight() -1 && gridArray.GetGridTile(x, y + 1).GetTileState() == GridMap.GridTile.TileState.CanBuy 
                        && gridArray.GetGridTile(x - 1, y + 1).GetTileState() == GridMap.GridTile.TileState.Open)  { gridArray.GetGridTile(x, y).SetTileState(GridMap.GridTile.TileState.Corner); }

                    if (x < gridArray.GetWidth() -1 && gridArray.GetGridTile(x + 1, y).GetTileState() == GridMap.GridTile.TileState.CanBuy 
                        && y < gridArray.GetHeight() -1 && gridArray.GetGridTile(x, y + 1).GetTileState() == GridMap.GridTile.TileState.CanBuy 
                        && gridArray.GetGridTile(x + 1, y + 1).GetTileState() == GridMap.GridTile.TileState.Open)  { gridArray.GetGridTile(x, y).SetTileState(GridMap.GridTile.TileState.Corner); }

                    if (x < gridArray.GetWidth() -1 && gridArray.GetGridTile(x + 1, y).GetTileState() == GridMap.GridTile.TileState.CanBuy 
                        &&  y > 0 && gridArray.GetGridTile(x, y - 1).GetTileState() == GridMap.GridTile.TileState.CanBuy 
                        && gridArray.GetGridTile(x + 1, y - 1).GetTileState() == GridMap.GridTile.TileState.Open)  { gridArray.GetGridTile(x, y).SetTileState(GridMap.GridTile.TileState.Corner); }

                }
            }
        }
    }
}
