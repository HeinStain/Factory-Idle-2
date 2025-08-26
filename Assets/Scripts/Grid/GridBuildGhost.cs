using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GridBuildGhost : MonoBehaviour
{
    
    public static GridBuildGhost Instance { get; private set; }
    private void Awake() => Instance = this;

    private Transform visual;
    private bool buildMode;
    private GridMap gridMap;


    [Header("Belt Ghost Prefab")]
    [SerializeField] private GameObject beltGhostPrefab;  


    [Header("Shared Visual Library")]
    [SerializeField] private BeltVisualLibrary visuals;  


    private readonly List<GameObject> pool = new();
    private static readonly Color GhostTint = new(0.4f, 0.7f, 1f, 0.85f);


    private void Start()
    {
        gridMap = GameManager.Instance.GetGrid();
        RefreshVisual();

        buildMode = false;
        Instance_OnBuildModeChanged(buildMode);

        BuildManager.Instance.OnSelectedChanged += RefreshVisual;
        BuildManager.Instance.OnBuildModeChanged += Instance_OnBuildModeChanged;
    }

    private void RefreshVisual()
    {
        if (visual != null) 
        {
            Destroy(visual.gameObject);
            visual = null;
        }

        MachineData machineData = BuildManager.Instance.heldMachine;

        if (machineData != null)
        {
            visual = Instantiate(machineData.visual, Vector2.zero, Quaternion.identity, transform);
            visual.localPosition = Vector2.zero;
            visual.localEulerAngles = Vector2.zero;
            SetLayerRecursive(visual.gameObject, LayerMask.NameToLayer("Build Ghost"));
        }
    }
    
    private void Instance_OnBuildModeChanged(bool arg)
    {
        buildMode = arg;
        if (visual == null) return;
        visual.localScale = buildMode ? Vector2.one : Vector2.zero;
    }

    private void LateUpdate()
    {
        if (!buildMode) return;

        var bm = BuildManager.Instance;
        var machine = bm.heldMachine;

        // default reset (for non-belt or idle states)
        if (visual != null)
        {
            visual.localPosition = Vector2.zero;
            visual.localRotation = Quaternion.identity;
        }

        // ----- Belt drag: parent at tile origin, child handles rotation + offset
        if (machine != null && bm.heldMachine == bm.machineList[0] && bm.draggingBelt && bm.dragPath.Count >= 2)
        {
            var from = bm.dragPath[^2];
            var to   = bm.dragPath[^1];
            var dir  = bm.GetRelativeDirection(from, to);

            Vector2 tileOrigin = gridMap.GetTileOriginPosition(to.x, to.y);
            transform.position = Vector2.Lerp(transform.position, tileOrigin, Time.deltaTime * 15f);
            transform.rotation = Quaternion.identity;

            if (visual != null)
            {
                visual.localRotation = Quaternion.Euler(0f, 0f, machine.GetRotationAngle(dir));
                visual.localPosition = (Vector2)machine.GetRotationOffset(dir);
            }
            return;
        }

        // ----- Not dragging a belt: use your normal ghost behavior
        Vector2 targetPosition = bm.GetMouseWorldSnappedPosition();
        transform.position = Vector2.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
        transform.rotation = Quaternion.Lerp(transform.rotation, bm.GetMachineRotation(), Time.deltaTime * 15f);
    }
    private static Vector2 WorldToLocalOffset(Vector2 offW, MachineData.Direction dir)
    {
        // Undo the parent's rotation so the child offset is in parent-local space.
        return dir switch
        {
            MachineData.Direction.Up    => offW,                                  // 0°
            MachineData.Direction.Right => new Vector2( offW.y, -offW.x),         // -90°
            MachineData.Direction.Down  => new Vector2(-offW.x, -offW.y),         // 180°
            MachineData.Direction.Left  => new Vector2(-offW.y,  offW.x),         // +90°
            _ => offW
        };
    }
    private void SetLayerRecursive(GameObject targetObj, int layer)
    {
        targetObj.layer = layer;
        foreach (Transform child in targetObj.transform) SetLayerRecursive(child.gameObject, layer);
    }    

    public void ShowPath(List<Vector2Int> path)
    {
        if (path == null || path.Count < 2) { ClearPath(); return; }
        EnsurePool(path.Count);
        var beltData = BuildManager.Instance.machineList[0];

        for (int i = 0; i < path.Count; i++)
        {
            GameObject g = pool[i];
            g.SetActive(true);

            Vector2Int xy = path[i];
            bool hasPrev = i > 0;
            bool hasNext = i < path.Count - 1;

            // Match placement: face TOWARD next; last faces back along the last segment
            MachineData.Direction faceDir =
                hasNext ? BuildManager.Instance.GetRelativeDirection(path[i], path[i + 1]) :
                hasPrev ? BuildManager.Instance.GetRelativeDirection(path[i - 1], path[i]) :
                MachineData.Direction.Up;

            // Pick sprite from prev/next only (pure path logic)
            if (hasPrev && hasNext)
            {
                var prevDir = BuildManager.Instance.GetRelativeDirection(path[i - 1], path[i]);
                var nextDir = BuildManager.Instance.GetRelativeDirection(path[i], path[i + 1]);

                if (prevDir != nextDir)
                    ApplyVisual(g, IsLeftTurn(prevDir, nextDir) ? BeltVisualState.LeftTurn : BeltVisualState.RightTurn);
                else
                    ApplyVisual(g, BeltVisualState.Straight);
            }

            // Rotate like the placed belt…
            g.transform.rotation = Quaternion.Euler(0f, 0f, beltData.GetRotationAngle(faceDir));

            // …and position like the placed belt (tile origin + rotation offset)
            Vector2Int rotOffset = beltData.GetRotationOffset(faceDir);
            g.transform.position = gridMap.GetTileOriginPosition(xy.x, xy.y) + (Vector2)rotOffset;
        }

        // Hide leftovers
        for (int i = path.Count; i < pool.Count; i++) pool[i].SetActive(false);
    }

    public void ClearPath()
    {
        foreach (var g in pool) g.SetActive(false);
    }

    private void EnsurePool(int needed)
    {
        while (pool.Count < needed)
        {
            var g = Instantiate(beltGhostPrefab);
            g.layer = LayerMask.NameToLayer("Build Ghost");
            var sr = g.transform.GetChild(0).GetComponent<SpriteRenderer>();
            sr.color = GhostTint;
            pool.Add(g);
        }
    }

    private static Vector2Int DirToVec(MachineData.Direction d) => d switch
    {
        MachineData.Direction.Up    => Vector2Int.up,
        MachineData.Direction.Right => Vector2Int.right,
        MachineData.Direction.Down  => Vector2Int.down,
        MachineData.Direction.Left  => Vector2Int.left,
        _ => Vector2Int.zero
    };

    private static int DirToSteps(MachineData.Direction d) => d switch
    {
        MachineData.Direction.Up    => 0,
        MachineData.Direction.Right => 1,
        MachineData.Direction.Down  => 2,
        MachineData.Direction.Left  => 3,
        _ => 0
    };

    private static bool IsLeftTurn(MachineData.Direction inDir, MachineData.Direction outDir)
    {
        Vector2Int a = DirToVec(inDir);
        Vector2Int b = DirToVec(outDir);
        // 2D cross product z-sign: >0 means left turn
        int cross = a.x * b.y - a.y * b.x;
        return cross > 0;
    }

    private void ApplyVisual(GameObject ghost, BeltVisualState vis)
    {
        var sr = ghost.transform.GetChild(0).GetComponent<SpriteRenderer>();
        sr.sprite = visuals.GetSprite(vis) ?? visuals.straight; // fallback
    }
}



