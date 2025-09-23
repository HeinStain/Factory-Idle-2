using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System.Data;
using System.Collections.Generic;


public class ProducerVisuals : MonoBehaviour
{


    private ItemData selectedRecipe;
    private bool menuOpen = false;



    [SerializeField] private GameObject itemsFolder;
    [SerializeField] private GameObject menuItemPrefab;
    

    [Header("Frame Overlay")]
    [SerializeField] private GameObject noItemSelectedFrame;

    [Header("Icon")]
    [SerializeField] private Image itemIcon;

    [Header("Title and Mastery")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI itemMasteryLevelText;
    [SerializeField] private TextMeshProUGUI itemMasteryProgressText;
    [SerializeField] private GameObject itemMasteryProgressBar;

    [Header("Text Info Display")]
    [SerializeField] private TextMeshProUGUI veinCountText;
    [SerializeField] private TextMeshProUGUI itemValueText;
    [SerializeField] private TextMeshProUGUI timeToCraftText;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private TextMeshProUGUI masteryXPText;
    [SerializeField] private TextMeshProUGUI masteryPoolXPText;

    [Header("Progress Bar")]
    [SerializeField] private TextMeshProUGUI progressPercentText;
    [SerializeField] private GameObject progressBar;

    [Header("Button")]
    [SerializeField] private Button craftButton;

    [Header("Total XP Bars")]
    [SerializeField] private TextMeshProUGUI skillLevelText;
    [SerializeField] private TextMeshProUGUI skillLevelProgressText;
    [SerializeField] private GameObject skillLevelProgressBar;

    [SerializeField] private TextMeshProUGUI masteryPoolPercentText;
    [SerializeField] private TextMeshProUGUI masteryPoolProgressText;
    [SerializeField] private GameObject masteryPoolProgressBar;

    
   
    private bool placed;
    private Dictionary<string, ItemSelectBlock> itemBlocks = new Dictionary<string, ItemSelectBlock>();


    [Header("Casts")]
    public Machine_Producer producer;
    [SerializeField] private Animator anim;
    
    
    [Header("Progress Bar Sprites")]
    [SerializeField] private Sprite whiteProgressBar;
    [SerializeField] private Sprite redProgressBar;
    [SerializeField] private Sprite greenProgressBar;

    [Header("Machine Objects")]
    [SerializeField] private GameObject machineProgressBar;
    [SerializeField] List<Transform> objectsToKeepStraight;


#region Setup
   private void Awake()
    {
        placed = false;

        GameManager.Instance.OnMenuTriggered += HandleMenuTriggered;
        BuildManager.Instance.OnMachinePlaced += HandlePlacement;
    }

    private void OnDestroy() 
    {
        GameManager.Instance.OnMenuTriggered -= HandleMenuTriggered;
        BuildManager.Instance.OnMachinePlaced -= HandlePlacement;
    }

    private void HandlePlacement(Machine producerType)
    {
        if (!placed)
        {
            if (transform.parent != null && transform.parent.GetComponent<Machine>() != null)
            {
                producer = producerType as Machine_Producer;
                anim = transform.parent.GetComponent<Animator>();
                placed = true;
            }
        }
    }

    private void HandleMenuTriggered(bool status)
    {   
        menuOpen = status;
        if (producer != null && menuOpen)
        {
            GenerateItemsList();
        }
    }

#endregion 


#region Update

    private void Update()
    {
        foreach (Transform obj in objectsToKeepStraight)
        {
            obj.eulerAngles = new Vector3(0,0,0);
        }
        
        if (placed)
        {
            if (menuOpen) { UpdateMenuVisuals(); }     // Visuals
            anim.SetBool("IsActive", producer.turnedOn);      // producer Animation
        }

        // Progress Bar
        if (producer != null)
        {
            if (producer.turnedOn)
            {
                if (producer.respawningReservoir)
                {
                    machineProgressBar.GetComponent<SpriteRenderer>().sprite = whiteProgressBar;

                    float t = Mathf.Clamp01((float)producer.tickTimer / (float)producer.timeToReservoirRespawn);
                    machineProgressBar.transform.localScale = new Vector3(t,1f,1f);
                }
                else
                {
                    machineProgressBar.GetComponent<SpriteRenderer>().sprite = greenProgressBar;
    
                    float t = Mathf.Clamp01((float)producer.tickTimer / (float)producer.GetMachineData().skill.defaultCraftingInterval);
                    machineProgressBar.transform.localScale = new Vector3(t,1f,1f);
                }
            }
            else
            {
                machineProgressBar.transform.localScale = new Vector3(0f,1f,1f);
            }
        }
        
    }

#endregion


#region MenuFunction


    private void GenerateItemsList()
    {
        // Clear existing items
        foreach (Transform child in itemsFolder.transform) { Destroy(child.gameObject); }

        itemBlocks.Clear();

        // Filter and sort items
        var sortedItems = ItemManager.Instance.itemList
            .Where(item => item.tags.Contains(producer.SkillTag))
            .OrderBy(item => item.unlockLevel)
            .ToList();

        // Create new items
        foreach (ItemData item in sortedItems)
        {
            GameObject itemBlock = Instantiate(menuItemPrefab, itemsFolder.transform);

            ItemSelectBlock itemMenuBlock = itemBlock.GetComponent<ItemSelectBlock>();
            
            itemMenuBlock.UpdateBlock(this, item);
            
            // Cache the block
            itemBlocks[item.displayName] = itemMenuBlock;
        }
    }




    public void SelectItem(ItemData item)
    {
        if (item == null)
        {
            Debug.LogError("Attempting to select null item");
            return;
        }

        Debug.Log($"Selecting item: {item.displayName} for {producer.GetMachineData().skill.skillName}");
        
        if (ItemManager.Instance.itemList.Contains(item))
        {
            if (item != selectedRecipe)
            {
                selectedRecipe = item;
                producer.selectedRecipe = item;
                producer.respawningReservoir = false;
                producer.turnedOn = false;

                UpdateSelectedItemVisuals(item);
                SetCraftButton();
            }
        }
    }

    private void UpdateSelectedItemVisuals(ItemData selectedItem)
    {
        // Update only the previously selected and newly selected items
        foreach (var blockPair in itemBlocks)
        {
            ItemSelectBlock block = blockPair.Value;
            if (block.item == selectedItem || block.item == selectedRecipe)
            {
                block.UpdateVisuals(); // New method for visual-only updates
            }
        }
    }

    public void ButtonSwitch()
    {
        if (producer.turnedOn)
        {
            SetStopButton();
        }
        else
        {
            SetCraftButton();
        }
    }

    private void SetCraftButton()
    {
        craftButton.onClick.RemoveAllListeners();
        craftButton.onClick.AddListener(() => producer.StartCrafting());
        craftButton.onClick.AddListener(() => ButtonSwitch());
        craftButton.transform.GetComponentInChildren<TextMeshProUGUI>().text = "CRAFT";
    }

    private void SetStopButton()
    {
        craftButton.onClick.RemoveAllListeners();
        craftButton.onClick.AddListener(() => producer.StopCrafting());
        craftButton.onClick.AddListener(() => ButtonSwitch());
        craftButton.transform.GetComponentInChildren<TextMeshProUGUI>().text = "STOP";
    }

#endregion
/// <summary>
/// ////////
/// ///
/// 
/// </summary>
/// 

///
///
///
///

///
#region Menu Visuals

    public void UpdateMenuVisuals()
    {
        if (selectedRecipe != null)
        {
            // First verify this item belongs to this machine's skill
            if (!selectedRecipe.tags.Contains(producer.SkillTag))
            {
                Debug.LogError($"Selected recipe {selectedRecipe.displayName} doesn't match machine skill {producer.SkillTag}");
                selectedRecipe = null;
                return;
            }

            noItemSelectedFrame.SetActive(false);

            // Cache dictionary lookups to avoid multiple calls
            string itemName = selectedRecipe.displayName;
            string skill = producer.GetMachineData().skill.skillName;

            // Verify all required dictionary entries exist
            if (!SkillManager.Instance.itemMasteryLevels.ContainsKey(itemName) ||
                !SkillManager.Instance.itemMasteryXPCount.ContainsKey(itemName) ||
                !producer.reservoirNodeCounts.ContainsKey(itemName) ||
                !producer.reservoirMaxes.ContainsKey(itemName))
            {
                Debug.LogError($"Missing dictionary entries for {itemName} in {skill}");
                return;
            }

            int itemMasteryLevel = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.itemMasteryLevels, selectedRecipe.displayName);
            
            // Update UI elements
            itemIcon.gameObject.SetActive(true);
            itemIcon.sprite = selectedRecipe.icon;
            itemIcon.SetNativeSize();

            titleText.text = itemName;
            itemMasteryLevelText.text = itemMasteryLevel.ToString();
            
            veinCountText.text = SkillManager.Instance.GetDictionaryValue(producer.reservoirNodeCounts, itemName) + "/" + SkillManager.Instance.GetDictionaryValue(producer.reservoirMaxes, itemName);
            itemValueText.text = selectedRecipe.value.ToString();
            timeToCraftText.text = (producer.GetMachineData().skill.defaultCraftingInterval / 30).ToString("F2");
            xpText.text = selectedRecipe.XP.ToString();
            
            // Only calculate mastery XP if all required data exists
            if (selectedRecipe.tags.Contains(producer.SkillTag))
            {
                int givenMasteryXP = (int)SkillManager.Instance.Calculate_GivenItemMasteryXP(itemName, producer.SkillTag);
                masteryXPText.text = givenMasteryXP.ToString();
                masteryPoolXPText.text = ((int)(givenMasteryXP / 4)).ToString();
            }

            // Update mastery progress
            float currentMasteryXP = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.itemMasteryXPCount, itemName);
            float nextLevelXP = SkillManager.Instance.Calculate_XPToNextLevel(itemMasteryLevel);
            
            float p1 = Mathf.Clamp01(currentMasteryXP / nextLevelXP);
           // Debug.Log($"Progress Bar Scale: {p1} for {itemName} - Raw values: {currentMasteryXP}/{nextLevelXP}");
            itemMasteryProgressBar.transform.localScale = Vector2.Lerp(new Vector2(0,1f), new Vector2(1,1f), p1);
            itemMasteryProgressText.text = $"{(int)currentMasteryXP} / {nextLevelXP}";
           // Debug.Log($"Progress Bar Object: {itemMasteryProgressBar.name}, Current Scale: {itemMasteryProgressBar.transform.localScale}");

            // Only update blocks that belong to this machine
            foreach (var block in itemBlocks.Values)
            {
                if (block.item.tags.Contains(producer.SkillTag))
                {
                    block.UpdateBlock(this, block.item);
                }
            }
        }
        else
        {
            noItemSelectedFrame.SetActive(true);
            itemIcon.gameObject.SetActive(false);

            titleText.text = "";
            itemMasteryLevelText.text = "";
            itemMasteryProgressText.text = "";

            veinCountText.text = "";
            itemValueText.text = "";

            timeToCraftText.text = "";
            xpText.text = "";
            masteryXPText.text = "";
            masteryPoolXPText.text = "";

            itemMasteryProgressBar.transform.localScale = new Vector2(0f,1f);
            itemMasteryProgressText.text = "0 / 0";

            progressPercentText.text = "";
            craftButton.onClick.RemoveAllListeners();
        }

        UpdateSkillAndMasteryInfo();
        
    }


    private void UpdateSkillAndMasteryInfo()
    {
        // Skill Level and XP
        skillLevelText.text = "Level " + SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.skillLevels, producer.GetMachineData().skill.skillName);

        int currentSkillLevel = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.skillLevels, producer.GetMachineData().skill.skillName);
        float currentSkillXP = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.skillXP, producer.GetMachineData().skill.skillName);
        
        float p2 = Mathf.Clamp01(currentSkillXP / SkillManager.Instance.Calculate_XPToNextLevel(currentSkillLevel));
        skillLevelProgressBar.transform.localScale = Vector2.Lerp(new Vector2(0,1f), new Vector2(1,1f), p2);
        skillLevelProgressText.text  =  (int)currentSkillXP + " / " + SkillManager.Instance.Calculate_XPToNextLevel(currentSkillLevel).ToString();

        // Mastery Pool 
        float currentMasteryPoolXP = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.skillMasteryPoolXP, producer.GetMachineData().skill.skillName);
       
        float p3 = Mathf.Clamp01(currentMasteryPoolXP / 11500000f);
        masteryPoolProgressBar.transform.localScale = Vector2.Lerp(new Vector2(0,1f), new Vector2(1,1f), p3);
        masteryPoolPercentText.text = "Mastery - " + ((currentMasteryPoolXP / producer.GetMachineData().skill.masteryPoolSize) * 100f).ToString("F2") + "%";
        masteryPoolProgressText.text  =  (int)currentMasteryPoolXP + " / " + producer.GetMachineData().skill.masteryPoolSize;

        // Progress Bar
        if (producer.turnedOn)
        {
            if (producer.respawningReservoir)
            {
                progressBar.GetComponent<Image>().sprite = whiteProgressBar;
                progressPercentText.text = ((float)producer.tickTimer / (float)producer.timeToReservoirRespawn * 100).ToString("F0") + "%";

                float t = Mathf.Clamp01((float)producer.tickTimer / (float)producer.timeToReservoirRespawn);
                progressBar.transform.localScale = new Vector3(t,1f,1f);
                
            }
            else
            {
                progressBar.GetComponent<Image>().sprite = redProgressBar;
                progressPercentText.text = (((float)producer.tickTimer / (float)producer.GetMachineData().skill.defaultCraftingInterval) * 100).ToString("F0") + "%";

                float t = Mathf.Clamp01((float)producer.tickTimer / (float)producer.GetMachineData().skill.defaultCraftingInterval);
                progressBar.transform.localScale = new Vector3(t,1f,1f);
            }
        }
        else
        {
            progressPercentText.text = "0%";
            progressBar.transform.localScale = new Vector3(0f,2.5f,1f);
        }
    }

    

#endregion

}