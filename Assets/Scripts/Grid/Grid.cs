using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class Grid <TGridObject>
{

    public event EventHandler<OnGridValueChangedEventArgs> OnGridValueChanged;
    public class OnGridValueChangedEventArgs : EventArgs 
    {
        public int x;
        public int y;
    }

    private int width;
    private int height;
    private float cellSize;
    private Vector2 originPosition;
    private TGridObject[,] gridArray;

    // Generate a grid
    public Grid(int width, int height, float cellSize, Vector2 originPosition, Func<Grid<TGridObject>, int, int, TGridObject> createGridObject) 
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        gridArray = new TGridObject[width, height];

        for (int x = 0; x < gridArray.GetLength(0); x ++) 
        {
            for (int y = 0; y < gridArray.GetLength(1); y++) 
            {
                gridArray[x, y] = createGridObject(this, x, y);
            }
        }



        bool showDebug = true;
        if (showDebug) 
        { 
            ShowDebugVisuals(); 
        } 
    }

    ///////////////////////////////////////////////////////////////////////
    
    private void ShowDebugVisuals()
    {
        GameObject canvasObject;
        TextMeshProUGUI[,] debugTextArray = new TextMeshProUGUI[width, height];

        canvasObject = new GameObject ("Canvas", typeof(Canvas));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        for (int x = 0; x < gridArray.GetLength(0); x ++) 
        {
            for (int y = 0; y < gridArray.GetLength(1); y++) 
            {
                debugTextArray[x,y] = CreateDebugText(gridArray[x,y]?.ToString(), canvasObject.transform, GetTileOriginPosition(x,y) + new Vector2(cellSize, cellSize) * .5f, cellSize/4, TextAlignmentOptions.Center);
                Debug.DrawLine(GetTileOriginPosition(x, y), GetTileOriginPosition(x, y + 1), Color.white, 1000f);
                Debug.DrawLine(GetTileOriginPosition(x, y), GetTileOriginPosition(x + 1, y), Color.white, 1000f);
                
            }
        }
        Debug.DrawLine(GetTileOriginPosition(0, height), GetTileOriginPosition(width, height), Color.white, 1000f);
        Debug.DrawLine(GetTileOriginPosition(width, 0), GetTileOriginPosition(width, height), Color.white, 1000f);
        OnGridValueChanged += (object sender, OnGridValueChangedEventArgs eventArgs) => { debugTextArray[eventArgs.x, eventArgs.y].text = gridArray[eventArgs.x, eventArgs.y]?.ToString(); };
    }

    // Create Text in the World
    private TextMeshProUGUI CreateDebugText(string text, Transform parent = null, Vector2 localPosition = default(Vector2), float fontSize = 40, TextAlignmentOptions textAlignment = TextAlignmentOptions.Left) 
    {
        GameObject gameObject = new GameObject("World_Text", typeof(TextMeshProUGUI));
        Transform transform = gameObject.transform;
        transform.SetParent(parent, false);
        transform.localPosition = localPosition;
        
        TextMeshProUGUI textMesh = gameObject.GetComponent<TextMeshProUGUI>();
        textMesh.alignment = textAlignment;
        textMesh.text = text;
        textMesh.fontSize = fontSize;

        return textMesh;
    }

    ///////////////////////////////////////////////////////////////////////


    public int GetWidth()
    {
        return width;
    }

    public int GetHeight()
    {
        return height;
    }

    // Get world position from grid position
    public Vector2 GetTileOriginPosition(int x, int y) 
    {
        return new Vector2(x, y) * cellSize + originPosition;
    }

    public Vector2 GetTileCentrePosition(int x, int y) 
    {
        return new Vector2(x + cellSize/2, y + cellSize/2) * cellSize + originPosition;
    }

    // Get grid position from world position
    public void GetXY(Vector2 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        y = Mathf.FloorToInt((worldPosition - originPosition).y / cellSize);
    }

    ///////////////////////////////////////////////////////////////////////

    /*
    // Convert world position to grid position and set gridTile
    public void SetGridTile(Vector2 worldPosition, TGridObject gridTile)
    {
        int x, y;
        GetXY(worldPosition, out x, out y);
        SetGridTile(x, y, gridTile);
    }

    // Set Value
    public void SetGridTile(int x, int y, TGridObject gridTile)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            gridArray[x, y] = gridTile;
            if (OnGridValueChanged != null) OnGridValueChanged(this, new OnGridValueChangedEventArgs { x = x, y = y });
        }
    }

    */
    public void TriggerGridObjectChanged(int x, int y) 
    {
        if (OnGridValueChanged != null) OnGridValueChanged(this, new OnGridValueChangedEventArgs { x = x, y = y });
    }
    
    ///////////////////////////////////////////////////////////////////////

    // Convert world position to grid position and get gridTile
    public TGridObject GetGridTile(Vector2 worldPosition) 
    {
        int x, y;
        GetXY(worldPosition, out x , out y);
        return GetGridTile(x, y);
    }

    // Get Value
    public TGridObject GetGridTile(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < width && y < height) 
        {
            return gridArray[x, y];
        } else {
            return default(TGridObject);
        }
    }
    
    ///////////////////////////////////////////////////////////////////////

    public bool ValidateGridPosition(Vector2Int gridPosition) 
    {
        if ((gridPosition.x >= 0 && gridPosition.x <= width - 1) && (gridPosition.y >= 0 && gridPosition.y <= height - 1))
        {
            return true;
        }
        else
        {
            return false;
        }
        
    }

}
