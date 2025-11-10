using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GridPrefabVisual : MonoBehaviour
{
    public static GridPrefabVisual Instance { get; private set; }

    [SerializeField] private Transform gridPrefabVisualNode;

    private List<Transform> visualNodeList;
    private Transform[,] visualNodeArray;
    private GridMap gridMap;
    private bool buyMode;

    [Header("Floor / Fallback")]
    [SerializeField] private Sprite openTile;

    // ------------------ RULE SYSTEM ------------------
    public enum MatchFlag : sbyte
    {
        Any = -1,   // orange
        No  = 0,    // red
        Yes = 1     // green
    }

    [Serializable]
    public struct WallRule
    {
        [Tooltip("Exact key string from the reference guide, e.g., 'TwoWallTR', 'Wall3_2Corner', '2CornerURBL', '3CornerNoTL', '2WallTR_CornerBL'")]
        public string key;
        public Sprite sprite;

        [Header("Orthogonals")]
        public MatchFlag Up;
        public MatchFlag Right;
        public MatchFlag Down;
        public MatchFlag Left;

        [Header("Diagonals")]
        public MatchFlag UpLeft;
        public MatchFlag UpRight;
        public MatchFlag DownRight;
        public MatchFlag DownLeft;

        // Higher is more specific
        public int SpecificityScore()
        {
            int cared = CountCared(Up, Right, Down, Left, UpLeft, UpRight, DownRight, DownLeft);
            int orthoCared = CountCared(Up, Right, Down, Left);
            // Weight orthogonals slightly higher; they define primary shape
            return cared * 10 + orthoCared * 2;
        }

        static int CountCared(params MatchFlag[] vals)
        {
            int c = 0; foreach (var v in vals) if (v != MatchFlag.Any) c++; return c;
        }
    }

    [Header("Rules (ordered by specificity on start)")]
    [SerializeField] private List<WallRule> rules = new List<WallRule>();
    // -------------------------------------------------

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
        for (int y = 0; y < gridMap.GetHeight(); y++)
        {
            Vector2 gridPosition = gridMap.GetTileOriginPosition(x, y) + new Vector2(0.5f, 0.5f);
            Transform visualNode = CreateVisualNode(gridPosition);
            visualNodeArray[x, y] = visualNode;
            visualNodeList.Add(visualNode);
        }

        // Sort rules by specificity (most specific first) so first match wins
        rules.Sort((a, b) => b.SpecificityScore().CompareTo(a.SpecificityScore()));

        HideNodeVisuals();
        UpdateVisual(gridMap);

        gridMap.OnGridValueChanged += GridMap_OnGridValueChanged;
        GridBuyManager.Instance.OnBuyModeChanged += Instance_OnBuyModeChanged;
    }

    private void GridMap_OnGridValueChanged(object sender, GridMap.OnGridValueChangedEventArgs e) => UpdateVisual(gridMap);

    private void Instance_OnBuyModeChanged(bool arg)
    {
        buyMode = arg;
        UpdateVisual(gridMap);
    }

    public void UpdateVisual(GridMap gridMap)
    {
        HideNodeVisuals();

        for (int x = 0; x < gridMap.GetWidth(); x++)
        for (int y = 0; y < gridMap.GetHeight(); y++)
        {
            GridMap.GridTile gridtile = gridMap.GetGridObject(x, y);
            Transform visualNode = visualNodeArray[x, y];
            visualNode.gameObject.SetActive(true);
            SetupVisualNode(visualNode, gridtile);
        }
    }

    private void HideNodeVisuals()
    {
        foreach (Transform visualNodeTransform in visualNodeList)
            visualNodeTransform.gameObject.SetActive(false);
    }

    private Transform CreateVisualNode(Vector2 position)
    {
        Transform visualNodeTransform = Instantiate(gridPrefabVisualNode, position, Quaternion.identity, this.transform);
        return visualNodeTransform;
    }

    // ---------- Autotile core ----------

    // “Unlocked floor” == Open
    private bool IsUnlocked(int x, int y)
    {
        if (!gridMap.IsValidPosition(x, y)) return false;
        return gridMap.GetGridObject(x, y).GetTileVisualState() == GridMap.GridTile.TileVisualState.Open;
    }

    private Sprite PickWallSprite(int x, int y)
    {
        // NOTE: If your world uses y+ down, flip the ±1 for Up/Down *here* only.
        bool U  = IsUnlocked(x,     y + 1);
        bool R  = IsUnlocked(x + 1, y    );
        bool D  = IsUnlocked(x,     y - 1);
        bool L  = IsUnlocked(x - 1, y    );
        bool UL = IsUnlocked(x - 1, y + 1);
        bool UR = IsUnlocked(x + 1, y + 1);
        bool DR = IsUnlocked(x + 1, y - 1);
        bool DL = IsUnlocked(x - 1, y - 1);

        // First rule whose required flags match wins.
        for (int i = 0; i < rules.Count; i++)
        {
            var r = rules[i];
            if (!r.sprite) continue;

            if (!Match(r.Up,        U))  continue;
            if (!Match(r.Right,     R))  continue;
            if (!Match(r.Down,      D))  continue;
            if (!Match(r.Left,      L))  continue;
            if (!Match(r.UpLeft,    UL)) continue;
            if (!Match(r.UpRight,   UR)) continue;
            if (!Match(r.DownRight, DR)) continue;
            if (!Match(r.DownLeft,  DL)) continue;

            return r.sprite;
        }

        return null; // nothing applies -> no wall rendered
    }

    private static bool Match(MatchFlag rule, bool actual)
    {
        if (rule == MatchFlag.Any) return true;       // orange
        if (rule == MatchFlag.Yes) return actual;     // green
        /* No */           return !actual;            // red
    }

    // ---------- Visual hookup ----------

    private void SetupVisualNode(Transform visualNodeTransform, GridMap.GridTile gridTile)
    {
        var tileSprite = visualNodeTransform.Find("Sprite").GetComponent<SpriteRenderer>();
        var costText   = visualNodeTransform.Find("Cost Text").GetComponent<TextMeshPro>();

        switch (gridTile.GetTileVisualState())
        {
            default:
            case GridMap.GridTile.TileVisualState.Locked:
                tileSprite.enabled = false;
                costText.gameObject.SetActive(false);
                break;

            case GridMap.GridTile.TileVisualState.Open:
                tileSprite.enabled = true;
                tileSprite.sprite = openTile;
                costText.gameObject.SetActive(false);
                break;

            case GridMap.GridTile.TileVisualState.CanBuy:
            case GridMap.GridTile.TileVisualState.Corner:
            {
                Vector2Int xy = gridTile.GetTileXY();
                int x = xy.x, y = xy.y;

                Sprite s = PickWallSprite(x, y);
                tileSprite.enabled = s != null;
                tileSprite.sprite = s;

                if (gridTile.GetTileVisualState() == GridMap.GridTile.TileVisualState.CanBuy)
                {
                    costText.text = "$ " + gridTile.GetUnlockCost().ToString();
                    costText.gameObject.SetActive(buyMode);
                }
                else costText.gameObject.SetActive(false);
                break;
            }
        }
    }
}
