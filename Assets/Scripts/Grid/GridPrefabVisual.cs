using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GridPrefabVisual : MonoBehaviour
{
    public static GridPrefabVisual Instance { get; private set; }

    [SerializeField] private Transform gridPrefabVisualNode;

    private List<Transform> visualNodeList;
    private Transform[,] visualNodeArray;
    private Grid<GridMap.GridTile> gridArray;

    [SerializeField] private Sprite openTile;
    [SerializeField] private Sprite wallTile;
    [SerializeField] private Sprite lockedTile;

    private bool buyMode;

    private void Awake() 
    {
        Instance = this;
        visualNodeList = new List<Transform>();
    }

    public void Setup(Grid<GridMap.GridTile> gridArray)
    {
        this.gridArray = gridArray;
        visualNodeArray = new Transform[gridArray.GetWidth(), gridArray.GetHeight()];

        for (int x = 0; x < gridArray.GetWidth(); x++)
        {
            for (int y = 0; y < gridArray.GetHeight(); y++)
            {
                Vector2 gridPosition = gridArray.GetTileOriginPosition(x,y) + Vector2.one * .5f;
                Transform visualNode = CreateVisualNode(gridPosition);
                visualNodeArray[x,y] = visualNode;
                visualNodeList.Add(visualNode);
            }
        }

        HideNodeVisuals();      
        UpdateVisual(gridArray);

        gridArray.OnGridValueChanged += Grid_OnGridValueChanged;
        GridBuyManager.Instance.OnBuyModeChanged += Instance_OnBuyModeChanged;
    }

    private void Grid_OnGridValueChanged(object sender, Grid<GridMap.GridTile>.OnGridValueChangedEventArgs e)
    {
        UpdateVisual(gridArray);
    }

    private void Instance_OnBuyModeChanged(bool arg)
    {
        buyMode = arg;
        UpdateVisual(gridArray);
        Debug.Log(buyMode);
    }

    public void UpdateVisual(Grid<GridMap.GridTile> gridArray)
    {
        HideNodeVisuals();
        
        for (int x = 0; x < gridArray.GetWidth(); x++)
        {
            for (int y = 0; y < gridArray.GetHeight(); y++)
            {
                GridMap.GridTile gridtile = gridArray.GetGridTile(x,y);

                Transform visualNode = visualNodeArray[x,y];
                visualNode.gameObject.SetActive(true);
                SetupVisualNode(visualNode, gridtile);
            }
        }
    }

    private void HideNodeVisuals()
    {
        foreach (Transform visualNodeTransform in visualNodeList)
        {
            visualNodeTransform.gameObject.SetActive(false);
        }
    }

    private Transform CreateVisualNode(Vector2 position)
    {
        Transform visualNodeTransform = Instantiate(gridPrefabVisualNode, position, Quaternion.identity, this.transform);
        return visualNodeTransform;
    }

    private void SetupVisualNode(Transform visualNodeTransform, GridMap.GridTile gridTile)
    {
        SpriteRenderer tileSprite = visualNodeTransform.Find("Sprite").GetComponent<SpriteRenderer>();
        TextMeshPro costText = visualNodeTransform.Find("Cost Text").GetComponent<TextMeshPro>();
    
        switch (gridTile.GetTileState())
        {
            default:
            case GridMap.GridTile.TileState.Locked :
                tileSprite.sprite = lockedTile;
                costText.gameObject.SetActive(false);
                break;
            case GridMap.GridTile.TileState.CanBuy :
                tileSprite.sprite = wallTile;
                costText.text = "$ " + gridTile.GetUnlockCost().ToString();
                if (buyMode) costText.gameObject.SetActive(true);
                else costText.gameObject.SetActive(false);
                break;
            case GridMap.GridTile.TileState.Corner :
                tileSprite.sprite = wallTile;
                costText.gameObject.SetActive(false);
                break;
            case GridMap.GridTile.TileState.Open :
                tileSprite.sprite = openTile;
                costText.gameObject.SetActive(false);
                break;

        }
    }

}
