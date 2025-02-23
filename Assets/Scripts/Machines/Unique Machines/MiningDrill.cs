using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using System.Linq;


public class MiningDrill : Machine
{

    [Header("Grid")]
    private Grid<GridMap.GridTile> gridArray;
    private GridMap.GridTile outputTile;
    public Belt outputBelt;

    [Header("Selection")]
    public ItemData selectedOre;
    public ItemData oreToCraft;

    [Header("Data")]
    public bool mining = false;
    public bool respawningVein;
    public int timeToVeinRespawn = 100;
    public Dictionary<string, int> oreVeinCounts = new Dictionary<string, int>();
    public Dictionary<string, int> oreVeinMax = new Dictionary<string, int>();
    

    [Header("Visuals")]
    [SerializeField] private GameObject menuCanvas;

    [SerializeField] private MachineData belt;


#region Main

    protected override void MachineSetup()
    {
        // Get grid
        gridArray = GameManager.Instance.GetGrid();

        // Create Ore Vein Dictionary
        foreach (ItemData item in ItemManager.Instance.itemList.Where(item => item.tags.Contains(ItemTag.Mining)))
        {
            oreVeinMax.Add(item.displayName, SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.itemMasteryLevels, item.displayName) + 5);
            oreVeinCounts.Add(item.displayName, SkillManager.Instance.GetDictionaryValue(oreVeinMax, item.displayName));
        }
        
        DispenseUpdateCycle();

    }
    

    private void Update()
    {
        if (mining)   { CraftingCycle();     } // Crafting
    }


    
    

#endregion

#region Get Output Belt

    protected override void GetDispenseTile()  
    {
        // Calculate tile Position
        // Get machine origin
        Vector2Int tileXY = originXY;

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

        // Mine Ore
        if (tickTimer >= machineData.defaultCraftingInterval && !respawningVein)
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
            ItemManager.SpawnObject(outputBelt, ItemManager.Instance.itemPrefab, oreToCraft, outputPos, Quaternion.identity);
            
            // Subtract from Vein Count
            SkillManager.Instance.ModifyDictionaryValue(oreVeinCounts, oreToCraft.displayName, -1, true);

            // Distribute XP
            HandleXPDistribution();

            // Check for last Vein
            int currentVeinCount = SkillManager.Instance.GetDictionaryValue(oreVeinCounts, oreToCraft.displayName);
            if (currentVeinCount < 1) { respawningVein = true; }   
        }
        
        // Respawn Vein
        if (respawningVein)
        {
            if (tickTimer >= timeToVeinRespawn)
            {
                // Timer
                tickTimer = 0;
                // Set vein to max
                int maxVeinCount = SkillManager.Instance.GetDictionaryValue(oreVeinMax, oreToCraft.displayName);
                SkillManager.Instance.ModifyDictionaryValue(oreVeinCounts, oreToCraft.displayName, maxVeinCount, false);
                // Reset
                respawningVein = false;
            }
        }
    }

#endregion



#region XP Distribution
    private void HandleXPDistribution()
    {

        string skillName = machineData.skillName;
        string oreName = oreToCraft.displayName;
        
        // Add Skill XP
        SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.skillXP, skillName, oreToCraft.XP, true);
        // Add Mastery and Mastery Pool XP
        float givenMasteryXP = SkillManager.Instance.Calculate_GivenItemMasteryXP(oreName, ItemTag.Mining);
        SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.skillMasteryPoolXP, skillName, givenMasteryXP/4, true);
        SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.itemMasteryXPCount, oreName, givenMasteryXP, true);
        
        //
        //
        // Handle Ore Mastery Level Up
        int currentOreMasteryLevel = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.itemMasteryLevels, oreName);
        float currentOreMasteryXP = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.itemMasteryXPCount, oreName);
        float masteryLevelXPReq = SkillManager.Instance.Calculate_XPToNextLevel(currentOreMasteryLevel);

        if (currentOreMasteryXP >= masteryLevelXPReq)
        {
            // Level up and update XP
            float leftOverMasteryXP = currentOreMasteryXP - masteryLevelXPReq;
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.itemMasteryLevels, oreName, currentOreMasteryLevel + 1, false);
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.itemMasteryXPCount, oreName, leftOverMasteryXP, false);

            // Update max vein and notify
            SkillManager.Instance.ModifyDictionaryValue(oreVeinMax, oreName, currentOreMasteryLevel + 1 + 5, false);
            NotificationManager.Instance.DisplayNotification($"{oreName} Mastery Level {currentOreMasteryLevel + 1}!");
        }

        //
        //
        // Handle Skill Level Up
        int currentMiningLevel = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.skillLevels, skillName);
        float currentMiningXP = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.skillXP, skillName);
        float levelXPReq = SkillManager.Instance.Calculate_XPToNextLevel(currentMiningLevel);

        if (currentMiningXP >= levelXPReq)
        {
            // Level up and update XP
            float leftOverXP = currentMiningXP - levelXPReq;
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.skillLevels, skillName, currentMiningLevel + 1, false);
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.skillXP, skillName, leftOverXP, false);

            // Notify level up and check to unlock new ore
            NotificationManager.Instance.DisplayNotification($"{skillName} Level {currentMiningLevel + 1}!");
            SkillManager.Instance.UnlockNewOreCheck();
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
            oreToCraft = selectedOre;
            tickTimer = 0;
            mining = true;
        }
        else
        {
            NotificationManager.Instance.DisplayNotification("No Output Belt");
        }
    }

    public void StopCrafting()
    {
        // Stop Mining
        oreToCraft = null;
        tickTimer = 0;
        mining = false;
    }


}