using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridMap
{
    
    private Grid<GridTile> gridArray;

    public GridMap(int width, int height, float cellSize, Vector2 origin)
    {
        gridArray = new Grid<GridTile>(width, height, cellSize, origin, (Grid<GridTile> tile, int x, int y) => new GridTile(tile, x, y)); 
    }

#region Get/Set Tile State

    public void SetTileState(Vector2 position, GridTile.TileState tileState)
    {
        GridTile gridTile = gridArray.GetGridTile(position);
        if (gridTile != null)
        {
            gridTile.SetTileState(tileState);
        }
    }

    public void SetTileState(int i1, int i2, GridTile.TileState tileState)
    {
        GridTile gridTile = gridArray.GetGridTile(i1, i2);
        if (gridTile != null)
        {
            gridTile.SetTileState(tileState);
        }
    }

    

    public GridTile.TileState? GetTileState(Vector2 position)
    {
        GridTile gridTile = gridArray.GetGridTile(position);
        if (gridTile != null)
        {
            return gridTile.GetTileState();
        }
        else
        {
            return null;
        }
    } 

    public GridTile.TileState? GetTileState(int i1, int i2)
    {
        GridTile gridTile = gridArray.GetGridTile(i1, i2);
        if (gridTile != null)
        {
            return gridTile.GetTileState();
        }
        else
        {
            return null;
        }
    }
    
#endregion

#region Set Game Manager

    public void SetGameManager(GameManager gameManager)
    {
        gameManager.SetGrid(gridArray);
    }
    
#endregion

#region Tile Functions

    public void SetUnlockCost(int i1, int i2, int cost)
    {
        GridTile gridTile = gridArray.GetGridTile(i1, i2);
        if (gridTile != null)
        {
            gridTile.SetUnlockCost(cost);
        }
    }
    
#endregion

#region >>> Grid Tile Class

    public class GridTile
    {
        public enum TileState 
        {
            CanBuy,
            Corner,
            Open,
            Locked
        }


        private Grid<GridTile> gridArray;
        private int x;
        private int y;
        private TileState tileState;
        private Machine machine;
        private int unlockCost;


        public GridTile(Grid<GridTile> gridArray, int x, int y)
        {
            this.gridArray = gridArray;
            this.x = x;
            this.y = y;
        }

        ///////////////////////////
       

        public Vector2Int GetTileXY()
        {
            return new Vector2Int(x,y);
        }


        /// ///////////////////////

        public void SetTileState(TileState tileState)
        {
            this.tileState = tileState;
            gridArray.TriggerGridObjectChanged(x, y);
        }

        public TileState GetTileState() {
            return tileState;
        }

        ///////////////////////////

        public void SetUnlockCost(int unlockCost)
        {
            this.unlockCost = unlockCost;
            gridArray.TriggerGridObjectChanged(x, y);
        }

        public int GetUnlockCost()
        {
            return unlockCost;
        }

        ///////////////////////////

        public Vector2 GetTileWorldPos()
        {
            return gridArray.GetTileCentrePosition(this.x, this.y);
        }
        
        ///////////////////////////

        public void SetMachine(Machine machine)
        {
            this.machine = machine;
            gridArray.TriggerGridObjectChanged(x,y);
        }

        public Machine GetMachine()
        {
            if (machine != null)
            {
                return machine;
            }
            else
            {
                return null;
            }
        }

        public void ClearMachine()
        {
            machine = null;
            gridArray.TriggerGridObjectChanged(x,y);
        }

        public bool CanBuild()
        {
            if (this.tileState == TileState.Open && machine == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////

        public override string ToString()
        {
            //return tileState.ToString() + "\n" + unlockCost;
            //return x + "," + y + "\n" + tileState.ToString() + "\n" + unlockCost + "\n" + machine;
            return x + "," + y + "\n" + machine;
        }
    }

#endregion
    
}