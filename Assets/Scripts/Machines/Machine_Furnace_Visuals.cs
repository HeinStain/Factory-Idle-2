using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System.Data;
using System.Collections.Generic;

public class Machine_Furnace_Visuals : MonoBehaviour, IRecipeMenu
{
    [Header("Function")]
        private Animator anim; 
        private ItemData selectedRecipe;
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
        [SerializeField] private Image part1Icon, part2Icon, resultIcon;
        [SerializeField] private TextMeshProUGUI part1ReqText, part2ReqText, resultValueText;
        [SerializeField] private GameObject arrow, part1Obj, part2Obj;
        
    [Header("Display Card - Info")]
        [SerializeField] private Button craftButton;
        [SerializeField] private TextMeshProUGUI value, craftTime;

    [Header("Display Card - Progress Bar")]
        [SerializeField] private TextMeshProUGUI progressPercentText;
        [SerializeField] private GameObject progressBar;
        [SerializeField] private Image progressBarImage;


#region Setup
    private void Awake()
    {
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
        if (placed) return;

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

        // If placed, animate and handle progress bars and menu
        if (placed) 
        { 
            //anim.SetBool("IsActive", machineScript.turnedOn);
            
            if (machineScript != null) 
            { 
                MachineProgressBarVisuals(); 

                if (machineScript.turnedOn && selectedRecipe != null)
                {
                    DisplayRecipe(selectedRecipe);
                }
                else
                {
                    ClearRecipeDisplay();
                }
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
    }

#endregion

#region Machine Display Bar

    private void MachineProgressBarVisuals()
    {
        // Set Bar to 0 if machine is off
        if (!machineScript.turnedOn) { machineProgressBar.transform.localScale = new Vector3(0f,1f,1f); return; }
        
        // Otherwise, show Display Bar
        machineProgressBarSR.sprite = greenProgressBar;
        float t = Mathf.Clamp01((float)machineScript.tickTimer / (float)machineData.defaultCraftingInterval);
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
        machineScript.turnedOn = false;

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

        craftButton.transform.GetComponentInChildren<TextMeshProUGUI>().text = "Recipe Selected";
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
            Debug.LogError($"Selected recipe {selectedRecipe.displayName} doesn't match machine skill {machineData}");
            selectedRecipe = null;
            return false;
        }

        return true;
    }

    private void ResetDisplayCard()
    {
        displayCardOverlay.SetActive(true);
        /*
        resultIcon.gameObject.SetActive(false);

        titleText.text = "";
        itemMasteryLevelText.text = "";
        itemMasteryProgressText.text = "";

        veinCount.text = "";
        value.text = "";

        craftTime.text = "";
        xp.text = "";
        masteryXP.text = "";
        masteryPoolXP.text = "";

        itemMasteryProgressBar.transform.localScale = new Vector2(0f,1f);
        itemMasteryProgressText.text = "0 / 0";

        progressPercentText.text = "";
        craftButton.onClick.RemoveAllListeners();
        */
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
        arrow.SetActive(false);
        part1Obj.gameObject.SetActive(false);
        part2Obj.gameObject.SetActive(false);

        // Checks for Ingredient
        if (selectedRecipe.ingredients.Count > 0)
        {
            arrow.SetActive(true);
            part1Obj.SetActive(true);
            part1Icon.gameObject.SetActive(true);
            part1Icon.sprite = selectedRecipe.ingredients[0].icon;
            part1Icon.SetNativeSize();
            
            //Check for Second Ingredient
            if (selectedRecipe.ingredients.Count > 1)
            {
                part2Obj.SetActive(true);
                part2Icon.gameObject.SetActive(true);
                part2Icon.sprite = selectedRecipe.ingredients[1].icon;
                part2Icon.SetNativeSize();
            }
        }

        // Recipe Result
        resultIcon.gameObject.SetActive(true);
        resultIcon.sprite = selectedRecipe.icon;
        resultIcon.SetNativeSize();

   
        // Info Display
        value.text = selectedRecipe.value.ToString();
        craftTime.text = (machineData.defaultCraftingInterval / 30).ToString("F2");
     
        CraftingProgressBar();
    }

    private void CraftingProgressBar()
    {
        // Reset if off
        if (!machineScript.turnedOn)
        {
            progressPercentText.text = "0%";
            progressBar.transform.localScale = new Vector3(0f,2.5f,1f);
            return;
        }

        // Set bar colour 
        progressBarImage.sprite = redProgressBar;

        // Get time limits
        float limit = machineData.defaultCraftingInterval;

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
