using UnityEngine;

public class Machine_Furnace : Machine
{
    public override string ToString() => "Furnace";

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

#region Setup

    protected override void MachineSetup()
    {
        // Get grid first
        gridMap = GameManager.Instance.GetGrid();

        // Set skill tag from machine data
        skillData = machineData.skill;
        
        BuildManager.Instance.OnMachinePlaced += RefreshBelt;
    }

    private void OnDestroy()
    {
        BuildManager.Instance.OnMachinePlaced -= RefreshBelt;
    }

#endregion

#region Main

    private void Update()
    {
        if (turnedOn)   { CraftingCycle();     } // Crafting
    }

    public override void StartCrafting()
    {
        // Start skillManagerelting
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

#region Crafting Cycle

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
        if (tickTimer >= machineData.skill.defaultCraftingInterval)
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
    }

    private void ProduceItem()
    {
        // Distribute XP
        HandleXPDistribution();
        
        // Spawn Item
        tickTimer = 0;
        ItemManager.SpawnObject(outputBelt, activeRecipe);
    }

#endregion

#region XP Distribution
    private void HandleXPDistribution()
    {
        SkillManager skillManager = SkillManager.Instance;
        SkillData skillData = machineData.skill;
        string itemName = activeRecipe.displayName;

        // Add Skill XP
        skillManager.ModifyDictionaryValue(skillManager.skillXP, skillData, activeRecipe.XP, true);
        // Add Mastery and Mastery Pool XP
        float givenMasteryXP = skillManager.Calculate_GivenItemMasteryXP(itemName, skillData);
        skillManager.ModifyDictionaryValue(skillManager.skillMasteryPoolXP, skillData, givenMasteryXP/4, true);
        skillManager.ModifyDictionaryValue(skillManager.itemMasteryXPCount, itemName, givenMasteryXP, true);
 
        //
        //
        // Handle Item Mastery Level Up
        int itemCurrentMasteryLevel = skillManager.GetDictionaryValue(skillManager.itemMasteryLevels, itemName);
        float itemCurrentMasteryXP = skillManager.GetDictionaryValue(skillManager.itemMasteryXPCount, itemName);
        float masteryLevelXPReq = skillManager.Calculate_XPToNextLevel(itemCurrentMasteryLevel);

        if (itemCurrentMasteryXP >= masteryLevelXPReq)
        {
            // Level up and update XP
            float leftOverMasteryXP = itemCurrentMasteryXP - masteryLevelXPReq;
            skillManager.ModifyDictionaryValue(skillManager.itemMasteryLevels, itemName, itemCurrentMasteryLevel + 1, false);
            skillManager.ModifyDictionaryValue(skillManager.itemMasteryXPCount, itemName, leftOverMasteryXP, false);
        }

        //
        //
        // Handle Skill Level Up
        int currentSkillLevel = skillManager.GetDictionaryValue(skillManager.skillLevels, skillData);
        float currentSkillXP = skillManager.GetDictionaryValue(skillManager.skillXP, skillData);
        float levelXPReq = skillManager.Calculate_XPToNextLevel(currentSkillLevel);

        if (currentSkillXP >= levelXPReq)
        {
            // Level up and update XP
            float leftOverXP = currentSkillXP - levelXPReq;
            skillManager.ModifyDictionaryValue(skillManager.skillLevels, skillData, currentSkillLevel + 1, false);
            skillManager.ModifyDictionaryValue(skillManager.skillXP, skillData, leftOverXP, false);

            // Check all unlocks for this skill
            skillManager.CheckSkillUnlocks(skillData);

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
