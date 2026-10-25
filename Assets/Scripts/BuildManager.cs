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
    public event Action OnQuickslotsChanged;

    private GridMap gridMap;

    // Every buildable machine. Order matters: Belt is detected as machineList[0].
    [SerializeField] public List<MachineData> machineList;
    public MachineData heldMachine;
    private MachineData.Direction direction = MachineData.Direction.Up;

    // Machines on number keys 1 to QuickslotCount. Empty slots are allowed.
    public const int QuickslotCount = 9;
    [SerializeField] private MachineData[] quickslots = new MachineData[QuickslotCount];
    public int SelectedQuickslot { get; private set; } = -1;

    private bool buildMode;


    private void Awake()
    {
        Instance = this;

        buildMode = false;

        if (quickslots == null || quickslots.Length != QuickslotCount) Array.Resize(ref quickslots, QuickslotCount);

        SelectMachine(machineList[0]);
    }

    private void OnValidate()
    {
        // Keep exactly QuickslotCount slots in the Inspector
        if (quickslots == null || quickslots.Length != QuickslotCount) Array.Resize(ref quickslots, QuickslotCount);
    }

    private void Start()
    {
        gridMap = GameManager.Instance.GetGrid();
    }

    private void Update()
    {
        if (buildMode == true)
        {
            // Ignore all build input while the machine menu is open
            if (GameManager.Instance.IsMachineMenuOpen)
            {
                if (pendingBeltDrag || draggingBelt) CancelDrag();
                return;
            }

            // Don't place or delete through UI
            bool pointerOverUI = GameManager.IsPointerOverUI();

            // Holding a belt
            if (heldMachine == machineList[0])
            {
                HandleBeltDrag();
                // Prevent other functionality while dragging
                if (draggingBelt) return;
            }

            if (Input.GetMouseButtonDown(0) && heldMachine != null && !pointerOverUI)
            {
                PlaceMachine();
            }

            if (Input.GetMouseButtonDown(1) && !pointerOverUI)
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
        Machine machine = Machine.Create(machineWorldPosition, new Vector2Int(x, y), direction, heldMachine, 99);
        List<Machine> machines = new List<Machine> { machine };
        List<Machine> belts = new List<Machine> {};

        // Handle extra belts
        if (heldMachine.hasDispenseBelt || heldMachine.hasIntakeBelt)
        {
            if (heldMachine.hasDispenseBelt)
            {
                // Get Dispense Belt positioning
                Vector2Int dispenseTileOffset = machine.GetDispenseTileIndexOffset();
                Vector2Int dispenseBeltRotationOffset = machineList[0].GetRotationOffset(direction);
                Vector2 dispenseBeltWorldPosition = gridMap.GetTileOriginPosition(dispenseTileOffset.x, dispenseTileOffset.y) 
                                                  + new Vector2Int(dispenseBeltRotationOffset.x, dispenseBeltRotationOffset.y);
                // Create Belt
                Machine dispenseBelt = Machine.Create(
                       worldPos: dispenseBeltWorldPosition, 
                       originXY: dispenseTileOffset, 
                      direction: direction, 
                    machineData: machineList[0],
                            tag: 1);

                // Associate Machine to Belt
                dispenseBelt.AssignAssociatedMachines(machines);
                // Add Belt to list of Belts
                belts.Add(dispenseBelt);
                // Occupy Tiles
                gridMap.GetGridObject(dispenseTileOffset.x, dispenseTileOffset.y).SetMachine(dispenseBelt);
            }

            if (heldMachine.hasIntakeBelt)
            {
                // Get Intake Belt positioning
                Vector2Int intakeTileOffset = machine.GetIntakeTileIndexOffset();
                Vector2Int intakeBeltRotationOffset = machineList[0].GetRotationOffset(direction);
                Vector2 intakeBeltWorldPosition = gridMap.GetTileOriginPosition(intakeTileOffset.x, intakeTileOffset.y) 
                                                + new Vector2Int(intakeBeltRotationOffset.x, intakeBeltRotationOffset.y);
                // Create Belt
                Machine intakeBelt = Machine.Create(
                       worldPos: intakeBeltWorldPosition, 
                       originXY: intakeTileOffset, 
                      direction: direction, 
                    machineData: machineList[0],
                            tag: 0);

                // Associate Machine to Belt
                intakeBelt.AssignAssociatedMachines(machines);
                // Add Belt to list of Belts
                belts.Add(intakeBelt);
                // Occupy Tiles
                gridMap.GetGridObject(intakeTileOffset.x, intakeTileOffset.y).SetMachine(intakeBelt);
            }

            // Associate Belts
            machine.AssignAssociatedMachines(belts);
        }

        // Tell all tiles they're occupied
        foreach(Vector2Int position in gridPositionList)
        {
            gridMap.GetGridObject(position.x, position.y).SetMachine(machine);
        
        }

        OnMachinePlaced?.Invoke(machine); 
    }


    private void ReplaceBelt(List<Vector2Int> gridPositionList, int x, int y, MachineData.Direction customDirection)
    {
        Vector2Int rotationOffset = heldMachine.GetRotationOffset(customDirection);
        Vector2 machineWorldPosition = gridMap.GetTileOriginPosition(x, y) + (Vector2)rotationOffset;

        Machine belt = Machine.Create(machineWorldPosition, new Vector2Int(x, y), customDirection, heldMachine, 99);

        foreach (Vector2Int position in gridPositionList)
            gridMap.GetGridObject(position.x, position.y).SetMachine(belt);

        OnMachinePlaced?.Invoke(belt);
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
        // Number keys from 1 pick quickslots from 0
        for (int i = 0; i < QuickslotCount; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { SelectQuickslot(i); }
        }
        if (Input.GetKeyDown(KeyCode.Alpha0)) { DeselectAllMachineTypes(); }
    }
#endregion

#region Quickslots

    public MachineData GetQuickslot(int index) => (index >= 0 && index < QuickslotCount) ? quickslots[index] : null;

    // Holds the machine in this quickslot. An empty or invalid slot does nothing.
    public void SelectQuickslot(int index)
    {
        MachineData machine = GetQuickslot(index);
        if (machine == null) return;

        heldMachine = machine;
        SelectedQuickslot = index;
        RefreshSelectedMachine();
    }

    // Holds any machine, highlighting its quickslot if it has one
    public void SelectMachine(MachineData machine)
    {
        heldMachine = machine;
        SelectedQuickslot = machine != null ? Array.IndexOf(quickslots, machine) : -1;
        RefreshSelectedMachine();
    }

    // Moves a machine into a quickslot, replacing whatever was there
    public void AssignQuickslot(int index, MachineData machine)
    {
        if (index < 0 || index >= QuickslotCount) return;

        // A machine sits in at most one slot, so clear the slot it is leaving
        if (machine != null)
        {
            for (int i = 0; i < QuickslotCount; i++)
            {
                if (quickslots[i] == machine) quickslots[i] = null;
            }
        }

        quickslots[index] = machine;
        SelectedQuickslot = heldMachine != null ? Array.IndexOf(quickslots, heldMachine) : -1;
        OnQuickslotsChanged?.Invoke();
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

    // Old UI: selects by machineList index. Still called by the scene's Machine Buttons until they're removed.
    public void SelectMachineType(int index)
    {
        if (index < 0 || index >= machineList.Count) return;
        SelectMachine(machineList[index]);
    }

    private void DeselectAllMachineTypes()
    {
        SelectMachine(null);
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
        if (!pendingBeltDrag && !draggingBelt && Input.GetMouseButtonDown(0) && !GameManager.IsPointerOverUI() && Walkable(tileXY))
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
        ReplaceBelt(gridPositionList, origin.x, origin.y, dir);
    }

    private bool Walkable(Vector2Int tile) => gridMap.GetGridObject(tile.x, tile.y).IsBlockedForBelt(allowOverwriteBelts: true) == false;



#endregion
    
}


  