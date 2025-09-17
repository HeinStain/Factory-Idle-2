using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    
    public static BuildManager Instance { get; private set; }

    public event Action OnSelectedChanged;
    public event Action<Machine> OnMachinePlaced;
    public event Action<bool> OnBuildModeChanged;

    private GridMap gridMap;
    
    [SerializeField] public List<MachineData> machineList;
    public MachineData heldMachine;
    private MachineData.Direction direction = MachineData.Direction.Up;

    private bool buildMode;


    private void Awake()
    {
        Instance = this;

        buildMode = false;

        heldMachine = machineList[0];
    }

    private void Start()
    {
        gridMap = GameManager.Instance.GetGrid();
    }

    private void Update()
    {
        if (buildMode == true)
        {
            // Holding a belt
            if (heldMachine == machineList[0])
            {
                HandleBeltDrag();
                // Prevent other functionality while dragging
                if (draggingBelt) return;
            }

            if (Input.GetMouseButtonDown(0) && heldMachine != null) 
            {
                PlaceMachine();
            }

            if (Input.GetMouseButtonDown(1))
            {
                
                ClearMachine();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                direction = MachineData.GetNextRotationDirection(direction);
            }

            HandleMachineSelection();
        }
    }

#region Machine Placement

    private void PlaceMachine()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        gridMap.GetXY(mousePosition, out int x, out int y);

        if (gridMap.GetGridObject(x,y) != null)
        {
            List<Vector2Int> gridPositionList = heldMachine.GetGridPositionList(new Vector2Int(x,y), direction);
            
            if (CanBuild(gridPositionList)) BuildMachine(gridPositionList, x, y);
            else NotificationManager.Instance.DisplayNotification("Cannot build here!");
        }
    }

    private bool CanBuild(List<Vector2Int> gridPositionList)
    {
        int clearedCount = 0;

        foreach (Vector2Int position in gridPositionList)
        {
            GridMap.GridTile tile = gridMap.GetGridObject(position.x, position.y);

            if (tile != null && tile.CanBuild() == true) clearedCount++;
        }

        if (clearedCount == gridPositionList.Count) return true;
        else return false;
    }   

    private void BuildMachine(List<Vector2Int> gridPositionList, int x, int y)
    {
        // Get Machine positioning
        Vector2Int rotationOffset = heldMachine.GetRotationOffset(direction);
        Vector2 machineWorldPosition = gridMap.GetTileOriginPosition(x, y) + new Vector2(rotationOffset.x, rotationOffset.y);

        // Create Machine
        Machine machine = Machine.Create(machineWorldPosition, new Vector2Int(x, y), direction, heldMachine);

        if (heldMachine.hasDispenseBelt)
        {
            // Get Belt positioning
            Vector2Int dispensePos = machine.GetDispenseTileIndexOffset();
            Vector2Int beltRotationOffset = machineList[0].GetRotationOffset(direction);
            Vector2 beltWorldPosition = gridMap.GetTileOriginPosition(dispensePos.x, dispensePos.y) + new Vector2Int(beltRotationOffset.x, beltRotationOffset.y);

            // Create Belt
            Machine belt = Machine.Create(beltWorldPosition, dispensePos, direction, machineList[0]);

            // Assign associations
            List<Machine> machines = new List<Machine> { machine };
            belt.AssignAssociatedMachines(machines);
            List<Machine> belts = new List<Machine> { belt };
            machine.AssignAssociatedMachines(belts);

            // Tell all tiles they're occupied
            foreach(Vector2Int position in gridPositionList)
            {
                if (position == dispensePos)
                {
                    gridMap.GetGridObject(position.x, position.y).SetMachine(belt);
                }
                gridMap.GetGridObject(position.x, position.y).SetMachine(machine);
            }
        }
        else
        {
            // Tell all tiles they're occupied
            foreach(Vector2Int position in gridPositionList)
            {
                gridMap.GetGridObject(position.x, position.y).SetMachine(machine);
            }
        }

        machine.BeltCheckUpdateCycle();
        OnMachinePlaced?.Invoke(machine); 
    }


    private void BuildMachine(List<Vector2Int> gridPositionList, int x, int y, MachineData.Direction customDirection)
    {
        Vector2Int rotationOffset = heldMachine.GetRotationOffset(customDirection);
        Vector2 machineWorldPosition = gridMap.GetTileOriginPosition(x, y) + (Vector2)rotationOffset;

        Machine machine = Machine.Create(machineWorldPosition, new Vector2Int(x, y), customDirection, heldMachine);

        foreach (Vector2Int position in gridPositionList)
            gridMap.GetGridObject(position.x, position.y).SetMachine(machine);

        machine.BeltCheckUpdateCycle();
        OnMachinePlaced?.Invoke(machine);
        
    }

#endregion

#region Machine Deletion

    private void ClearMachine()
    {
        //CHEKK TILE IS VALIS 

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        GridMap.GridTile gridTile = gridMap.GetGridObject(mousePosition);


        Machine machine = gridTile.GetMachine();
        Belt belt = gridTile.GetBelt();
                
        if (belt != null)
        {
            DestroyMachinesAtPositions(new List<Machine> { belt });
        }
        
        if (machine != null)
        {
            // Destroy associated machines
            DestroyMachinesAtPositions(machine.GetAssociatedMachines());
            // Destroy the main machine
            DestroyMachinesAtPositions(new List<Machine> { machine });
        }

    }


    private void DestroyMachinesAtPositions(List<Machine> machines)
    {
        foreach (Machine m in machines)
        {
            foreach (Vector2Int position in m.GetGridPositionList())
            {
                GridMap.GridTile tile = gridMap.GetGridObject(position.x, position.y);
                if (tile != null)
                {
                    tile.GetMachine()?.DestroySelf();
                    tile.GetBelt()?.DestroySelf();
                    tile.ClearMachine();
                }
            }
        }
    }

#endregion

#region Machine Selection
    private void HandleMachineSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { SelectMachineType(0); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { SelectMachineType(1); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { SelectMachineType(2); }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { SelectMachineType(3); }
        if (Input.GetKeyDown(KeyCode.Alpha5)) { SelectMachineType(4); }
        if (Input.GetKeyDown(KeyCode.Alpha6)) { SelectMachineType(5); }
        if (Input.GetKeyDown(KeyCode.Alpha7)) { SelectMachineType(6); }
        if (Input.GetKeyDown(KeyCode.Alpha8)) { SelectMachineType(7); }
        if (Input.GetKeyDown(KeyCode.Alpha0)) { DeselectAllMachineTypes(); }
    }
#endregion

#region Functions
    public void BuildMode(bool b)
    {
        buildMode = b;
        OnBuildModeChanged?.Invoke(buildMode);
    }

    private void RefreshSelectedMachine()
    {
        OnSelectedChanged?.Invoke();
    }

    public void SelectMachineType(int index)
    {
        heldMachine = machineList[index]; 
        RefreshSelectedMachine();
    }

    private void DeselectAllMachineTypes()
    {
        heldMachine = null;
        RefreshSelectedMachine();
    }
#endregion


#region Used by GridBuildGhost.cs

    public Vector2 GetMouseWorldSnappedPosition() 
    {
        Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        gridMap.GetXY(mouse, out int x, out int y);

        if (heldMachine == null) return mouse;

        // While dragging belts: keep parent at tile origin (no rotation offset).
        if (heldMachine == machineList[0] && draggingBelt)
            return gridMap.GetTileOriginPosition(x, y);

        // Normal case (non-dragging): include rotation offset from the current facing.
        var dirForOffset = direction;
        Vector2Int rotOffset = heldMachine.GetRotationOffset(dirForOffset);
        return gridMap.GetTileOriginPosition(x, y) + (Vector2)rotOffset;
    }
    public Quaternion GetMachineRotation() => (heldMachine != null) ? Quaternion.Euler(0, 0, heldMachine.GetRotationAngle(direction)) : Quaternion.identity;


         // Smart‑belt drag state
    public bool draggingBelt = false;
    public readonly List<Vector2Int> dragPath = new();

    public MachineData.Direction GetCurrentDirection() => direction;    
    private bool pendingBeltDrag = false;
    private Vector2Int dragStart;

    private void HandleBeltDrag()
    {
        // Tile under mouse
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        gridMap.GetXY(mousePos, out int mouseX, out int mouseY);
        Vector2Int tileXY = new(mouseX, mouseY);

        // ---- LMB down: arm a potential drag (no ghosts yet)
        if (!pendingBeltDrag && !draggingBelt && Input.GetMouseButtonDown(0) && Walkable(tileXY))
        {
            pendingBeltDrag = true;
            dragStart = tileXY;
            return; // skip one-click build this frame
        }

        // ---- While holding: transition pending -> real drag on first step
        if (pendingBeltDrag && Input.GetMouseButton(0))
        {
            if (tileXY != dragStart)
            {
                int steps = Mathf.Abs(tileXY.x - dragStart.x) + Mathf.Abs(tileXY.y - dragStart.y);
                if (steps == 1 && Walkable(tileXY))
                {
                    pendingBeltDrag = false;
                    draggingBelt = true;
                    dragPath.Clear();
                    dragPath.Add(dragStart);
                    dragPath.Add(tileXY);
                    GridBuildGhost.Instance.ShowPath(dragPath); // now show ghosts (path size 2)
                }
            }
        }

        // ---- Grow / shrink an active drag
        if (draggingBelt && Input.GetMouseButton(0))
        {
            Vector2Int last = dragPath[^1];
            if (tileXY != last)
            {
                int steps = Mathf.Abs(tileXY.x - last.x) + Mathf.Abs(tileXY.y - last.y);
                if (steps == 1)
                {
                    if (dragPath.Count > 1 && tileXY == dragPath[^2]) dragPath.RemoveAt(dragPath.Count - 1);
                    else if (Walkable(tileXY)) dragPath.Add(tileXY);

                    GridBuildGhost.Instance.ShowPath(dragPath);
                }
            }
        }

        // ---- RMB: cancel
        if ((pendingBeltDrag || draggingBelt) && Input.GetMouseButtonDown(1))
        {
            CancelDrag();
            return;
        }

        // ---- LMB up: finalize
        if (Input.GetMouseButtonUp(0))
        {
            if (draggingBelt)
            {
                int c = dragPath.Count;
                if (c >= 2)
                {
                    for (int i = 0; i < c - 1; i++)
                    {
                        var here = dragPath[i];
                        var next = dragPath[i + 1];
                        BuildOrReplaceBelt(here, GetRelativeDirection(here, next));
                    }
                    BuildOrReplaceBelt(dragPath[c - 1], GetRelativeDirection(dragPath[c - 2], dragPath[c - 1]));
                }
                CancelDrag();
            }
            else if (pendingBeltDrag)
            {
                // Single click: place one belt facing current manual direction
                if (Walkable(dragStart)) BuildOrReplaceBelt(dragStart, direction);
                CancelDrag();
            }
        }
    }

    private void CancelDrag()
    {
        pendingBeltDrag = false;
        draggingBelt = false;
        dragPath.Clear();
        GridBuildGhost.Instance.ClearPath();
    }

    public MachineData.Direction GetRelativeDirection(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;
        if (delta == Vector2Int.up) return MachineData.Direction.Up;
        if (delta == Vector2Int.right) return MachineData.Direction.Right;
        if (delta == Vector2Int.down) return MachineData.Direction.Down;
        if (delta == Vector2Int.left) return MachineData.Direction.Left;
        return direction; // fallback
    }



    private void BuildOrReplaceBelt(Vector2Int xy, MachineData.Direction dir)
    {
        Vector2Int origin = xy; // No offset needed
        List<Vector2Int> gridPositionList = new() { origin };

        // Remove any existing belt at this tile
        gridMap.GetGridObject(origin.x, origin.y)?.GetBelt()?.DestroySelf();

        // Place belt exactly where ghost was
        BuildMachine(gridPositionList, origin.x, origin.y, dir);
    }

    private bool Walkable(Vector2Int tile) => gridMap.GetGridObject(tile.x, tile.y).IsBlockedForBelt(allowOverwriteBelts: true) == false;



#endregion
    
}


  