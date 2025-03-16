using UnityEngine;
using TMPro;

public class GridDebugVisual<TGridObject>
{

    private Grid<TGridObject> grid;
    private TextMeshProUGUI[,] debugTextArray;
    private GameObject canvasObject;


    public GridDebugVisual(Grid<TGridObject> grid)
    {
        this.grid = grid;
        debugTextArray = new TextMeshProUGUI[grid.GetWidth(), grid.GetHeight()];
    }




    #region Debug Visuals

    public void ShowDebugVisuals()
    {
        int width = grid.GetWidth();
        int height = grid.GetHeight();
        float cellSize = grid.GetCellSize();

        canvasObject = new GameObject ("Grid_DebugCanvas", typeof(Canvas));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingLayerName = "Build Ghost";

        for (int x = 0; x < width; x ++) 
        {
            for (int y = 0; y < height; y++) 
            {

                // Create text display for cell
                TGridObject gridObject = grid.GetGridObject(x, y);

                debugTextArray[x, y] = CreateDebugText(
                    gridObject?.ToString(), 
                    canvasObject.transform, 
                    grid.GetTileCentrePosition(x, y), 
                    cellSize/4, 
                    TextAlignmentOptions.Center
                );

                // Draw cell borders
                Debug.DrawLine(grid.GetTileOriginPosition(x, y), grid.GetTileOriginPosition(x, y + 1), Color.white, 1000f);
                Debug.DrawLine(grid.GetTileOriginPosition(x, y), grid.GetTileOriginPosition(x + 1, y), Color.white, 1000f);
                
            }
        }
        // Draw outer borders
        Debug.DrawLine(grid.GetTileOriginPosition(0, height), grid.GetTileOriginPosition(width, height), Color.white, 1000f);
        Debug.DrawLine(grid.GetTileOriginPosition(width, 0), grid.GetTileOriginPosition(width, height), Color.white, 1000f);

        // Subscribe to grid changes to update debug text
        grid.OnGridValueChanged += UpdateDebugText;
    }

    private void UpdateDebugText(object sender, Grid<TGridObject>.OnGridValueChangedEventArgs e)
    {
        debugTextArray[e.x, e.y].text = grid.GetGridObject(e.x, e.y)?.ToString();
    }


    // Create Text in the World
    private TextMeshProUGUI CreateDebugText(string text, Transform parent = null, Vector2 localPosition = default, float fontSize = 40, TextAlignmentOptions textAlignment = TextAlignmentOptions.Left) 
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

    public void DestroyDebugVisuals()
    {
        if (canvasObject != null)
        {
            Object.Destroy(canvasObject);
            grid.OnGridValueChanged -= UpdateDebugText;
        }
    }

    #endregion
}
