using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class Grid <TGridObject>
{
    public event EventHandler<OnGridValueChangedEventArgs> OnGridValueChanged;
    public class OnGridValueChangedEventArgs : EventArgs { public int x, y; }

    private int width, height;
    private float cellSize;
    private Vector2 originPosition;
    private TGridObject[,] gridArray;

#region Grid

    public Grid(int width, int height, float cellSize, Vector2 originPosition, Func<Grid<TGridObject>, int, int, TGridObject> createGridObject) 
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        gridArray = new TGridObject[width, height];

        for (int x = 0; x < width; x ++) 
        {
            for (int y = 0; y < height; y++) 
            {
                gridArray[x, y] = createGridObject(this, x, y);
            }
        }
    }
    
#endregion

#region Grid Access

    // Notify subscribers that a grid cell has changed
    public void TriggerGridObjectChanged(int x, int y) 
    {
        OnGridValueChanged?.Invoke(this, new OnGridValueChangedEventArgs { x = x, y = y });
    }

    // Basic grid property accessors
    public int GetWidth() => width;
    public int GetHeight() => height;
    public float GetCellSize() => cellSize;


    public Vector2 GetTileOriginPosition(int x, int y) { return new Vector2(x, y) * cellSize + originPosition; }
    public Vector2 GetTileCentrePosition(int x, int y) { return new Vector2(x + cellSize/2, y + cellSize/2) * cellSize + originPosition; }


    // Convert world position to grid coordinates
    public void GetXY(Vector2 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        y = Mathf.FloorToInt((worldPosition - originPosition).y / cellSize);
    }

    // Check if coordinates are within grid bounds
    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    // Get grid object at world position
    public TGridObject GetGridObject(Vector2 worldPosition) 
    {
        GetXY(worldPosition, out int x, out int y);
        return GetGridObject(x, y);
    }

    // Get grid object at grid coordinates
    public TGridObject GetGridObject(int x, int y)
    {
        return IsValidPosition(x, y) ? gridArray[x,y] : default;
    }

    // Set grid object at grid coordinates
    public void SetGridObject(int x, int y, TGridObject value)
    {
        if (IsValidPosition(x, y))
        {
            gridArray[x, y] = value;
            TriggerGridObjectChanged(x, y);
        }
    }

    // Set grid object at world position
    public void SetGridObject(Vector2 worldPosition, TGridObject value)
    {
        GetXY(worldPosition, out int x, out int y);
        SetGridObject(x, y, value);
    }

#endregion



}
