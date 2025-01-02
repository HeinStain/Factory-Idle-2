using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using Unity.Collections;
using System.Data;

public class MiningDrill : Machine
{
    
    private int timeToCraft = 60;
    private int timeToVeinRespawn = 100;
    [SerializeField] private BoxCollider2D clickBox;

    private bool mining = false;

    public ItemData oreToCraft;

    public Dictionary<string, int> oreVeinCurrent = new Dictionary<string, int>();


    [Header("Machine Visuals")]

    [SerializeField] private Animator anim;
    [SerializeField] private Transform drillSprite;
    [SerializeField] private Transform itemDisplay;
    
    [Header("Menu")]
    
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject oresFolder;
    [SerializeField] private GameObject menuItemPrefab;

    private ItemData selectedOre;

    private bool menuOpen;

    [Header("Selection Menu")]
    // Frame Overlay
    [SerializeField] private GameObject noOreSelectedFrame;

    // Icon
    [SerializeField] private Image icon;

    // Title and Mastery
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI oreMasteryLevelText;
    [SerializeField] private TextMeshProUGUI oreMasteryProgressText;
    [SerializeField] private GameObject oreMasteryProgressBar;

    // Text Info Display 
    [SerializeField] private TextMeshProUGUI veinCountText;
    [SerializeField] private TextMeshProUGUI oreValueText;
    [SerializeField] private TextMeshProUGUI timeToCraftText;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private TextMeshProUGUI masteryXPText;
    [SerializeField] private TextMeshProUGUI masteryPoolXPText;

    // Progress Bar
    [SerializeField] private TextMeshProUGUI progressPercentText;
    [SerializeField] private GameObject progressBar;

    // Button
    [SerializeField] private Button craftButton;

    [Header("Total XP Bars")]
    [SerializeField] private TextMeshProUGUI miningLevelText;
    [SerializeField] private TextMeshProUGUI miningLevelProgressText;
    [SerializeField] private GameObject miningLevelProgressBar;

    [SerializeField] private TextMeshProUGUI masteryPoolPercentText;
    [SerializeField] private TextMeshProUGUI masteryPoolProgressText;
    [SerializeField] private GameObject masteryPoolProgressBar;

    [Header("Progress Bar Sprites")]
    private bool respawningVein;
    [SerializeField] private Sprite darkProgressBar;
    [SerializeField] private Sprite normalProgressBar;


#region Main

    protected override void MachineSetup()
    {
        // Set visuals
        drillSprite.eulerAngles = new Vector3(0,0,0);
        itemDisplay.eulerAngles = new Vector3(0,0,0);
        foreach (ItemData ore in ItemManager.Instance.oresList)
        {
            oreVeinCurrent.Add(ore.displayName, SkillManager.Instance.GetDictionaryIntValue(SkillManager.Instance.oreVeinMax, ore.displayName));
        }
    }

    private void Update()
    {
        //Crafting
        if (mining)   { CraftingCycle(); }
        //Visuals
        if (menuOpen) { UpdateMenuVisuals(); }
        //Machine Animation
        anim.SetBool("IsActive", mining);
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
            
            // Spawn object                                                      <      POS       >
            ItemManager.SpawnObject(ItemManager.Instance.itemPrefab, oreToCraft, new Vector3(0,0,0), Quaternion.identity);
            
            // Subtract from Vein Count
            SkillManager.Instance.ChangeDictionaryIntValue(oreVeinCurrent, oreToCraft.displayName, -1);

            // Distribute XP
            HandleXPDistribution();

            // Check for last Vein
            int currentVeinCount = SkillManager.Instance.GetDictionaryIntValue(oreVeinCurrent, oreToCraft.displayName);
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
                int maxVeinCount = SkillManager.Instance.GetDictionaryIntValue(SkillManager.Instance.oreVeinMax, selectedOre.displayName);
                SkillManager.Instance.SetDictionaryIntValue(oreVeinCurrent, selectedOre.displayName, maxVeinCount);
                // Reset
                respawningVein = false;
            }
        }
    }

    private void HandleXPDistribution()
    {
        // Add Level, Mastery Pool and Mastery XP
        SkillManager.Instance.AddXP(SkillManager.Instance.skillXP, GetMachineName(), oreToCraft.XP);
        SkillManager.Instance.AddXP(SkillManager.Instance.skillMasteryPoolXP, GetMachineName(), SkillManager.Instance.CalculateGivenMasteryPoolXP(oreToCraft.displayName));
        SkillManager.Instance.AddXP(SkillManager.Instance.oreMasteryXP, oreToCraft.displayName,  SkillManager.Instance.CalculateGivenOreMasteryXP(oreToCraft.displayName));

        // Ore Mastery Level Up
        int currentOreMasteryLevel = SkillManager.Instance.GetDictionaryIntValue(SkillManager.Instance.oreMasteryLevel, oreToCraft.displayName);
        float currentOreMasteryXP = SkillManager.Instance.GetDictionaryFloatValue(SkillManager.Instance.oreMasteryXP, oreToCraft.displayName);
        float masteryLevelXPReq = SkillManager.Instance.CalculateOreMasteryXPToNextLevel(currentOreMasteryLevel);

        if (currentOreMasteryXP >= masteryLevelXPReq)
        {
            float leftOverMasteryXP = currentOreMasteryXP - masteryLevelXPReq;
            SkillManager.Instance.SetDictionaryIntValue(SkillManager.Instance.oreMasteryLevel, oreToCraft.displayName, currentOreMasteryLevel + 1);
            SkillManager.Instance.SetDictionaryFloatValue(SkillManager.Instance.oreMasteryXP, oreToCraft.displayName, leftOverMasteryXP);
            SkillManager.Instance.SetDictionaryIntValue(SkillManager.Instance.oreVeinMax, oreToCraft.displayName, SkillManager.Instance.CalculateVeinMax(currentOreMasteryLevel +1));
            NotificationManager.Instance.DisplayNotification(oreToCraft.displayName + " Mastery Level " + SkillManager.Instance.GetDictionaryIntValue(SkillManager.Instance.oreMasteryLevel, oreToCraft.displayName).ToString() + "!");
        }

        // Skill Level Up
        int currentMiningLevel = SkillManager.Instance.GetDictionaryIntValue(SkillManager.Instance.skillLevels, GetMachineName());
        float currentMiningXP = SkillManager.Instance.GetDictionaryFloatValue(SkillManager.Instance.skillXP, GetMachineName());
        float levelXPReq = SkillManager.Instance.CalculateXPToNextLevel(currentMiningLevel);
        
        if (currentMiningXP >= levelXPReq)
        {
            float leftOverXP = currentMiningXP - levelXPReq;
            SkillManager.Instance.SetDictionaryIntValue(SkillManager.Instance.skillLevels, GetMachineName(), currentMiningLevel + 1);
            SkillManager.Instance.SetDictionaryFloatValue(SkillManager.Instance.skillXP, GetMachineName(), leftOverXP);
            NotificationManager.Instance.DisplayNotification(GetMachineName() + " Level " + SkillManager.Instance.GetDictionaryIntValue(SkillManager.Instance.skillLevels, GetMachineName()).ToString() + "!");
        }
    }

#endregion

#region Menu Function

    private void GenerateItemsList()
    {
        if (oresFolder.transform.childCount > 0)
        {
            foreach (Transform child in oresFolder.transform)
            {
                Destroy(child.gameObject);
            }
        }
        
        List<ItemData> sortedOres = ItemManager.Instance.oresList.OrderBy (ore => ore.value).ToList();
        
        foreach (ItemData ore in sortedOres)
        {
            if (ore.tags.Contains(ItemTag.Ore))
            {
                GameObject oreBlock = Instantiate(menuItemPrefab, oresFolder.transform);
                
                oreBlock.GetComponent<OreSelectBlock>().SetOre(ore);
                oreBlock.GetComponent<OreSelectBlock>().UpdateBlock(this);
                
            }
        }
    }

    public void SelectOre(ItemData ore)
    {
        if (ItemManager.Instance.oresList.Contains(ore))
        {
            selectedOre = ore;
            mining = false;
            respawningVein = false;
        }
    }

    public void StartButton(string name)
    {
        // TODO
        // Check for output belt
        // If it exists
            oreToCraft = selectedOre;
            tickTimer = 0;
            mining = true;
        // else
            // error
            // break
    }

#endregion

#region Open / Close Menu

    // Called from MouseManager
    public void OpenMenu()
    {
        menuOpen = true;
        menuCanvas.SetActive(true);
        GameManager.Instance.SetMenuOpen(true);
        GenerateItemsList();
    }

    // Called from close button
    public void CloseMenu()
    {
        menuOpen = false;
        menuCanvas.SetActive(false);
        GameManager.Instance.SetMenuOpen(false);
    }

#endregion

#region Menu Visuals

    public void UpdateMenuVisuals()
    {
        if (selectedOre != null)
        {
            noOreSelectedFrame.SetActive(false);

            int oreMasteryLevel = SkillManager.Instance.GetDictionaryIntValue(SkillManager.Instance.oreMasteryLevel, selectedOre.displayName);
            
            icon.gameObject.SetActive(true);
            icon.sprite = selectedOre.icon;
            icon.SetNativeSize();

            titleText.text = selectedOre.displayName;
            oreMasteryLevelText.text = oreMasteryLevel.ToString();
            
            veinCountText.text = SkillManager.Instance.GetDictionaryIntValue(oreVeinCurrent, selectedOre.displayName) + "/" + SkillManager.Instance.GetDictionaryIntValue(SkillManager.Instance.oreVeinMax, selectedOre.displayName);
            oreValueText.text = selectedOre.value.ToString();

            timeToCraftText.text = (timeToCraft / 20).ToString("F2");
            xpText.text = selectedOre.XP.ToString();
            masteryXPText.text = ((int)(SkillManager.Instance.CalculateGivenOreMasteryXP(selectedOre.displayName))).ToString();
            masteryPoolXPText.text = ((int)(SkillManager.Instance.CalculateGivenMasteryPoolXP(selectedOre.displayName))).ToString();
            
            // Ore Mastery
            float currentMasteryXP = SkillManager.Instance.GetDictionaryFloatValue(SkillManager.Instance.oreMasteryXP, selectedOre.displayName);
            
            float p1 = Mathf.Clamp01(currentMasteryXP / SkillManager.Instance.CalculateOreMasteryXPToNextLevel(oreMasteryLevel));
            oreMasteryProgressBar.transform.localScale = Vector2.Lerp(new Vector2(0,1f), new Vector2(1,1f), p1);
            oreMasteryProgressText.text = (int)currentMasteryXP + " / " + SkillManager.Instance.CalculateOreMasteryXPToNextLevel(oreMasteryLevel).ToString();

            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(() => StartButton(selectedOre.displayName));

            OreSelectBlock[] oreBlocks = FindObjectsOfType<OreSelectBlock>();
            foreach (OreSelectBlock oreBlock in oreBlocks)
            {
                oreBlock.UpdateBlock(this);
            }
        }
        else
        {
            noOreSelectedFrame.SetActive(true);
            icon.gameObject.SetActive(false);

            titleText.text = "";
            oreMasteryLevelText.text = "";
            oreMasteryProgressText.text = "";

            veinCountText.text = "";
            oreValueText.text = "";

            timeToCraftText.text = "";
            xpText.text = "";
            masteryXPText.text = "";
            masteryPoolXPText.text = "";

            oreMasteryProgressBar.transform.localScale = new Vector2(0f,1f);
            oreMasteryProgressText.text = "0 / 0";

            progressPercentText.text = "";
            craftButton.onClick.RemoveAllListeners();
        }

        // Mining Level and XP
        miningLevelText.text = "Level " + SkillManager.Instance.GetDictionaryIntValue(SkillManager.Instance.skillLevels, GetMachineName());

        int currentMiningLevel = SkillManager.Instance.GetDictionaryIntValue(SkillManager.Instance.skillLevels, GetMachineName());
        float currentMiningXP = SkillManager.Instance.GetDictionaryFloatValue(SkillManager.Instance.skillXP, GetMachineName());
        
        float p2 = Mathf.Clamp01(currentMiningXP / SkillManager.Instance.CalculateXPToNextLevel(currentMiningLevel));
        miningLevelProgressBar.transform.localScale = Vector2.Lerp(new Vector2(0,1f), new Vector2(1,1f), p2);
        miningLevelProgressText.text  =  (int)currentMiningXP + " / " + SkillManager.Instance.CalculateXPToNextLevel(currentMiningLevel).ToString();

        // Mastery Pool 
        float currentMasteryPoolXP = SkillManager.Instance.GetDictionaryFloatValue(SkillManager.Instance.skillMasteryPoolXP, GetMachineName());
       
        float p3 = Mathf.Clamp01(currentMasteryPoolXP / 11500000f);
        masteryPoolProgressBar.transform.localScale = Vector2.Lerp(new Vector2(0,1f), new Vector2(1,1f), p3);
        masteryPoolPercentText.text = "Mastery - " + ((currentMasteryPoolXP / 11500000f) * 100f).ToString("F2") + "%";
        masteryPoolProgressText.text  =  (int)currentMasteryPoolXP + " / " + "11,500,000";

        // Progress Bar
        if (mining)
        {
            if (respawningVein)
            {
                progressBar.GetComponent<Image>().sprite = darkProgressBar;
                progressPercentText.text = ((float)tickTimer / (float)timeToVeinRespawn * 100).ToString("F0") + "%";

                float t = Mathf.Clamp01((float)tickTimer / (float)timeToVeinRespawn);
                progressBar.transform.localScale = new Vector2(t,1f);
                
            }
            else
            {
                progressBar.GetComponent<Image>().sprite = normalProgressBar;
                progressPercentText.text = (((float)tickTimer / (float)timeToCraft) * 100).ToString("F0") + "%";

                float t = Mathf.Clamp01((float)tickTimer / (float)timeToCraft);
                progressBar.transform.localScale = new Vector2(t,1f);
            }
        }
        else
        {
            progressPercentText.text = "0%";
            progressBar.transform.localScale = new Vector2(0f,2.5f);
        }
    }

#endregion





}
