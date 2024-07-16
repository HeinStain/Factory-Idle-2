using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    
    public static BuildManager Instance { get; private set; }

    public event EventHandler OnSelectedChanged;
    public event EventHandler OnMachinePlaced;
    public event Action<bool> OnBuildModeChanged;

    private Grid<GridMap.GridTile> gridArray;
    
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

    public void SetGrid(Grid<GridMap.GridTile> gridArray)
    {
        this.gridArray = gridArray;
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
        gridArray.GetXY(mousePosition, out int x, out int y);
        
        if (gridArray.ValidateGridPosition(new Vector2Int(x,y)))
        {
            List<Vector2Int> gridPositionList = heldMachine.GetGridPositionList(new Vector2Int(x,y), direction);
        
            // Check if can build
            bool canBuild = true;
            foreach (Vector2Int position in gridPositionList)
            {
                if (gridArray.ValidateGridPosition(position) == false)
                {
                    canBuild = false;
                    break;
                }
                else if (gridArray.GetGridTile(position.x, position.y).CanBuild() == false)
                {
                    canBuild = false;
                    break;
                }
            }

            //Build
            if (canBuild)
            {
                Vector2Int rotationOffset = heldMachine.GetRotationOffset(direction);
                Vector2 machineWorldPosition = gridArray.GetTileOriginPosition(x, y) + new Vector2(rotationOffset.x, rotationOffset.y);

                Machine machine = Machine.Create(machineWorldPosition, new Vector2Int(x, y), direction, heldMachine);

                // Tell all tiles they're occupied
                foreach(Vector2Int position in gridPositionList)
                {
                    gridArray.GetGridTile(position.x, position.y).SetMachine(machine);
                }
                
                OnMachinePlaced?.Invoke(this, EventArgs.Empty); 
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
        GridMap.GridTile gridTile = gridArray.GetGridTile(mousePosition);
        Machine machine = gridTile.GetMachine();
        if (machine != null)
        {
            machine.DestroySelf();

            List<Vector2Int> gridPositionList = machine.GetGridPositionList();

            foreach(Vector2Int position in gridPositionList)
            {
                gridArray.GetGridTile(position.x, position.y).ClearMachine();
            }
        }
    }

    private void RotateHeldMachine()
    {
        direction = MachineData.GetNextRotationDirection(direction);
    }

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
        OnSelectedChanged?.Invoke(this, EventArgs.Empty);
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

        gridArray.GetXY(mousePosition, out int x, out int y);

        if(heldMachine != null)
        {
            Vector2Int rotationOffset = heldMachine.GetRotationOffset(direction);
            Vector2 machineWorldPosition = gridArray.GetTileOriginPosition(x, y) + new Vector2(rotationOffset.x, rotationOffset.y);
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
    public GridMap.GridTile GetGridTile(Vector2 pos)
    {
        return gridArray.GetGridTile(pos);
    }
    #endregion


    
}


