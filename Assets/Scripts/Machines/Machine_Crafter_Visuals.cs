using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System.Data;
using System.Collections.Generic;

public class Machine_Crafter_Visuals : MonoBehaviour, IRecipeMenu
{
    [Header("Function")]
        private Animator anim; 
        private ItemData selectedRecipe;
        private Machine owner;
        private Machine machineScript;
        private MachineData machineData;
        private string skillName;
        private bool menuOpen = false;
        private bool placed = false;
        private Dictionary<ItemData, RecipeBlock> recipeBlocks = new();
        public Machine GetMachineScript() => machineScript;
        public void SelectItem(ItemData item) => SelectItemInternal(item);
        public ItemData GetSelectedRecipe() => selectedRecipe;

    [Header("Machine Parts")]
        [SerializeField] private SpriteRenderer recipeDisplaySprite;
        [SerializeField] private GameObject machineProgressBar;
        [SerializeField] private SpriteRenderer machineProgressBarSR;
        [SerializeField] List<Transform> objectsToKeepStraight;
        
    [Header("Resources")]
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private GameObject recipeCard;
        [SerializeField] private Sprite redProgressBar, greenProgressBar;

    [Header("Display Card - Header")]
        [SerializeField] private GameObject displayCardOverlay;
        [SerializeField] private TextMeshProUGUI titleText;

    [Header("Display Card - Recipe")]
        [SerializeField] private Image resultIcon;
        [SerializeField] private TextMeshProUGUI resultValueText;
        [SerializeField] private GameObject arrow;
        [SerializeField] private Transform ingredientSlotsRoot;
        [SerializeField] private GameObject ingredientSlotPrefab;
        private List<IngredientSlotUI> ingredientSlotUIs = new();

    [Header("Display Card - Info")]
        [SerializeField] private TextMeshProUGUI value, craftTime;

    [Header("Display Card - Progress Bar")]
        [SerializeField] private TextMeshProUGUI progressPercentText;
        [SerializeField] private GameObject progressBar;
        [SerializeField] private Image progressBarImage;


#region Setup
    private void Awake()
    {
        // Only a placed machine's own visual binds to it. The build ghost's copy has no Machine above it.
        owner = GetComponentInParent<Machine>();
        if (owner == null) return;

        GameManager.Instance.OnMenuTriggered += MenuTrigger;
        BuildManager.Instance.OnMachinePlaced += Place;
    }

    private void OnDestroy() 
    {
        GameManager.Instance.OnMenuTriggered -= MenuTrigger;
        BuildManager.Instance.OnMachinePlaced -= Place;
    }

    private void Place(Machine machine)
    {
        if (placed || machine != owner) return;

        machineScript = machine;
        machineData = machineScript.GetMachineData();
        anim = machineScript.transform.GetComponent<Animator>();
        placed = true;
    }

    private void MenuTrigger(bool status)
    {   
        menuOpen = status;
        if (machineScript != null && menuOpen)
        {
            GenerateItemsList();
        }
    }


#endregion 


#region Update

    private void Update()
    {
        // Keep objects straight
        foreach (Transform obj in objectsToKeepStraight) { obj.eulerAngles = new Vector3(0,0,0); }

        // Stop if not placed yet, or the machine has been destroyed
        if (!placed || machineScript == null) return;

        // Animate and handle progress bars and menu
        //if (anim != null) anim.SetBool("IsActive", machineScript.turnedOn);

        MachineProgressBarVisuals();

        if (machineScript.turnedOn && selectedRecipe != null)
        {
            DisplayRecipe(selectedRecipe);
        }
        else
        {
            ClearRecipeDisplay();
        }

        if (menuOpen)
        {
            if (HasValidSelection())
            {
                Update_DisplayCard();
                Update_RecipeCard();
            }
            else
            {
                ResetDisplayCard();
            }
        }
    }

#endregion

#region Machine Display Bar

    private void MachineProgressBarVisuals()
    {
        // Set Bar to 0 if machine is off or no recipe is selected
        if (!machineScript.turnedOn || selectedRecipe == null) { machineProgressBar.transform.localScale = new Vector3(0f,1f,1f); return; }

        // Otherwise, show Display Bar
        machineProgressBarSR.sprite = greenProgressBar;
        float t = Mathf.Clamp01((float)machineScript.tickTimer / (float)machineScript.GetCraftTicks(selectedRecipe));
        machineProgressBar.transform.localScale = new Vector3(t,1f,1f);
    }

#endregion

#region MenuFunction


    private void GenerateItemsList()
    {
        // Clear existing recipe block objects
        foreach (Transform child in contentRoot.transform) { Destroy(child.gameObject); }

        // Empty block dictionary
        recipeBlocks.Clear();

        // Filter and sort recipes to a list
        var sortedItems = ItemManager.Instance.itemList
            .Where(item => item.producedBy == machineData)    // Filter to relevant items
            .ToList();                                                           // Turn it into a list

        // Create recipe blocks from list 
        foreach (ItemData itemData in sortedItems)
        {
            GameObject recipeBlockObj = Instantiate(recipeCard, contentRoot.transform);
            RecipeBlock recipeBlockScript = recipeBlockObj.GetComponent<RecipeBlock>();
            recipeBlockScript.UpdateBlock(this, itemData);
            recipeBlocks[itemData] = recipeBlockScript;
        }
    }

    public void SelectItemInternal(ItemData item)
    {
        // Safety checks
        if (item == null) { Debug.LogError("Attempting to select null item"); return; }
        if (!ItemManager.Instance.itemList.Contains(item)) { Debug.LogError("Selected Item not in ItemManager.itemList"); return; }
        if (item == selectedRecipe) { Debug.Log("Item already selected"); return; }

        // Print selection
        Debug.Log($"Selecting item: {item.displayName} in {machineData.machineName}");
        
        // Turn off machine production
        machineScript.StopCrafting();

        // Set new selection
        selectedRecipe = item;
        machineScript.SetSelectedRecipe(item);

        // Update only the selected items
        foreach (var kvp in recipeBlocks)
        {
            RecipeBlock itemBlock = kvp.Value;
            if (itemBlock.itemData == selectedRecipe) { itemBlock.UpdateBlockVisuals(); }
            itemBlock.UpdateBlockVisuals();
        }
    }

#endregion


#region Validation and Reset
    
    private bool HasValidSelection()
    {
        // Verify recipe is selected
        if (selectedRecipe == null) return false;

        // Verify recipe has correct tag
        if (selectedRecipe.producedBy != machineData)
        {
            Debug.LogError($"Selected recipe {selectedRecipe.displayName} doesn't match machine {machineData}");
            selectedRecipe = null;
            return false;
        }

        return true;
    }

    private void ResetDisplayCard()
    {
        displayCardOverlay.SetActive(true);
    }

#endregion

#region Update Visuals
    
    private void Update_DisplayCard()
    {
        // Remove overlay
        displayCardOverlay.SetActive(false);

        // Header
        titleText.text = selectedRecipe.displayName;
        
        // Reset Recipe
        arrow.SetActive(selectedRecipe.ingredients.Count > 0);

        // Ingredient slots
        UpdateIngredientSlots();

        // Recipe Result
        resultIcon.gameObject.SetActive(true);
        resultIcon.sprite = selectedRecipe.icon;

   
        // Info Display
        value.text = selectedRecipe.value.ToString();
        craftTime.text = selectedRecipe != null
            ? (machineScript.GetCraftTicks(selectedRecipe) / 30f).ToString("F2")
            : "0.00";
     
        CraftingProgressBar();
    }

    private void UpdateIngredientSlots()
    {
        Machine_Crafter crafter = machineScript as Machine_Crafter;
        if (crafter == null || ingredientSlotsRoot == null || ingredientSlotPrefab == null) return;

        int slotCount = crafter.GetIngredientSlotCount();

        // Grow the pool if this recipe needs more slots than we've built so far
        while (ingredientSlotUIs.Count < slotCount)
        {
            GameObject slotObj = Instantiate(ingredientSlotPrefab, ingredientSlotsRoot);
            ingredientSlotUIs.Add(slotObj.GetComponent<IngredientSlotUI>());
        }

        // Show and update the slots this recipe needs, hide the rest of the pool
        for (int i = 0; i < ingredientSlotUIs.Count; i++)
        {
            bool active = i < slotCount;
            ingredientSlotUIs[i].gameObject.SetActive(active);

            if (active)
            {
                // Keep slots as the first children of their parent, ahead of the arrow/result objects
                ingredientSlotUIs[i].transform.SetSiblingIndex(i);
                ingredientSlotUIs[i].UpdateVisual(crafter.GetIngredientSlotItem(i), crafter.GetIngredientSlotFilled(i));
            }
        }
    }

    private void CraftingProgressBar()
    {
        // Reset if off or no recipe is selected
        if (!machineScript.turnedOn || selectedRecipe == null)
        {
            progressPercentText.text = "0%";
            progressBar.transform.localScale = new Vector3(0f,2.5f,1f);
            return;
        }

        // Set bar colour
        progressBarImage.sprite = redProgressBar;

        // Get time limits
        float limit = machineScript.GetCraftTicks(selectedRecipe);

        // Calculate and normalize time (0-1)
        float progress = (limit > 0f) 
            ? Mathf.Clamp01(machineScript.tickTimer / limit) 
            : 0f;

        // Scale bar, show percentage
        progressPercentText.text = $"{progress * 100:0}%"; 
        progressBar.transform.localScale = new Vector3(progress,1f,1f);
    }

    private void Update_RecipeCard()
    {
        if (recipeBlocks.TryGetValue(selectedRecipe, out var selectedBlock))
        {                            
            selectedBlock.UpdateBlock(this, selectedBlock.itemData);
        }
    }


#endregion

#region 
    

    private void DisplayRecipe(ItemData recipe)
    {
        recipeDisplaySprite.sprite = recipe.icon;
    }

    private void ClearRecipeDisplay()
    {
        recipeDisplaySprite.sprite = null;
    }

#endregion


}
