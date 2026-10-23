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

        UpdateAllTiles();

        gridMap.OnGridValueChanged += Grid_OnGridValueChanged;
    }

    private void Grid_OnGridValueChanged(object sender, GridMap.OnGridValueChangedEventArgs e)
    {
        UpdateWallTilesAround(e.x, e.y);
    }

    private void Update()
    {
        if (buyMode == true)
        {
            // Don't buy through UI or while the machine menu is open
            if (Input.GetMouseButtonDown(0) && !GameManager.IsPointerOverUI() && !GameManager.Instance.IsMachineMenuOpen)
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
                    UpdateWallTilesAround(x, y);
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

    bool IsInner(int x, int y) => x > 0 && y > 0 && x < gridMap.GetWidth()-1 && y < gridMap.GetHeight()-1;

    public void UpdateAllTiles()
    {
        // Set Wall Tiles
        for (int x = 0; x < gridMap.GetWidth(); x++) 
        {
            for (int y = 0; y < gridMap.GetHeight(); y++) 
            {
                if (gridMap.GetGridObject(x, y).GetTileVisualState() is GridMap.GridTile.TileVisualState.Locked
                    or GridMap.GridTile.TileVisualState.Corner)
                {
                    if (IsInner(x,y))
                    {
                        if (x > 0                    && gridMap.GetGridObject(x-1, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open) gridMap.GetGridObject(x,y).SetTileVisualState(GridMap.GridTile.TileVisualState.CanBuy);
                        if (x < gridMap.GetWidth()-1 && gridMap.GetGridObject(x+1, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open) gridMap.GetGridObject(x,y).SetTileVisualState(GridMap.GridTile.TileVisualState.CanBuy);
                        if (y > 0                    && gridMap.GetGridObject(x, y-1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open) gridMap.GetGridObject(x,y).SetTileVisualState(GridMap.GridTile.TileVisualState.CanBuy);
                        if (y < gridMap.GetHeight()-1&& gridMap.GetGridObject(x, y+1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open) gridMap.GetGridObject(x,y).SetTileVisualState(GridMap.GridTile.TileVisualState.CanBuy);
                    }
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
                TryMarkBoundaryWall(x,y);
            }
        }
    }









    private void UpdateWallTilesAround(int cx, int cy)
    {
        // Re-eval a tight 3x3 neighborhood
        for (int x = cx - 1; x <= cx + 1; x++)
        for (int y = cy - 1; y <= cy + 1; y++)
        {
            if (!gridMap.IsValidPosition(x, y)) continue;

            var t = gridMap.GetGridObject(x, y);
            if (t == null) continue;

            // 1) Promote to CanBuy if Locked/Corner and adjacent to Open
            if ((t.GetTileVisualState() == GridMap.GridTile.TileVisualState.Locked ||
                 t.GetTileVisualState() == GridMap.GridTile.TileVisualState.Corner))
            {
                if (IsInner(x,y) && (
                    (x > 0                    && gridMap.GetGridObject(x-1, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open) ||
                    (x < gridMap.GetWidth()-1 && gridMap.GetGridObject(x+1, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open) ||
                    (y > 0                    && gridMap.GetGridObject(x, y-1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open) ||
                    (y < gridMap.GetHeight()-1&& gridMap.GetGridObject(x, y+1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open)))
                {
                    t.SetTileVisualState(GridMap.GridTile.TileVisualState.CanBuy);
                }
            }

            // 2) Mark diagonal corners in the same neighborhood
            if (t.GetTileVisualState() == GridMap.GridTile.TileVisualState.Locked)
            {
                bool canLeft  = x > 0;
                bool canRight = x < gridMap.GetWidth()-1;
                bool canDown  = y > 0;
                bool canUp    = y < gridMap.GetHeight()-1;

                // TL
                if (canLeft && canDown &&
                    gridMap.GetGridObject(x-1, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy &&
                    gridMap.GetGridObject(x, y-1).GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy &&
                    gridMap.GetGridObject(x-1, y-1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open)
                    t.SetTileVisualState(GridMap.GridTile.TileVisualState.Corner);

                // BL
                if (canLeft && canUp &&
                    gridMap.GetGridObject(x-1, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy &&
                    gridMap.GetGridObject(x, y+1).GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy &&
                    gridMap.GetGridObject(x-1, y+1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open)
                    t.SetTileVisualState(GridMap.GridTile.TileVisualState.Corner);

                // TR
                if (canRight && canDown &&
                    gridMap.GetGridObject(x+1, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy &&
                    gridMap.GetGridObject(x, y-1).GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy &&
                    gridMap.GetGridObject(x+1, y-1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open)
                    t.SetTileVisualState(GridMap.GridTile.TileVisualState.Corner);

                // BR
                if (canRight && canUp &&
                    gridMap.GetGridObject(x+1, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy &&
                    gridMap.GetGridObject(x, y+1).GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy &&
                    gridMap.GetGridObject(x+1, y+1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open)
                    t.SetTileVisualState(GridMap.GridTile.TileVisualState.Corner);
            }

            TryMarkBoundaryWall(x,y);
        }
    }





    // Boundary visual walls: if a Locked boundary cell touches Open, mark as Corner (visual only)
    void TryMarkBoundaryWall(int x, int y)
    {
        if (!gridMap.IsValidPosition(x, y)) return;

        var t = gridMap.GetGridObject(x, y);
        if (t.GetTileVisualState() != GridMap.GridTile.TileVisualState.Locked) return;

        int w = gridMap.GetWidth();
        int h = gridMap.GetHeight();

        bool isBoundary = x == 0 || y == 0 || x == w - 1 || y == h - 1;
        if (!isBoundary) return;

        // orthogonal opens
        bool openL = x > 0     && gridMap.GetGridObject(x - 1, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open;
        bool openR = x < w - 1 && gridMap.GetGridObject(x + 1, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open;
        bool openD = y > 0     && gridMap.GetGridObject(x, y - 1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open;
        bool openU = y < h - 1 && gridMap.GetGridObject(x, y + 1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open;

        bool touchesOpenOrtho = openL || openR || openD || openU;

        // diagonal opens (needed for convex outer corners)
        bool openUL = x > 0     && y < h - 1 && gridMap.GetGridObject(x - 1, y + 1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open;
        bool openUR = x < w - 1 && y < h - 1 && gridMap.GetGridObject(x + 1, y + 1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open;
        bool openDR = x < w - 1 && y > 0     && gridMap.GetGridObject(x + 1, y - 1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open;
        bool openBL = x > 0     && y > 0     && gridMap.GetGridObject(x - 1, y - 1).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open;

        bool touchesOpenDiag = openUL || openUR || openDR || openBL;

        // mark as Corner if:
        // - it touches an Open orthogonally (your old behavior), OR
        // - it touches Open diagonally but NOT orthogonally (the missing convex-corner case)
        if (touchesOpenOrtho || (!touchesOpenOrtho && touchesOpenDiag))
        {
            t.SetTileVisualState(GridMap.GridTile.TileVisualState.Corner);
        }
    }


































































}
