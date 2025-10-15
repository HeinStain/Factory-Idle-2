using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System.Data;
using System.Collections.Generic;


public class ProducerVisuals : MonoBehaviour
{

    [Header("Function")]
        private Animator anim;
        private ItemData selectedRecipe;
        private Machine_Producer producerScript;
        private SkillManager skillManager;
        private MachineData machineData;
        private string skillName;
        private bool menuOpen = false;
        private bool placed = false;
        private Dictionary<ItemData, ProducerRecipeBlock> recipeBlocks = new();


    [Header("Machine Parts")]
        [SerializeField] private GameObject machineProgressBar;
        [SerializeField] private SpriteRenderer machineProgressBarSR;
        [SerializeField] List<Transform> objectsToKeepStraight;


    [Header("Resources")]
        [SerializeField] private GameObject recipeFolder;
        [SerializeField] private GameObject producerRecipeCard;
        [SerializeField] private Sprite whiteProgressBar, redProgressBar, greenProgressBar;


    [Header("XP Bars")]
        [SerializeField] private GameObject skillLevelProgressBar;
        [SerializeField] private GameObject masteryPoolProgressBar;
        [SerializeField] private TextMeshProUGUI skillLevelText, skillLevelProgressText, masteryPoolPercentText, masteryPoolProgressText;
        

    [Header("Display Card - Header")]
        [SerializeField] private GameObject displayCardOverlay;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI titleText, itemMasteryLevelText, itemMasteryProgressText;
        [SerializeField] private GameObject itemMasteryProgressBar;

    [Header("Display Card - Info")]
        [SerializeField] private Button craftButton;
        [SerializeField] private TextMeshProUGUI veinCount, value, craftTime, xp, masteryXP, masteryPoolXP;

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
        skillManager = SkillManager.Instance;
        skillName = machineData.skill.skillName;
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

    public Machine_Producer GetProducerScript() => producerScript;

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
            if (producerScript != null) { MachineProgressBarVisuals(); } 

            if (menuOpen) 
            { 
                if (HasValidSelection()) 
                {
                    Update_DisplayCard();
                    Update_RecipeCard();
                    Update_SkillAndMasteryBar();
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
        if (!producerScript.turnedOn) { machineProgressBar.transform.localScale = new Vector3(0f,1f,1f); return; }
        
        // Otherwise, show Display / Respawn Bar
        if (producerScript.respawningReservoir) 
        {   
            machineProgressBarSR.sprite = whiteProgressBar;
            float t = Mathf.Clamp01((float)producerScript.tickTimer / (float)producerScript.timeToReservoirRespawn);
            machineProgressBar.transform.localScale = new Vector3(t,1f,1f);
        } else { 
            machineProgressBarSR.sprite = greenProgressBar;
            float t = Mathf.Clamp01((float)producerScript.tickTimer / (float)machineData.skill.defaultCraftingInterval);
            machineProgressBar.transform.localScale = new Vector3(t,1f,1f);
        } 
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
            .Where(item => item.tags.Contains(producerScript.SkillTag)) // Filter to relevant items
            .OrderBy(item => item.unlockLevel)                          // Order by unlock level
            .ToList();                                                  // Turn it into a list

        // Create recipe blocks from list 
        foreach (ItemData itemData in sortedItems)
        {
            GameObject recipeBlockObj = Instantiate(producerRecipeCard, recipeFolder.transform);
            ProducerRecipeBlock recipeBlockScript = recipeBlockObj.GetComponent<ProducerRecipeBlock>();
            recipeBlockScript.UpdateBlock(this, itemData);
            recipeBlocks[itemData] = recipeBlockScript;
        }
    }

    public void SelectItem(ItemData item)
    {
        // Safety checks
        if (item == null) { Debug.LogError("Attempting to select null item"); return; }
        if (!ItemManager.Instance.itemList.Contains(item)) { Debug.LogError("Selected Item not in ItemManager.itemList"); return; }
        if (item == selectedRecipe) { Debug.Log("Item already selected"); return; }

        // Print selection
        Debug.Log($"Selecting item: {item.displayName} in {machineData.skill.skillName}");
        
        // Turn off machine production
        producerScript.respawningReservoir = false;
        producerScript.turnedOn = false;

        // Set new selection
        selectedRecipe = item;
        producerScript.selectedRecipe = item;

        // Update only the selected items
        foreach (var kvp in recipeBlocks)
        {
            ProducerRecipeBlock itemBlock = kvp.Value;
            if (itemBlock.itemData == selectedRecipe) { itemBlock.UpdateBlockVisuals(); }
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
        if (!selectedRecipe.tags.Contains(producerScript.SkillTag))
        {
            Debug.LogError($"Selected recipe {selectedRecipe.displayName} doesn't match machine skill {producerScript.SkillTag}");
            selectedRecipe = null;
            return false;
        }

        // Verify all required dictionary entries exist
        if (!skillManager.itemMasteryLevels.ContainsKey(selectedRecipe.displayName) ||
            !skillManager.itemMasteryXPCount.ContainsKey(selectedRecipe.displayName) ||
            !producerScript.reservoirNodeCounts.ContainsKey(selectedRecipe.displayName) ||
            !producerScript.reservoirMaxes.ContainsKey(selectedRecipe.displayName))
        {
            Debug.LogError($"Missing dictionary entries for {selectedRecipe.displayName} in {machineData.skill.skillName}");
            return false;
        }

        return true;
    }

    private void ResetDisplayCard()
    {
        displayCardOverlay.SetActive(true);
        itemIcon.gameObject.SetActive(false);

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
        itemMasteryLevelText.text = skillManager.GetDictionaryValue(skillManager.itemMasteryLevels, selectedRecipe.displayName).ToString();

        // Calculate and Update mastery progress
        float currentMasteryXP = skillManager.GetDictionaryValue(skillManager.itemMasteryXPCount, selectedRecipe.displayName);
        float nextLevelXP = skillManager.Calculate_XPToNextLevel(skillManager.GetDictionaryValue(skillManager.itemMasteryLevels, selectedRecipe.displayName));
        float progress = Mathf.Clamp01(currentMasteryXP / nextLevelXP);
        itemMasteryProgressBar.transform.localScale = Vector2.Lerp(new Vector2(0,1f), new Vector2(1,1f), progress);
        itemMasteryProgressText.text = $"{(int)currentMasteryXP} / {nextLevelXP}";
        
        // Info Display
        veinCount.text = skillManager.GetDictionaryValue(producerScript.reservoirNodeCounts, selectedRecipe.displayName) + "/" + skillManager.GetDictionaryValue(producerScript.reservoirMaxes, selectedRecipe.displayName);
        value.text = selectedRecipe.value.ToString();
        craftTime.text = (machineData.skill.defaultCraftingInterval / 30).ToString("F2");
        xp.text = selectedRecipe.XP.ToString();
        int givenMasteryXP = (int)skillManager.Calculate_GivenItemMasteryXP(selectedRecipe.displayName, producerScript.SkillTag);
        masteryXP.text = givenMasteryXP.ToString();
        masteryPoolXP.text = ((int)(givenMasteryXP / 4)).ToString();

        CraftingProgressBar();
    }

    private void CraftingProgressBar()
    {
        // Reset if off
        if (!producerScript.turnedOn)
        {
            progressPercentText.text = "0%";
            progressBar.transform.localScale = new Vector3(0f,2.5f,1f);
            return;
        }

        // Set bar colour 
        progressBarImage.sprite = producerScript.respawningReservoir 
            ? whiteProgressBar 
            : redProgressBar;

        // Get time limits
        float limit = producerScript.respawningReservoir 
            ? producerScript.timeToReservoirRespawn 
            : machineData.skill.defaultCraftingInterval;

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

    private void Update_SkillAndMasteryBar()
    {
        // Skill Level and XP
        skillLevelText.text = "Level " + skillManager.GetDictionaryValue(skillManager.skillLevels, machineData.skill.skillName);

        int currentSkillLevel = skillManager.GetDictionaryValue(skillManager.skillLevels, machineData.skill.skillName);
        float currentSkillXP = skillManager.GetDictionaryValue(skillManager.skillXP, machineData.skill.skillName);
        
        float t1 = Mathf.Clamp01(currentSkillXP / skillManager.Calculate_XPToNextLevel(currentSkillLevel));
        skillLevelProgressBar.transform.localScale = Vector2.Lerp(new Vector2(0,1f), new Vector2(1,1f), t1);
        skillLevelProgressText.text  =  (int)currentSkillXP + " / " + skillManager.Calculate_XPToNextLevel(currentSkillLevel).ToString();

        // Mastery Pool 
        float currentMasteryPoolXP = skillManager.GetDictionaryValue(skillManager.skillMasteryPoolXP, machineData.skill.skillName);
       
        float t2 = Mathf.Clamp01(currentMasteryPoolXP / machineData.skill.masteryPoolSize);
        masteryPoolProgressBar.transform.localScale = Vector2.Lerp(new Vector2(0,1f), new Vector2(1,1f), t2);
        masteryPoolPercentText.text = "Mastery - " + ((currentMasteryPoolXP / machineData.skill.masteryPoolSize) * 100f).ToString("F2") + "%";
        masteryPoolProgressText.text  =  (int)currentMasteryPoolXP + " / " + machineData.skill.masteryPoolSize;
    }



#endregion

}