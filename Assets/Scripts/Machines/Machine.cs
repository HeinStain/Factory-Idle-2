using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Machine : MonoBehaviour
{
    
    protected MachineData machineData;
    [SerializeField] protected Vector2Int originXY;
    public MachineData.Direction direction;
    private List<Machine> associatedMachines;
    public int tickTimer;
    public bool functioning = false;

    [SerializeField] protected ItemTag skillTag;
    public ItemTag SkillTag => skillTag;  // Read-only property


    private void Start()
    {
        TickManager.OnTick += delegate (object sender, TickManager.OnTickEventArgs e) 
        { 
            tickTimer++;
        };
    }

    public static Machine Create(Vector2 worldPos, Vector2Int originXY, MachineData.Direction direction, MachineData machineData)
    {
        Transform machineTransform = Instantiate(machineData.prefab, worldPos, Quaternion.Euler(0,0, machineData.GetRotationAngle(direction)));
        Machine machine = machineTransform.GetComponent<Machine>();

        Transform visuals = machineTransform.Find("Visual");
        if (visuals != null)
        {
            SpriteRenderer[] spriteRenderers = visuals.GetComponentsInChildren<SpriteRenderer>();

            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                spriteRenderer.sortingLayerName = "Placed Machine";
            }
        }
        machine.machineData = machineData;
        machine.originXY = originXY;
        machine.direction = direction;
        machine.MachineSetup();
        
        return machine;
    }

    protected virtual void MachineSetup() { }

    public List<Vector2Int> GetGridPositionList()
    {
        return machineData.GetGridPositionList(originXY, direction);
    }

    public string GetMachineName()
    {
        return machineData.machineName;
    }

    public MachineData GetMachineData()
    {
        return machineData;
    }


    public Vector2Int GetMachineOrigin()
    {
        return originXY;
    }

    public void DestroySelf()
    {
        ClearMachine();
        Destroy(gameObject);
    }

    protected virtual void ClearMachine() {}

    public void DispenseUpdateCycle()
    {
        //Get output belt
        GetDispenseTile();
        GetDispenseBelt();
    }

    protected virtual void GetDispenseTile() {}
    
    protected virtual void GetDispenseBelt() {}

    public virtual Vector2Int GetDispenseTileIndexOffset() { return new Vector2Int(0,0); }
    
    public void AssignAssociatedMachines(List<Machine> machines)
    {   
        associatedMachines = machines;
    }

    public List<Machine> GetAssociatedMachines()
    {
        return associatedMachines;
    }

}
