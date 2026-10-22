using UnityEngine;

public class Machine_Producer : Machine
{
    public override string ToString() => "Producer";

    [Header("Grid")]
    private GridMap gridMap;
    private GridMap.GridTile outputTile;
    public Belt outputBelt;

    [Header("Selection")]
    public ItemData selectedRecipe;
    public ItemData activeRecipe;

    private bool itemPending = false;

    [Header("Visuals")]
    [SerializeField] private GameObject menuCanvas;
    
#region Main

    protected override void MachineSetup()
    {
        // Get grid first
        gridMap = GameManager.Instance.GetGrid();
        BuildManager.Instance.OnMachinePlaced += RefreshBelt;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        BuildManager.Instance.OnMachinePlaced -= RefreshBelt;
    }

    private void Update()
    {
        if (turnedOn) { CraftingCycle(); } // Crafting
    }
    
    public override void StartCrafting()
    {
        // Start Mining
        activeRecipe = selectedRecipe;
        tickTimer = 0;
        turnedOn = true;
    }

    public override void StopCrafting()
    {
        // Stop Mining
        activeRecipe = null;
        tickTimer = 0;
        turnedOn = false;
    }
    
    public override void SetSelectedRecipe(ItemData item)
    {
        // Discard any output still waiting for belt space from the previous recipe
        itemPending = false;
        selectedRecipe = item;
    }

#endregion

#region Get Output Belt

    private void RefreshBelt(Machine machine)
    {
        if (machine != this) return;
        if (gridMap == null) return;

        // Return the tile
        outputTile = gridMap.GetGridObject(
            GetDispenseTileIndexOffset().x, 
            GetDispenseTileIndexOffset().y);
        
        // Return the belt
        if (outputTile != null)
            outputBelt = outputTile.GetBelt() != null ? outputTile.GetBelt() : null;
    }

    public override Vector2Int GetDispenseTileIndexOffset()
    {
        // Get machine origin
        Vector2Int tileXY = originXY;
        tileXY = direction switch
        {
            MachineData.Direction.Up     =>   new Vector2Int(tileXY.x += 1, tileXY.y += 1),   
            MachineData.Direction.Right  =>   new Vector2Int(tileXY.x += 1, tileXY.y += 0),
            MachineData.Direction.Down   =>   new Vector2Int(tileXY.x += 0, tileXY.y += 0),  
            MachineData.Direction.Left   =>   new Vector2Int(tileXY.x += 0, tileXY.y += 1),
            _ => tileXY
            
        };

        return tileXY;       
    }
    
#endregion

#region Crafting

    private void CraftingCycle()
    {
        // Handle pending item
        if (itemPending)
        {
            if (outputBelt != null && outputBelt.RearSlotAvailable()) 
            { 
                itemPending = false;
                tickTimer = 0; 
                ItemManager.SpawnObject(outputBelt, activeRecipe);
            }
            return; // Don’t progress crafting until item is placed
        }

        // Normal crafting
        if (tickTimer >= GetCraftTicks(activeRecipe))
        {
            if (outputBelt != null && outputBelt.RearSlotAvailable())
            {
                tickTimer = 0; 
                ItemManager.SpawnObject(outputBelt, activeRecipe);
            }
            else
            {
                itemPending = true;
            }
        }
    }

#endregion

#region Open/Close Menu

    // Called from MouseManager
    public void OpenMenu()
    {
        menuCanvas.SetActive(true);
        GameManager.Instance.SetMenuOpen(true);
    }

    // Called from close button
    public void CloseMenu()
    {
        menuCanvas.SetActive(false);
        GameManager.Instance.SetMenuOpen(false);
    }

#endregion

}
