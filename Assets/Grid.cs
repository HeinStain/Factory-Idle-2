using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid 
{

    private int width;
    private int height;
    private float cellSize;
    private Vector3 originPosition;
    private int[,] gridArray;
    

    // Generate a grid
    public Grid(int width, int height, float cellSize, Vector3 originPosition) 
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        gridArray = new int[width, height];

        // Visual
        for (int x = 0; x < width; x ++) 
        {
            for (int y = 0; y < height; y++) 
            {
                Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x, y + 1), Color.white, 100f);
                Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x + 1, y), Color.white, 100f);
            }
        }
        Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, 100f);
        Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 100f);
    }

    // Get world position from grid position
    private Vector3 GetWorldPosition(int x, int y) 
    {
        return new Vector3(x, y) * cellSize + originPosition;
    }

    // Get grid position from world position
    private void GetXY(Vector3 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        y = Mathf.FloorToInt((worldPosition - originPosition).y / cellSize);
    }

    // Set cell value based on grid position
    public void SetValue(int x, int y, int value)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            gridArray[x, y] = value;
        }
    }
    // Convert world position to grid position and set value
    public void SetValue(Vector3 worldPosition, int value)
    {
        int x, y;
        GetXY(worldPosition, out x, out y);
        SetValue(x, y, value);
    }

    //Get cell value from grid position
    public int GetValue(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < width && y < height) 
        {
            return gridArray[x, y];
        } 
        else
        {
            return 0;
        }
    }

    //Convert world position to grid position and get value
    public int GetValue(Vector3 worldPosition) 
    {
        int x, y;
        GetXY(worldPosition, out x , out y);
        return GetValue(x, y);
    }
}
