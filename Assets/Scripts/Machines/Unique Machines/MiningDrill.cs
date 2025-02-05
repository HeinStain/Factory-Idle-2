using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;


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
    public int timeToCraft = 60;
    public int timeToVeinRespawn = 100;
    public Dictionary<string, int> oreVeinCounts = new Dictionary<string, int>();

    [Header("Visuals")]
    [SerializeField] private GameObject menuCanvas;

    [SerializeField] private MachineData belt;

    public override string ToString()
    {
        return "Mining";
    }



#region Main

    protected override void MachineSetup()
    {
        // Get grid
        gridArray = GameManager.Instance.GetGrid();

        // Create Ore Vein Dictionary
        foreach (ItemData ore in ItemManager.Instance.itemList)
        {
            oreVeinCounts.Add(ore.displayName, SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.oreVeinMax, ore.displayName));
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
        if (tickTimer >= timeToCraft && !respawningVein)
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
            ItemManager.SpawnObject(ItemManager.Instance.itemPrefab, oreToCraft, outputPos, Quaternion.identity);
            
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
                int maxVeinCount = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.oreVeinMax, oreToCraft.displayName);
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

        string machineName = GetMachineName();
        string oreName = oreToCraft.displayName;
        
        // Add Level, Mastery Pool and Mastery XP
        AddSkillXP(SkillManager.Instance.skillXP, machineName, oreToCraft.XP);
        AddSkillXP(SkillManager.Instance.skillMasteryPoolXP, machineName, SkillManager.Instance.CalculateGivenMasteryPoolXP(oreName));
        AddSkillXP(SkillManager.Instance.oreMasteryXP, oreName, SkillManager.Instance.CalculateGivenOreMasteryXP(oreName));

        // Handle Ore Mastery Level Up
        HandleOreMasteryLevelUp(oreName);

        // Handle Skill Level Up
        HandleSkillLevelUp(machineName);
    }
    
    private void AddSkillXP(Dictionary<string, float> xpPool, string key, float xpAmount)
    {
        SkillManager.Instance.AddXP(xpPool, key, xpAmount);
    }

    private void HandleOreMasteryLevelUp(string oreName)
    {
        int currentOreMasteryLevel = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.oreMasteryLevel, oreName);
        float currentOreMasteryXP = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.oreMasteryXP, oreName);
        float masteryLevelXPReq = SkillManager.Instance.CalculateOreMasteryXPToNextLevel(currentOreMasteryLevel);

        if (currentOreMasteryXP >= masteryLevelXPReq)
        {
            // Level up and update XP
            float leftOverMasteryXP = currentOreMasteryXP - masteryLevelXPReq;
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.oreMasteryLevel, oreName, currentOreMasteryLevel + 1, false);
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.oreMasteryXP, oreName, leftOverMasteryXP, false);

            // Update max vein and notify
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.oreVeinMax, oreName, SkillManager.Instance.CalculateVeinMax(currentOreMasteryLevel + 1), false);
            NotificationManager.Instance.DisplayNotification($"{oreName} Mastery Level {currentOreMasteryLevel + 1}!");
        }
    }

    private void HandleSkillLevelUp(string machineName)
    {
        int currentMiningLevel = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.skillLevels, machineName);
        float currentMiningXP = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.skillXP, machineName);
        float levelXPReq = SkillManager.Instance.CalculateXPToNextLevel(currentMiningLevel);

        if (currentMiningXP >= levelXPReq)
        {
            // Level up and update XP
            float leftOverXP = currentMiningXP - levelXPReq;
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.skillLevels, machineName, currentMiningLevel + 1, false);
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.skillXP, machineName, leftOverXP, false);

            // Notify level up and check to unlock new ore
            NotificationManager.Instance.DisplayNotification($"{machineName} Level {currentMiningLevel + 1}!");
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