using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GridEdgeOverlay : MonoBehaviour
{

    private bool overlayDirty;

    [Header("Render")]
    [SerializeField] private string sortingLayerName = "Grid Overlay";
    [SerializeField] private int sortingOrder = 0;
    [SerializeField] private float edgeThickness = 0.06f;     // one thickness for strips + ticks
    [SerializeField] private Color edgeColor = Color.white;   // one color for everything

    [Header("Ticks")]
    [SerializeField, Range(0f, 1f)] private float tickLength = 0.66f;  // in tiles (multiplied by cell size)
    [SerializeField] private float tickZOffset = -0.0015f;             // render above strips
    [SerializeField, Range(2, 24)] private int tickSegments = 8;       // segments per tick (for smoother fade)
    [SerializeField] private AnimationCurve tickAlphaCurve =            // 0 = base, 1 = tip
        new AnimationCurve(
            new Keyframe(0f, 1f, 0f, -1.2f),
            new Keyframe(0.35f, 0.6f),
            new Keyframe(1f, 0f)
        );

    // vertex → direction bitmask (we emit one nub per direction to get clean L-corners)
    private readonly Dictionary<Vector2Int, int> vertexDirMask = new();
    private const int UP = 1, DOWN = 2, LEFT = 4, RIGHT = 8;

    private MeshFilter mf;
    private MeshRenderer mr;
    private Mesh mesh;
    private GridMap grid;

    // stored delegates so we can unsubscribe correctly
    private EventHandler<GridMap.OnGridValueChangedEventArgs> onGridChanged;
    private Action<bool> onBuyModeChanged;

    private void Awake()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
        mesh = new Mesh { name = "Grid Edge Overlay Mesh" };
        mf.sharedMesh = mesh;

        // Sprites/Default respects vertex colors (needed for tick fade)
        var mat = new Material(Shader.Find("Sprites/Default")) { color = Color.white };
        mr.sharedMaterial = mat;
        mr.sortingLayerName = sortingLayerName;
        mr.sortingOrder = sortingOrder;
        mr.enabled = false;
    }

    public void Setup(GridMap g)
    {
        grid = g;

        onGridChanged    = (_, __) => overlayDirty = true;
        onBuyModeChanged = OnBuyModeChanged;

        grid.OnGridValueChanged += onGridChanged;
        GridBuyManager.Instance.OnBuyModeChanged += onBuyModeChanged;

        Rebuild();
    }

    // Add LateUpdate:
    private void LateUpdate()
    {
        if (overlayDirty)
        {
            overlayDirty = false;
            Rebuild();
        }
    }

    private void OnDestroy()
    {
        if (grid != null && onGridChanged != null) grid.OnGridValueChanged -= onGridChanged;
        if (GridBuyManager.Instance != null && onBuyModeChanged != null)
            GridBuyManager.Instance.OnBuyModeChanged -= onBuyModeChanged;
    }

    private void OnBuyModeChanged(bool isOn)
    {
        mr.enabled = isOn;
        if (isOn) overlayDirty = true;
    }

    private void Rebuild()
    {
        if (grid == null) return;

        mesh.Clear();              // clear FIRST so indices start from 0
        vertexDirMask.Clear();

        var verts  = new List<Vector3>();
        var uvs    = new List<Vector2>();
        var tris   = new List<int>();
        var colors = new List<Color>();

        int w = grid.GetWidth();
        int h = grid.GetHeight();
        float cs = grid.GetCellSize();
        Vector2 origin = grid.GetTileOriginPosition(0, 0);
        const float stripZ = -0.001f;
        float halfEdge = edgeThickness * 0.5f;

        bool IsCanBuy(int x, int y)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return false;
            var s = grid.GetGridObject(x, y).GetTileVisualState();
            return s == GridMap.GridTile.TileVisualState.CanBuy;
        }
        // 1) Build border strips and collect corner direction masks
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            if (grid.GetGridObject(x, y).GetTileVisualState() != GridMap.GridTile.TileVisualState.Open) continue;

            Vector2 bl = origin + new Vector2(x * cs,      y * cs);
            Vector2 br = origin + new Vector2((x + 1)*cs,  y * cs);
            Vector2 tl = origin + new Vector2(x * cs,     (y + 1)*cs);
            Vector2 tr = origin + new Vector2((x + 1)*cs, (y + 1)*cs);

            // bottom
            if (IsCanBuy(x, y - 1)) { AddStrip(bl, br, Vector2.down); Reg(x, y, DOWN); Reg(x+1, y, DOWN); }
            // top
            if (IsCanBuy(x, y + 1)) { AddStrip(tl, tr, Vector2.up);   Reg(x, y+1, UP);  Reg(x+1, y+1, UP);  }
            // left
            if (IsCanBuy(x - 1, y)) { AddStrip(bl, tl, Vector2.left); Reg(x, y, LEFT);  Reg(x, y+1, LEFT);  }
            // right
            if (IsCanBuy(x + 1, y)) { AddStrip(br, tr, Vector2.right);Reg(x+1,y,RIGHT); Reg(x+1,y+1,RIGHT); }


            void AddStrip(Vector2 a, Vector2 b, Vector2 outward)
            {
                Vector2 n = outward.normalized * halfEdge;
                int i = verts.Count;

                verts.Add(new Vector3(a.x + n.x, a.y + n.y, stripZ));
                verts.Add(new Vector3(b.x + n.x, b.y + n.y, stripZ));
                verts.Add(new Vector3(b.x - n.x, b.y - n.y, stripZ));
                verts.Add(new Vector3(a.x - n.x, a.y - n.y, stripZ));

                uvs.Add(Vector2.zero); uvs.Add(Vector2.right); uvs.Add(Vector2.one); uvs.Add(Vector2.up);

                // strips are fully opaque in edgeColor
                colors.Add(edgeColor); colors.Add(edgeColor); colors.Add(edgeColor); colors.Add(edgeColor);

                tris.Add(i + 0); tris.Add(i + 1); tris.Add(i + 2);
                tris.Add(i + 0); tris.Add(i + 2); tris.Add(i + 3);
            }

            void Reg(int vx, int vy, int bit)
            {
                var key = new Vector2Int(vx, vy);
                vertexDirMask[key] = (vertexDirMask.TryGetValue(key, out int m) ? m : 0) | bit;
            }
        }

        // 2) Emit ticks with segmented alpha falloff
        foreach (var kvp in vertexDirMask)
        {
            Vector2Int v = kvp.Key;
            int mask = kvp.Value;
            Vector2 vWorld = origin + new Vector2(v.x * cs, v.y * cs);

            if ((mask & UP)    != 0) AddTick(vWorld, Vector2.up);
            if ((mask & DOWN)  != 0) AddTick(vWorld, Vector2.down);
            if ((mask & LEFT)  != 0) AddTick(vWorld, Vector2.left);
            if ((mask & RIGHT) != 0) AddTick(vWorld, Vector2.right);
        }

        // push mesh
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return;

        // ------- helpers -------
        void AddTick(Vector2 v, Vector2 outward)
        {
            float halfW = edgeThickness * 0.5f;
            float len   = Mathf.Clamp01(tickLength) * cs;

            Vector2 ortho = new Vector2(-outward.y, outward.x).normalized * halfW;
            Vector2 dir   = outward.normalized;

            int segs = Mathf.Max(2, tickSegments);
            for (int s = 0; s < segs; s++)
            {
                float t0 = (float)s / segs;
                float t1 = (float)(s + 1) / segs;

                Vector2 base0 = v + dir * (t0 * len);
                Vector2 base1 = v + dir * (t1 * len);

                Vector3 p0 = new Vector3(base0.x - ortho.x, base0.y - ortho.y, tickZOffset);
                Vector3 p1 = new Vector3(base0.x + ortho.x, base0.y + ortho.y, tickZOffset);
                Vector3 p2 = new Vector3(base1.x + ortho.x, base1.y + ortho.y, tickZOffset);
                Vector3 p3 = new Vector3(base1.x - ortho.x, base1.y - ortho.y, tickZOffset);

                int i = verts.Count;
                verts.Add(p0); verts.Add(p1); verts.Add(p2); verts.Add(p3);
                uvs.Add(Vector2.zero); uvs.Add(Vector2.right); uvs.Add(Vector2.one); uvs.Add(Vector2.up);

                float a0 = Mathf.Clamp01(tickAlphaCurve.Evaluate(t0));
                float a1 = Mathf.Clamp01(tickAlphaCurve.Evaluate(t1));
                Color c0 = edgeColor; c0.a *= a0;
                Color c1 = edgeColor; c1.a *= a1;
                colors.Add(c0); colors.Add(c0); colors.Add(c1); colors.Add(c1);

                tris.Add(i + 0); tris.Add(i + 1); tris.Add(i + 2);
                tris.Add(i + 0); tris.Add(i + 2); tris.Add(i + 3);
            }
        }
    }





}
