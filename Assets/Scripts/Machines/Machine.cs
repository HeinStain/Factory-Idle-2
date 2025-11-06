using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Machine : MonoBehaviour
{
    
    protected MachineData machineData;
    [SerializeField] protected Vector2Int originXY;
    public MachineData.Direction direction;
    [SerializeField] private List<Machine> associatedMachines;
    public int tickTimer;
    public bool turnedOn = false;

    [SerializeField] protected ItemTag skillTag;
    public ItemTag SkillTag => skillTag;  // Read-only property


    private void Start()
    {
        TickManager.OnTick += delegate (object sender, TickManager.OnTickEventArgs e) 
        { 
            tickTimer++;
        };
    }

    public static Machine Create(Vector2 worldPos, Vector2Int originXY, MachineData.Direction direction, MachineData machineData, int tag)
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
        machine.AssignTag(tag);
        machine.MachineSetup();
        
        return machine;
    }

    protected virtual void MachineSetup() { }

#region Machine Data

    public Vector2Int GetMachineOrigin() => originXY;
    public MachineData GetMachineData() => machineData;
    
    public List<Vector2Int> GetGridPositionList() => machineData.GetGridPositionList(originXY, direction);

#endregion

#region Associated Machines    

    public void AssignAssociatedMachines(List<Machine> machines) => associatedMachines = machines;
    public List<Machine> GetAssociatedMachines() => associatedMachines;
    public virtual Vector2Int GetDispenseTileIndexOffset() { return new Vector2Int(0,0); }
    public virtual Vector2Int GetIntakeTileIndexOffset() { return new Vector2Int(0,0); }
    public virtual void AssignTag(int tag) {}

#endregion

#region UpdateCycle

    public void BeltCheckUpdateCycle() { GetDispenseTile(); GetDispenseBelt(); GetIntakeTile(); GetIntakeBelt(); }

    protected virtual void GetDispenseTile() {}
    protected virtual void GetDispenseBelt() {}
    protected virtual void GetIntakeTile() {}
    protected virtual void GetIntakeBelt() {}

#endregion

#region Destruction

    public void DestroySelf() { ClearMachine(); Destroy(gameObject); }
    
    protected virtual void ClearMachine() {}

#endregion


}
