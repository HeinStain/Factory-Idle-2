using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GridMap : Grid<GridMap.GridTile> 
{
    
    private GridDebugVisual<GridTile> debugVisual;
    
    public GridMap(int width, int height, float cellSize, Vector2 origin, bool showDebug = true) 
        : base(width, height, cellSize, origin, (grid, x, y) => new GridTile(grid as GridMap, x, y))
    {
        if (showDebug)
        {
            debugVisual = new GridDebugVisual<GridTile>(this);
            debugVisual.ShowDebugVisuals();
        }
    }

#region Setup

    public void Setup(GameManager gameManager)
    {
        gameManager.SetGrid(this);
    }
    
#endregion

#region Get/Set Tile State

    public void SetUnlockCost(int x, int y, int cost) { GetGridObject(x, y)?.SetUnlockCost(cost); }

    public void SetTileVisualState(int x, int y, GridTile.TileVisualState tileVisualState) { GetGridObject(x, y)?.SetTileVisualState(tileVisualState); }

    public GridTile.TileVisualState? GetTileVisualState(int x, int y)
    {
        GridTile gridTile = GetGridObject(x, y);
        return gridTile != null ? gridTile.GetTileVisualState() : null;
    }
    
#endregion


#region >>> Grid Tile Class

    public class GridTile
    {
        public enum TileVisualState 
        {
            CanBuy, Corner, Open, Locked
        }

        private GridMap grid;
        private readonly int x, y;
        private TileVisualState tileVisualState;
        private Machine placedMachine;
        private Belt placedBelt;
        private int unlockCost;


        public GridTile(GridMap grid, int x, int y)
        {
            this.grid = grid;
            this.x = x;
            this.y = y;
        }

        public bool CanBuild() => tileVisualState == TileVisualState.Open && placedMachine == null && placedBelt == null;
        public Machine GetMachine() => placedMachine;
        public Belt GetBelt() => placedBelt;
        public TileVisualState GetTileVisualState() => tileVisualState;
        public int GetUnlockCost() => unlockCost;
        public Vector2 GetTileCentrePosition() => grid.GetTileCentrePosition(x, y);
        public Vector2Int GetTileXY() => new Vector2Int(x, y);
    
        public void SetTileVisualState(TileVisualState tileVisualState)
        {
            this.tileVisualState = tileVisualState;
            grid.TriggerGridObjectChanged(x, y);
        }

        public void SetUnlockCost(int unlockCost)
        {
            this.unlockCost = unlockCost;
            grid.TriggerGridObjectChanged(x, y);
        }

        public void SetMachine(Machine machine)
        {
            if (machine is Belt belt)
            {
                placedBelt = belt;
            }
            else
            {
                placedMachine = machine;
            }
            grid.TriggerGridObjectChanged(x, y);
        }
        public void ClearBelt()
        {
            placedBelt = null;
            grid.TriggerGridObjectChanged(x,y);
        }

        public void ClearMachine()
        {
            placedBelt = null;
            placedMachine = null;
            grid.TriggerGridObjectChanged(x,y);
        }

        public bool IsBlockedForBelt(bool allowOverwriteBelts)
        {
            if (placedMachine != null) return true;                 // machine present => blocked
            if (!allowOverwriteBelts && placedBelt != null) return true;
            return tileVisualState != TileVisualState.Open;         // optional extra checks
        }
        
        ///////////////////////////

        public override string ToString()
        {
            return $"{x},{y}\n{placedMachine}\n{placedBelt}";
        }
    }

#endregion
    
}