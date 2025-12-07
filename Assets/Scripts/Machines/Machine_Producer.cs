using System.Collections.Generic;
using UnityEngine;
using System.Linq;


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
        gridMap = GameManager.Instance.GetGrid();

        // Set skill tag from machine data
        skillData = machineData.skill;
        //Debug.Log($"Setting up {machineData.machineName} with skillData: {skillData}");

        // Now create dictionaries with verified tag
        foreach (ItemData item in ItemManager.Instance.itemList)
        {
            if (item.skillData == skillData)
            {
                //Debug.Log($"Adding {item.displayName} to {machineData.machineName} with tag {skillData}");
                reservoirMaxes.Add(item.displayName, 
                    SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.itemMasteryLevels, item.displayName) + 5);
                reservoirNodeCounts.Add(item.displayName, 
                    SkillManager.Instance.GetDictionaryValue(reservoirMaxes, item.displayName));
            }
        }
        
        BuildManager.Instance.OnMachinePlaced += RefreshBelt;
    }

    private void OnDestroy()
    {
        BuildManager.Instance.OnMachinePlaced -= RefreshBelt;
    }

    private void Update()
    {
        if (turnedOn)   { CraftingCycle();     } // Crafting
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
                ProduceItem(); 
            }
            return; // Don’t progress crafting until item is placed
        }


        // Normal crafting
        if (tickTimer >= machineData.skill.defaultCraftingInterval && !respawningReservoir)
        {
            if (outputBelt != null && outputBelt.RearSlotAvailable())
            {
                ProduceItem();
            }
            else
            {
                itemPending = true;
            }
        }
        
        // Respawn Reservoir
        if (respawningReservoir)
        {
            if (tickTimer >= timeToReservoirRespawn)
            {
                // Set Reservoir to max
                int reservoirMax = SkillManager.Instance.GetDictionaryValue(reservoirMaxes, activeRecipe.displayName);
                SkillManager.Instance.ModifyDictionaryValue(reservoirNodeCounts, activeRecipe.displayName, reservoirMax, false);
                // Reset
                respawningReservoir = false;
                // Timer
                tickTimer = 0;
            }
        }
    }

    private void ProduceItem()
    {
        // Distribute XP
        HandleXPDistribution();

        tickTimer = 0; 
        ItemManager.SpawnObject(outputBelt, activeRecipe);
      
        // Subtract from Reservoir Count
        SkillManager.Instance.ModifyDictionaryValue(reservoirNodeCounts, activeRecipe.displayName, -1, true);

        // Check for last Reservoir Node
        int currentNodeCount = SkillManager.Instance.GetDictionaryValue(reservoirNodeCounts, activeRecipe.displayName);
        if (currentNodeCount < 1) { respawningReservoir = true; } 
    }

#endregion


#region XP Distribution
    private void HandleXPDistribution()
    {

        SkillData skillData = machineData.skill;
        string itemName = activeRecipe.displayName;

        // Add Skill XP
        SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.skillXP, skillData, activeRecipe.XP, true);
        // Add Mastery and Mastery Pool XP
        float givenMasteryXP = SkillManager.Instance.Calculate_GivenItemMasteryXP(itemName, skillData);
        SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.skillMasteryPoolXP, skillData, givenMasteryXP/4, true);
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
        int currentSkillLevel = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.skillLevels, skillData);
        float currentSkillXP = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.skillXP, skillData);
        float levelXPReq = SkillManager.Instance.Calculate_XPToNextLevel(currentSkillLevel);

        if (currentSkillXP >= levelXPReq)
        {
            // Level up and update XP
            float leftOverXP = currentSkillXP - levelXPReq;
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.skillLevels, skillData, currentSkillLevel + 1, false);
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.skillXP, skillData, leftOverXP, false);

            // Check all unlocks for this skill
            SkillManager.Instance.CheckSkillUnlocks(skillData);

            // Notify level up
            NotificationManager.Instance.DisplayNotification($"{skillData} Level {currentSkillLevel + 1}!");
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
