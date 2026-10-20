using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System.Data;
using System.Collections.Generic;


public class Machine_Producer_Visuals : MonoBehaviour, IRecipeMenu
{

    [Header("Function")]
        private Animator anim;
        private ItemData selectedRecipe;
        private Machine_Producer producerScript;
        private MachineData machineData;
        private bool menuOpen = false;
        private bool placed = false;
        private Dictionary<ItemData, RecipeBlock> recipeBlocks = new();
        public Machine GetMachineScript() => producerScript;
        public void SelectItem(ItemData item) => SelectItemInternal(item);
        public ItemData GetSelectedRecipe() => selectedRecipe;

    [Header("Machine Parts")]
        [SerializeField] private SpriteRenderer recipeDisplaySprite;
        [SerializeField] private GameObject machineProgressBar;
        [SerializeField] private SpriteRenderer machineProgressBarSR;
        [SerializeField] List<Transform> objectsToKeepStraight;

    [Header("Resources")]
        [SerializeField] private GameObject recipeFolder;
        [SerializeField] private GameObject producerRecipeCard;
        [SerializeField] private Sprite redProgressBar, greenProgressBar;
        
    [Header("Display Card - Header")]
        [SerializeField] private GameObject displayCardOverlay;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI titleText;

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

        producerScript = machine as Machine_Producer;
        machineData = producerScript.GetMachineData();
        anim = producerScript.transform.GetComponent<Animator>();
        placed = true;
    }

    private void MenuTrigger(bool status)
    {   
        menuOpen = status;
        if (producerScript != null && menuOpen)
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
            anim.SetBool("IsActive", producerScript.turnedOn);
            
            if (producerScript != null) 
            { 
                MachineProgressBarVisuals(); 

                if (producerScript.turnedOn && selectedRecipe != null)
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
        // Set Bar to 0 if machine is off or no recipe is selected
        if (!producerScript.turnedOn || selectedRecipe == null) { machineProgressBar.transform.localScale = new Vector3(0f,1f,1f); return; }

        // Show Display Bar
        machineProgressBarSR.sprite = greenProgressBar;
        float t = Mathf.Clamp01((float)producerScript.tickTimer / (float)producerScript.GetCraftTicks(selectedRecipe));
        machineProgressBar.transform.localScale = new Vector3(t,1f,1f);
    }

#endregion

#region MenuFunction


    private void GenerateItemsList()
    {
        // Clear existing recipe block objects
        foreach (Transform child in recipeFolder.transform) { Destroy(child.gameObject); }

        // Empty block dictionary
        recipeBlocks.Clear();

        // Filter and sort recipes to a list
        var sortedItems = ItemManager.Instance.itemList
            .Where(item => item.producedBy == machineData)    // Filter to relevant items
            .ToList();                                                  // Turn it into a list

        // Create recipe blocks from list 
        foreach (ItemData itemData in sortedItems)
        {
            GameObject recipeBlockObj = Instantiate(producerRecipeCard, recipeFolder.transform);
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
        producerScript.StopCrafting();

        // Set new selection
        selectedRecipe = item;
        producerScript.SetSelectedRecipe(item);

        // Update only the selected items
        foreach (var kvp in recipeBlocks)
        {
            RecipeBlock itemBlock = kvp.Value;
            if (itemBlock.itemData == selectedRecipe) { itemBlock.UpdateBlockVisuals(); }
            itemBlock.UpdateBlockVisuals();
        }

        UpdateCraftButton();
    }

#endregion

#region Craft Button

    // Called when "Craft" or "Stop" button is pressed
    public void UpdateCraftButton()
    {
        if (producerScript.turnedOn) SetStopButton();
        else SetCraftButton();
    }

    private void SetCraftButton()
    {
        craftButton.onClick.RemoveAllListeners();
        craftButton.onClick.AddListener(() => producerScript.StartCrafting());
        craftButton.onClick.AddListener(() => UpdateCraftButton());
        craftButton.transform.GetComponentInChildren<TextMeshProUGUI>().text = "CRAFT";
    }

    private void SetStopButton()
    {
        craftButton.onClick.RemoveAllListeners();
        craftButton.onClick.AddListener(() => producerScript.StopCrafting());
        craftButton.onClick.AddListener(() => UpdateCraftButton());
        craftButton.transform.GetComponentInChildren<TextMeshProUGUI>().text = "STOP";
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
        itemIcon.gameObject.SetActive(false);

        titleText.text = "";

        value.text = "";

        craftTime.text = "";
        
        progressPercentText.text = "";
        craftButton.onClick.RemoveAllListeners();
    }

#endregion

#region Update Visuals
    
    private void Update_DisplayCard()
    {
        // Remove overlay
        displayCardOverlay.SetActive(false);

        // Header
        itemIcon.gameObject.SetActive(true);
        itemIcon.sprite = selectedRecipe.icon;
        itemIcon.SetNativeSize();
        titleText.text = selectedRecipe.displayName;
        
        // Info Display
        value.text = selectedRecipe.value.ToString();
        craftTime.text = selectedRecipe != null
            ? (producerScript.GetCraftTicks(selectedRecipe) / 30f).ToString("F2")
            : "0.00";

        CraftingProgressBar();
    }

    private void CraftingProgressBar()
    {
        // Reset if off or no recipe is selected
        if (!producerScript.turnedOn || selectedRecipe == null)
        {
            progressPercentText.text = "0%";
            progressBar.transform.localScale = new Vector3(0f,2.5f,1f);
            return;
        }

        // Set bar colour
        progressBarImage.sprite = redProgressBar;

        // Get time limits
        float limit = producerScript.GetCraftTicks(selectedRecipe);

        // Calculate and normalize time (0-1)
        float progress = (limit > 0f) 
            ? Mathf.Clamp01(producerScript.tickTimer / limit) 
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