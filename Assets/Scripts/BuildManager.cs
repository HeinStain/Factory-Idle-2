using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    
    public static BuildManager Instance { get; private set; }

    public event Action OnSelectedChanged;
    public event Action<Machine> OnMachinePlaced;
    public event Action<bool> OnBuildModeChanged;

    private GridMap gridMap;
    
    [SerializeField] public List<MachineData> machineList;
    private MachineData heldMachine;
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
                RotateHeldMachine();
            }

            HandleMachineSelection();
        }
    }

#region Controls

    private void PlaceMachine()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        gridMap.GetXY(mousePosition, out int x, out int y);
        
        if (gridMap.GetGridObject(x,y) != null)
        {
            List<Vector2Int> gridPositionList = heldMachine.GetGridPositionList(new Vector2Int(x,y), direction);
        
            // Check if can build
            bool canBuild = true;
            foreach (Vector2Int position in gridPositionList)
            {
                if (gridMap.GetGridObject(position.x, position.y) == null)
                {
                    canBuild = false;
                    break;
                }
                else if (gridMap.GetGridObject(position.x, position.y).CanBuild() == false)
                {
                    canBuild = false;
                    break;
                }
            }

            //Build
            if (canBuild)
            {
                Vector2Int rotationOffset = heldMachine.GetRotationOffset(direction);
                
                Vector2 machineWorldPosition = gridMap.GetTileOriginPosition(x, y) + new Vector2(rotationOffset.x, rotationOffset.y);
                Machine machine = Machine.Create(machineWorldPosition, new Vector2Int(x, y), direction, heldMachine);

                if (heldMachine.hasDispenseBelt)
                {

                    Vector2Int dispensePos = machine.GetDispenseTileIndexOffset();
                    Vector2 beltWorldPosition = gridMap.GetTileOriginPosition(dispensePos.x, dispensePos.y) + new Vector2Int(rotationOffset.x, rotationOffset.y);
                     
                    Vector2Int poo =  direction switch
                    {
                        MachineData.Direction.Up    =>   new Vector2Int(dispensePos.x += 0, dispensePos.y += 0),
                        MachineData.Direction.Right =>   new Vector2Int(dispensePos.x += 0, dispensePos.y += 1),
                        MachineData.Direction.Down  =>   new Vector2Int(dispensePos.x += 1, dispensePos.y += 1), 
                        MachineData.Direction.Left  =>   new Vector2Int(dispensePos.x += 1, dispensePos.y += 0), 
                        _ => new Vector2Int(dispensePos.x += 0, dispensePos.y += 0)
                    };

                    Machine belt = Machine.Create(beltWorldPosition, new Vector2Int(poo.x, poo.y), direction, machineList[0]);

                    List<Machine> machines = new List<Machine> { machine };
                    belt.AssignAssociatedMachines(machines);

                    List<Machine> belts = new List<Machine> { belt };
                    machine.AssignAssociatedMachines(belts);

                    // Tell all tiles they're occupied
                    foreach(Vector2Int tileXY in gridPositionList)
                    {
                        if (tileXY == poo)
                        {
                            gridMap.GetGridObject(tileXY.x, tileXY.y).SetMachine(belt);
                            machine.DispenseUpdateCycle();
                            
                        }
                        else
                        {
                            gridMap.GetGridObject(tileXY.x, tileXY.y).SetMachine(machine);
                        }
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

                OnMachinePlaced?.Invoke(machine); 
                
            } 
            else
            {
                Debug.Log("Cannot build here!");
            }
        }
    }

   
    private void ClearMachine()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        GridMap.GridTile gridTile = gridMap.GetGridObject(mousePosition);
        Machine machine = gridTile.GetMachine();

        if (machine == null) return; // Exit if no machine is found

        // Destroy associated machines
        DestroyMachinesAtPositions(machine.associatedMachines);

        // Destroy the main machine
        DestroyMachinesAtPositions(new List<Machine> { machine });
    }


    private void DestroyMachinesAtPositions(List<Machine> machines)
    {
        foreach (Machine m in machines)
        {
            foreach (Vector2Int position in m.GetGridPositionList())
            {
                GridMap.GridTile tile = gridMap.GetGridObject(position.x, position.y);
                Machine tileMachine = tile.GetMachine();

                if (tileMachine != null)
                {
                    tileMachine.DestroySelf();
                    tile.ClearMachine();
                }
            }
        }
    }


    private void RotateHeldMachine()
    {
        direction = MachineData.GetNextRotationDirection(direction);
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


    #region Info
    // Used by GridBuildGhost.cs
    public Vector2 GetMouseWorldSnappedPosition() 
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        gridMap.GetXY(mousePosition, out int x, out int y);

        if(heldMachine != null)
        {
            Vector2Int rotationOffset = heldMachine.GetRotationOffset(direction);
            Vector2 machineWorldPosition = gridMap.GetTileOriginPosition(x, y) + new Vector2(rotationOffset.x, rotationOffset.y);
            return machineWorldPosition;
        }
        else
        {
            return mousePosition;
        }
    }

    // Used by GridBuildGhost.cs
    public Quaternion GetMachineRotation()
    {
        if (heldMachine != null)
        {
            return Quaternion.Euler(0, 0, heldMachine.GetRotationAngle(direction));
        } 
        else 
        {
            return Quaternion.identity;
        }
    }
    
    // Used by GridBuildGhost.cs
    public MachineData GetHeldMachine() 
    {
        return heldMachine;
    }

    // Used by Grabber.cs
    public GridMap.GridTile GetGridObject(Vector2 pos)
    {
        return gridMap.GetGridObject(pos);
    }
#endregion


    
}


