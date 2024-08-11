using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Machine : MonoBehaviour
{
    
    private MachineData machineData;
    [SerializeField] protected Vector2Int originXY;
    protected MachineData.Direction direction;

    protected int tickTimer;

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

        //

        //machineTransform.transform.Find("Visual/Sprite").GetComponent<SpriteRenderer>().sortingLayerName = "Placed Machine";

        //

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
}
