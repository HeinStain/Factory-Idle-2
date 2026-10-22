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
    
    private void Start()
    {
        TickManager.OnTick += HandleTick;
    }

    // Derived machines that override this must call base.OnDestroy()
    protected virtual void OnDestroy()
    {
        TickManager.OnTick -= HandleTick;
    }

    private void HandleTick(object sender, TickManager.OnTickEventArgs e) => tickTimer++;

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


#region Destruction

    public void DestroySelf() { ClearMachine(); Destroy(gameObject); }
    
    protected virtual void ClearMachine() {}

#endregion

#region Craft Time

    // Items already warned about, so each item only logs once per play session
    private static HashSet<ItemData> warnedNoCraftTime = new HashSet<ItemData>();

    // Clear on play start (covers Enter Play Mode with domain reload disabled)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCraftTimeWarnings()
    {
        warnedNoCraftTime.Clear();
    }

    // Ticks needed to craft a recipe. Uses the recipe's craftTicks and never returns less than 1.
    public int GetCraftTicks(ItemData recipe)
    {
        if (recipe == null) return 1;

        if (recipe.craftTicks > 0) return recipe.craftTicks;

        if (warnedNoCraftTime.Add(recipe))
        {
            Debug.LogWarning($"{recipe.displayName} ({recipe.name}): no craft time set on item ({machineData.machineName}), using 1 tick", recipe);
        }
        return 1;
    }

#endregion

    public virtual void StartCrafting() {}
    public virtual void StopCrafting() {}
    public virtual void SetSelectedRecipe(ItemData item) {}

}
