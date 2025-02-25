using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class Machine_Producer : Machine
{


    [Header("Grid")]
    private Grid<GridMap.GridTile> gridArray;
    private GridMap.GridTile outputTile;
    public Belt outputBelt;


    [Header("Selection")]
    public ItemData selectedRecipe;
    public ItemData itemToCraft;


    [Header("Data")]
    public bool respawningReservoir;
    public int timeToReservoirRespawn = 100;
    public Dictionary<string, int> reservoirNodeCounts = new Dictionary<string, int>();
    public Dictionary<string, int> reservoirMaxes = new Dictionary<string, int>();
    

    [Header("Visuals")]
    [SerializeField] private GameObject menuCanvas;


#region Main


    protected override void MachineSetup()
    {
        // Get grid first
        gridArray = GameManager.Instance.GetGrid();

        // Set skill tag from machine data
        skillTag = (ItemTag)System.Enum.Parse(typeof(ItemTag), machineData.skillName);
        //Debug.Log($"Setting up {machineData.machineName} with skillTag: {skillTag}");

        // Now create dictionaries with verified tag
        foreach (ItemData item in ItemManager.Instance.itemList)
        {
            if (item.tags.Contains(skillTag))
            {
                //Debug.Log($"Adding {item.displayName} to {machineData.machineName} with tag {skillTag}");
                reservoirMaxes.Add(item.displayName, 
                    SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.itemMasteryLevels, item.displayName) + 5);
                reservoirNodeCounts.Add(item.displayName, 
                    SkillManager.Instance.GetDictionaryValue(reservoirMaxes, item.displayName));
            }
        }

        DispenseUpdateCycle();
    }




    private void Update()
    {
        if (functioning)   { CraftingCycle();     } // Crafting
    }

#endregion


#region Get Output Belt

    protected override void GetDispenseTile()  
    {
        // Get machine origin
        Vector2Int tileXY = originXY;
        
        // Calculate tile Position
        switch (direction)
        {
            case MachineData.Direction.Up:     tileXY.x += 1; tileXY.y += 1;  break;
            case MachineData.Direction.Right:  tileXY.x += 1; tileXY.y += 0;  break;
            case MachineData.Direction.Down:   tileXY.x += 0; tileXY.y += 0;  break;
            case MachineData.Direction.Left:   tileXY.x += 0; tileXY.y += 1;  break;
        }

        // Return the tile
        outputTile = gridArray.ValidateGridPosition(tileXY) ? gridArray.GetGridTile(tileXY.x, tileXY.y) : null;
    }

    protected override void GetDispenseBelt()
    {
        // Check for Output Tile, Open state, and machine type of Belt
        if (outputTile == null || 
            outputTile.GetTileState() != GridMap.GridTile.TileState.Open || 
            outputTile.GetMachine()?.GetMachineName() != "Belt")
        {
            outputBelt = null; return;
        }

        // Get Belt
        Belt belt = outputTile.GetMachine() as Belt;
        outputBelt = belt;
    }

    public override Vector2Int GetDispenseTileIndexOffset()
    {
        // Get machine origin
        Vector2Int tileXY = originXY;
        tileXY = direction switch
        {
            MachineData.Direction.Up     =>   new Vector2Int(tileXY.x += 1, tileXY.y += 1),   
            MachineData.Direction.Right  =>   new Vector2Int(tileXY.x += 1, tileXY.y -= 1),
            MachineData.Direction.Down   =>   new Vector2Int(tileXY.x -= 1, tileXY.y -= 1),  
            MachineData.Direction.Left   =>   new Vector2Int(tileXY.x -= 1, tileXY.y += 1),
            _ => tileXY
            
        };

        return tileXY;       
    }
    
#endregion


#region Crafting

    private void CraftingCycle()
    {

        // Craft
        if (tickTimer >= machineData.defaultCraftingInterval && !respawningReservoir)
        {
            // Timer
            tickTimer = 0;

            // Get output belt path
            List<Belt> outputBeltPath = BeltManager.Instance.GetBeltPath(outputBelt);
            // Get rotation offset
            Vector2 offset = BeltManager.Instance.GetBeltCentreOffset(outputBeltPath[0]);
            // Set output position
            Vector3 outputPos = new Vector3(outputBeltPath[0].transform.position.x + offset.x, outputBeltPath[0].transform.position.y + offset.y, outputBelt.transform.position.z);
            
            // Spawn object                                                     <   POS   >
            ItemManager.SpawnObject(outputBelt, ItemManager.Instance.itemPrefab, itemToCraft, outputPos, Quaternion.identity);
            
            // Subtract from Reservoir Count
            SkillManager.Instance.ModifyDictionaryValue(reservoirNodeCounts, itemToCraft.displayName, -1, true);

            // Distribute XP
            HandleXPDistribution();

            // Check for last Reservoir Node
            int currentNodeCount = SkillManager.Instance.GetDictionaryValue(reservoirNodeCounts, itemToCraft.displayName);
            if (currentNodeCount < 1) { respawningReservoir = true; }   
        }
        
        // Respawn Reservoir
        if (respawningReservoir)
        {
            if (tickTimer >= timeToReservoirRespawn)
            {
                // Timer
                tickTimer = 0;
                // Set Reservoir to max
                int reservoirMax = SkillManager.Instance.GetDictionaryValue(reservoirMaxes, itemToCraft.displayName);
                SkillManager.Instance.ModifyDictionaryValue(reservoirNodeCounts, itemToCraft.displayName, reservoirMax, false);
                // Reset
                respawningReservoir = false;
            }
        }
    }

#endregion


#region XP Distribution
    private void HandleXPDistribution()
    {

        string skillName = machineData.skillName;
        string itemName = itemToCraft.displayName;

        // Add Skill XP
        SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.skillXP, skillName, itemToCraft.XP, true);
        // Add Mastery and Mastery Pool XP
        float givenMasteryXP = SkillManager.Instance.Calculate_GivenItemMasteryXP(itemName, skillTag);
        SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.skillMasteryPoolXP, skillName, givenMasteryXP/4, true);
        SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.itemMasteryXPCount, itemName, givenMasteryXP, true);
 
        //
        //
        // Handle Item Mastery Level Up
        int itemCurrentMasteryLevel = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.itemMasteryLevels, itemName);
        float itemCurrentMasteryXP = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.itemMasteryXPCount, itemName);
        float masteryLevelXPReq = SkillManager.Instance.Calculate_XPToNextLevel(itemCurrentMasteryLevel);

        if (itemCurrentMasteryXP >= masteryLevelXPReq)
        {
            // Level up and update XP
            float leftOverMasteryXP = itemCurrentMasteryXP - masteryLevelXPReq;
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.itemMasteryLevels, itemName, itemCurrentMasteryLevel + 1, false);
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.itemMasteryXPCount, itemName, leftOverMasteryXP, false);

            // Update max vein and notify
            SkillManager.Instance.ModifyDictionaryValue(reservoirMaxes, itemName, itemCurrentMasteryLevel + 1 + 5, false);
            NotificationManager.Instance.DisplayNotification($"{itemName} Mastery Level {itemCurrentMasteryLevel + 1}!");
        }

        //
        //
        // Handle Skill Level Up
        int currentSkillLevel = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.skillLevels, skillName);
        float currentSkillXP = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.skillXP, skillName);
        float levelXPReq = SkillManager.Instance.Calculate_XPToNextLevel(currentSkillLevel);

        if (currentSkillXP >= levelXPReq)
        {
            // Level up and update XP
            float leftOverXP = currentSkillXP - levelXPReq;
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.skillLevels, skillName, currentSkillLevel + 1, false);
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.skillXP, skillName, leftOverXP, false);

            // Check all unlocks for this skill
            SkillManager.Instance.CheckSkillUnlocks(skillName);

            // Notify level up
            NotificationManager.Instance.DisplayNotification($"{skillName} Level {currentSkillLevel + 1}!");
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



public void StartCrafting()
    {
        // Check for output belt
        if (outputBelt != null)
        {
            // Start Mining
            itemToCraft = selectedRecipe;
            tickTimer = 0;
            functioning = true;
        }
        else
        {
            NotificationManager.Instance.DisplayNotification("No Output Belt");
        }
    }

    public void StopCrafting()
    {
        // Stop Mining
        itemToCraft = null;
        tickTimer = 0;
        functioning = false;
    }

}
