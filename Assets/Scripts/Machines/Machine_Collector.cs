using UnityEngine;

public class Machine_Collector : Machine
{
    
    public override string ToString() => "Collector";

    [Header("Grid")]
    private GridMap gridMap;
    private GridMap.GridTile tile;
    public Belt belt;


#region Main


    protected override void MachineSetup()
    {
        gridMap = GameManager.Instance.GetGrid();
        TickManager.OnTick += HandleTick;
    }
    
    private void OnDestroy()
    {
        // Always unsubscribe to avoid memory leaks / null refs
        TickManager.OnTick -= HandleTick;
    }


    private void HandleTick(object sender, TickManager.OnTickEventArgs e)
    {
        if (belt == null) return;

        var item = belt.PeekFrontItem();
        if (item == null) return;

        // 1) Add to your inventory (replace with your actual inventory call)
        // InventoryManager.Instance.AddItem(item);

        // 2) Remove + despawn from the world (pooled)
        belt.RemoveFrontItem(despawn: true);
    }



#endregion






#region Get Belt

    protected override void GetIntakeTile()  
    {
        tile = gridMap.GetGridObject(originXY.x, originXY.y);
    }

    protected override void GetIntakeBelt()
    {
        // Check for Output Tile, Open state, and machine type of Belt
        if (tile == null || 
            tile.GetTileVisualState() != GridMap.GridTile.TileVisualState.Open || 
            tile.GetBelt() == null)
        { belt = null; return; }

        // Get Belt
        belt = tile.GetBelt();
    }

#endregion




}
