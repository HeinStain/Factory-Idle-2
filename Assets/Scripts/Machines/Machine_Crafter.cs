using UnityEngine;
using System.Collections.Generic;

public class Machine_Crafter : Machine
{
    public override string ToString() => "Crafter";

    [Header("Grid")]
    private GridMap gridMap;
    private GridMap.GridTile outputTile;
    public Belt outputBelt;

    [Header("Selection")]
    public ItemData selectedRecipe;
    public ItemData activeRecipe;

    private bool itemPending = false;

    [Header("Ingredients")]
    [SerializeField] private List<IngredientSlot> ingredientSlots = new();

    [System.Serializable]
    private class IngredientSlot
    {
        public ItemData acceptedItem;
        public bool filled;
    }

    [Header("Visuals")]
    [SerializeField] private GameObject menuCanvas;

#region Main

    protected override void MachineSetup()
    {
        // Get grid first
        gridMap = GameManager.Instance.GetGrid();
        BuildManager.Instance.OnMachinePlaced += RefreshBelt;
    }

    private void OnDestroy()
    {
        BuildManager.Instance.OnMachinePlaced -= RefreshBelt;
    }

    private void Update()
    {
        // Auto-start once a recipe is selected and every ingredient slot is filled
        if (!turnedOn && selectedRecipe != null && AllSlotsFilled())
        {
            StartCrafting();
        }

        if (turnedOn) { CraftingCycle(); } // Crafting
    }

    public override void StartCrafting()
    {
        // Start Smelting
        activeRecipe = selectedRecipe;
        ConsumeIngredients();
        tickTimer = 0;
        turnedOn = true;
    }

    public override void StopCrafting()
    {
        // Stop Smelting
        activeRecipe = null;
        tickTimer = 0;
        turnedOn = false;
        itemPending = false;
    }

    public override void SetSelectedRecipe(ItemData item)
    {
        selectedRecipe = item;
        BuildIngredientSlots(item);
    }

#endregion

#region Ingredient Slots

    private void BuildIngredientSlots(ItemData recipe)
    {
        if (ingredientSlots.Exists(slot => slot.filled))
        {
            Debug.Log($"Discarding filled ingredients in {machineData.machineName} due to recipe change");
        }

        ingredientSlots.Clear();

        if (recipe == null) return;

        foreach (ItemData ingredient in recipe.ingredients)
        {
            ingredientSlots.Add(new IngredientSlot { acceptedItem = ingredient, filled = false });
        }
    }

    private bool AllSlotsFilled()
    {
        if (ingredientSlots.Count == 0) return false;

        foreach (IngredientSlot slot in ingredientSlots)
        {
            if (!slot.filled) return false;
        }

        return true;
    }

    private void ConsumeIngredients()
    {
        foreach (IngredientSlot slot in ingredientSlots)
        {
            slot.filled = false;
        }
    }

    // Non-mutating check: would TryDepositIngredient succeed right now?
    public bool CanAcceptIngredient(ItemData item)
    {
        if (selectedRecipe == null) return false;

        foreach (IngredientSlot slot in ingredientSlots)
        {
            if (slot.acceptedItem == item && !slot.filled) return true;
        }

        return false;
    }

    // Called by Grabbers to deliver an ingredient. Returns whether it was accepted.
    public bool TryDepositIngredient(ItemData item)
    {
        if (!CanAcceptIngredient(item)) return false;

        foreach (IngredientSlot slot in ingredientSlots)
        {
            if (slot.acceptedItem == item && !slot.filled)
            {
                slot.filled = true;
                return true;
            }
        }

        return false;
    }

    // Read-only access for the display UI
    public int GetIngredientSlotCount() => ingredientSlots.Count;
    public ItemData GetIngredientSlotItem(int index) => ingredientSlots[index].acceptedItem;
    public bool GetIngredientSlotFilled(int index) => ingredientSlots[index].filled;

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
                ItemManager.SpawnObject(outputBelt, activeRecipe);
                StopCrafting();
            }
            return; // Don’t progress crafting until item is placed
        }

        // Normal crafting
        if (tickTimer >= machineData.defaultCraftingInterval)
        {
            if (outputBelt != null && outputBelt.RearSlotAvailable())
            {
                ItemManager.SpawnObject(outputBelt, activeRecipe);
                StopCrafting();
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
