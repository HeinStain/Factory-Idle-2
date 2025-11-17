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
        BuildManager.Instance.OnMachinePlaced += RefreshBelt;
    }

    private void OnDestroy()
    {
        // Always unsubscribe to avoid memory leaks / null refs
        TickManager.OnTick -= HandleTick;
        BuildManager.Instance.OnMachinePlaced -= RefreshBelt;
    }


    private void HandleTick(object sender, TickManager.OnTickEventArgs e)
    {
        if (belt == null) return;

        var item = belt.PeekFrontItem();
        if (item == null) return;

        InventoryManager.Instance.AddItem(item);

        belt.RemoveFrontItem(despawn: true);
    }

#endregion


#region Input Belt

    public override Vector2Int GetIntakeTileIndexOffset() => originXY;       
    
#endregion


#region Get Belt


    private void RefreshBelt(Machine machine)
    {
        if (machine != this) return;
        if (gridMap == null) return;

        tile = gridMap.GetGridObject(originXY.x, originXY.y);

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
