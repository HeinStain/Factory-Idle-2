using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class GridPrefabVisual : MonoBehaviour
{
    public static GridPrefabVisual Instance { get; private set; }

    [SerializeField] private Transform gridPrefabVisualNode;

    private List<Transform> visualNodeList;
    private Transform[,] visualNodeArray;
    private GridMap gridMap;

    [SerializeField] private Sprite openTile;
    [SerializeField] private Sprite wallTile;

    private bool buyMode;

    private void Awake() 
    {
        Instance = this;
        visualNodeList = new List<Transform>();
    }

    public void Setup(GridMap gridMap)
    {
        this.gridMap = gridMap;
        visualNodeArray = new Transform[gridMap.GetWidth(), gridMap.GetHeight()];

        for (int x = 0; x < gridMap.GetWidth(); x++)
        {
            for (int y = 0; y < gridMap.GetHeight(); y++)
            {
                Vector2 gridPosition = gridMap.GetTileOriginPosition(x,y) + new Vector2(0.5f, 0.5f);
                Transform visualNode = CreateVisualNode(gridPosition);
                visualNodeArray[x,y] = visualNode;
                visualNodeList.Add(visualNode);
            }
        }

        HideNodeVisuals();      
        UpdateVisual(gridMap);

        gridMap.OnGridValueChanged += GridMap_OnGridValueChanged;
        GridBuyManager.Instance.OnBuyModeChanged += Instance_OnBuyModeChanged;
    }

    private void GridMap_OnGridValueChanged(object sender, GridMap.OnGridValueChangedEventArgs e)
    { 
        UpdateVisual(gridMap);
    }



    private void Instance_OnBuyModeChanged(bool arg)
    {
        buyMode = arg;
        UpdateVisual(gridMap);
        Debug.Log(buyMode);
    }

    public void UpdateVisual(GridMap gridMap)
    {
        HideNodeVisuals();
        
        for (int x = 0; x < gridMap.GetWidth(); x++)
        {
            for (int y = 0; y < gridMap.GetHeight(); y++)
            {
                GridMap.GridTile gridtile = gridMap.GetGridObject(x,y);

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
    
        switch (gridTile.GetTileVisualState())
        {
            default:

            case GridMap.GridTile.TileVisualState.Locked :
                tileSprite.enabled = false;
                costText.gameObject.SetActive(false);
                break;

            case GridMap.GridTile.TileVisualState.CanBuy :
                tileSprite.enabled = true;
                tileSprite.sprite = wallTile;
                costText.text = "$ " + gridTile.GetUnlockCost().ToString();
                costText.gameObject.SetActive(buyMode);

                break;

            case GridMap.GridTile.TileVisualState.Corner :
                tileSprite.enabled = true;
                tileSprite.sprite = wallTile;
                costText.gameObject.SetActive(false);
                break;

            case GridMap.GridTile.TileVisualState.Open :
                tileSprite.enabled = true;
                tileSprite.sprite = openTile;
                costText.gameObject.SetActive(false);
                break;

        }
    }

}
