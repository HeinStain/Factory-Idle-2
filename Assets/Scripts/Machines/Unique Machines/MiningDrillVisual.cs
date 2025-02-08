using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System.Data;

public class MiningDrillVisual : MonoBehaviour
{
    private ItemData selectedOre;
    private bool menuOpen;


    [Header("Machine Visuals")]
    [SerializeField] private Transform drillSprite;
    [SerializeField] private Transform itemDisplay;
    [SerializeField] private GameObject progressBarDisplay;
    
    [SerializeField] private GameObject oresFolder;
    [SerializeField] private GameObject menuItemPrefab;

    [Header("Frame Overlay")]
    [SerializeField] private GameObject noOreSelectedFrame;

    [Header("Icon")]
    [SerializeField] private Image icon;

    [Header("Title and Mastery")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI oreMasteryLevelText;
    [SerializeField] private TextMeshProUGUI oreMasteryProgressText;
    [SerializeField] private GameObject oreMasteryProgressBar;

    [Header("Text Info Display")]
    [SerializeField] private TextMeshProUGUI veinCountText;
    [SerializeField] private TextMeshProUGUI oreValueText;
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
    [SerializeField] private TextMeshProUGUI miningLevelText;
    [SerializeField] private TextMeshProUGUI miningLevelProgressText;
    [SerializeField] private GameObject miningLevelProgressBar;

    [SerializeField] private TextMeshProUGUI masteryPoolPercentText;
    [SerializeField] private TextMeshProUGUI masteryPoolProgressText;
    [SerializeField] private GameObject masteryPoolProgressBar;

    [Header("Progress Bar Sprites")]
    [SerializeField] private Sprite whiteProgressBar;
    [SerializeField] private Sprite redProgressBar;
    [SerializeField] private Sprite greenProgressBar;

    [SerializeField] public MiningDrill miningDrill;

    private bool placed;
    [SerializeField] private Animator anim;
    
    private void Awake()
    {
        placed = false;

        // Subscribe to Menu Open Event
        GameManager.Instance.OnMenuTriggered += HandleMenuTriggered;
        BuildManager.Instance.OnMachinePlaced += HandlePlacement;
        
    }

    private void OnDestroy() 
    {
        GameManager.Instance.OnMenuTriggered -= HandleMenuTriggered;
        BuildManager.Instance.OnMachinePlaced -= HandlePlacement;
    }

    private void HandlePlacement(Machine machine)
    {
        if (!placed)
        {
            MiningDrill m = machine as MiningDrill;
            
            if (transform.parent != null)
            {
                if (transform.parent.GetComponent<MiningDrill>() != null)
                {
                    miningDrill = m;
                    anim = transform.parent.GetComponent<Animator>();
                    placed = true;
                }
            }
        }
        
    }

    private void HandleMenuTriggered(bool status)
    {   
        menuOpen = status;
        if (miningDrill != null && menuOpen)
        {
            GenerateItemsList();
        }
    }

    private void Update()
    {

        drillSprite.eulerAngles = new Vector3(0,0,0);
        itemDisplay.eulerAngles = new Vector3(0,0,0);
        
        if (placed)
        {
            if (menuOpen) { UpdateMenuVisuals(); }     // Visuals
            anim.SetBool("IsActive", miningDrill.mining);      // Machine Animation
        }

        // Progress Bar
        if (miningDrill != null)
        {
            if (miningDrill.mining)
            {
                if (miningDrill.respawningVein)
                {
                    progressBarDisplay.GetComponent<SpriteRenderer>().sprite = whiteProgressBar;

                    float t = Mathf.Clamp01((float)miningDrill.tickTimer / (float)miningDrill.timeToVeinRespawn);
                    progressBarDisplay.transform.localScale = new Vector2(t,1f);
                    
                }
                else
                {
                    progressBarDisplay.GetComponent<SpriteRenderer>().sprite = greenProgressBar;
    
                    float t = Mathf.Clamp01((float)miningDrill.tickTimer / (float)miningDrill.timeToCraft);
                    progressBarDisplay.transform.localScale = new Vector2(t,1f);
                }
            }
            else
            {
                progressBarDisplay.transform.localScale = new Vector2(0f,1f);
            }
        }
        
    }


#region MenuFunction

    private void GenerateItemsList()
    {
        // Clear Item List
        foreach (Transform child in oresFolder.transform) { Destroy(child.gameObject); }
    
        // Filter items with "Ore" tag, and order by value
        var sortedOres = ItemManager.Instance.itemList.Where(ore => ore.tags.Contains(ItemTag.Ore)).OrderBy(ore => ore.value).ToList();

        // Instantiate and set ores
        foreach (ItemData ore in sortedOres)
        {
            GameObject oreBlock = Instantiate(menuItemPrefab, oresFolder.transform);

            // Get the OreSelectBlock component once for reuse
            OreSelectBlock oreMenuBlock = oreBlock.GetComponent<OreSelectBlock>();

            oreMenuBlock.SetOre(ore);
            oreMenuBlock.UpdateBlock(this);
        }
    }

    public void SelectOre(ItemData ore)
    {
        if (ItemManager.Instance.itemList.Contains(ore))
        {
            selectedOre = ore;
            miningDrill.selectedOre = ore;
            miningDrill.mining = false;
            miningDrill.respawningVein = false;

            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(() => miningDrill.StartCrafting());
            craftButton.onClick.AddListener(() => ButtonSwitch());
            craftButton.transform.GetComponentInChildren<TextMeshProUGUI>().text = "CRAFT";
        }
    }

    public void ButtonSwitch()
    {
        // Check for output belt
        if (miningDrill.mining)
        {
            //Change to stop button
            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(() => miningDrill.StopCrafting());
            craftButton.onClick.AddListener(() => ButtonSwitch());
            craftButton.transform.GetComponentInChildren<TextMeshProUGUI>().text = "STOP";
        }
        else
        {
            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(() => miningDrill.StartCrafting());
            craftButton.onClick.AddListener(() => ButtonSwitch());
            craftButton.transform.GetComponentInChildren<TextMeshProUGUI>().text = "CRAFT";
        }
    }

#endregion
    
#region Menu Visuals

    public void UpdateMenuVisuals()
    {
        if (selectedOre != null)
        {
            noOreSelectedFrame.SetActive(false);

            int oreMasteryLevel = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.oreMasteryLevel, selectedOre.displayName);
            
            icon.gameObject.SetActive(true);
            icon.sprite = selectedOre.icon;
            icon.SetNativeSize();

            titleText.text = selectedOre.displayName;
            oreMasteryLevelText.text = oreMasteryLevel.ToString();
            
            veinCountText.text = SkillManager.Instance.GetDictionaryValue(miningDrill.oreVeinCounts, selectedOre.displayName) + "/" + SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.oreVeinMax, selectedOre.displayName);
            oreValueText.text = selectedOre.value.ToString();

            timeToCraftText.text = (miningDrill.timeToCraft / 30).ToString("F2");
            xpText.text = selectedOre.XP.ToString();
            masteryXPText.text = ((int)(SkillManager.Instance.CalculateGivenOreMasteryXP(selectedOre.displayName))).ToString();
            masteryPoolXPText.text = ((int)(SkillManager.Instance.CalculateGivenMasteryPoolXP(selectedOre.displayName))).ToString();
            

            // Ore Mastery
            float currentMasteryXP = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.oreMasteryXP, selectedOre.displayName);
            
            float p1 = Mathf.Clamp01(currentMasteryXP / SkillManager.Instance.CalculateOreMasteryXPToNextLevel(oreMasteryLevel));
            oreMasteryProgressBar.transform.localScale = Vector2.Lerp(new Vector2(0,1f), new Vector2(1,1f), p1);
            oreMasteryProgressText.text = (int)currentMasteryXP + " / " + SkillManager.Instance.CalculateOreMasteryXPToNextLevel(oreMasteryLevel).ToString();

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
        miningLevelText.text = "Level " + SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.skillLevels, miningDrill.GetMachineName());

        int currentMiningLevel = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.skillLevels, miningDrill.GetMachineName());
        float currentMiningXP = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.skillXP, miningDrill.GetMachineName());
        
        float p2 = Mathf.Clamp01(currentMiningXP / SkillManager.Instance.CalculateXPToNextLevel(currentMiningLevel));
        miningLevelProgressBar.transform.localScale = Vector2.Lerp(new Vector2(0,1f), new Vector2(1,1f), p2);
        miningLevelProgressText.text  =  (int)currentMiningXP + " / " + SkillManager.Instance.CalculateXPToNextLevel(currentMiningLevel).ToString();

        // Mastery Pool 
        float currentMasteryPoolXP = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.skillMasteryPoolXP, miningDrill.GetMachineName());
       
        float p3 = Mathf.Clamp01(currentMasteryPoolXP / 11500000f);
        masteryPoolProgressBar.transform.localScale = Vector2.Lerp(new Vector2(0,1f), new Vector2(1,1f), p3);
        masteryPoolPercentText.text = "Mastery - " + ((currentMasteryPoolXP / 11500000f) * 100f).ToString("F2") + "%";
        masteryPoolProgressText.text  =  (int)currentMasteryPoolXP + " / " + "11,500,000";

        // Progress Bar
        if (miningDrill.mining)
        {
            if (miningDrill.respawningVein)
            {
                progressBar.GetComponent<Image>().sprite = whiteProgressBar;
                progressPercentText.text = ((float)miningDrill.tickTimer / (float)miningDrill.timeToVeinRespawn * 100).ToString("F0") + "%";

                float t = Mathf.Clamp01((float)miningDrill.tickTimer / (float)miningDrill.timeToVeinRespawn);
                progressBar.transform.localScale = new Vector2(t,1f);
                
            }
            else
            {
                progressBar.GetComponent<Image>().sprite = redProgressBar;
                progressPercentText.text = (((float)miningDrill.tickTimer / (float)miningDrill.timeToCraft) * 100).ToString("F0") + "%";

                float t = Mathf.Clamp01((float)miningDrill.tickTimer / (float)miningDrill.timeToCraft);
                progressBar.transform.localScale = new Vector2(t,1f);
            }
        }
        else
        {
            progressPercentText.text = "0%";
            progressBar.transform.localScale = new Vector2(0f,2.5f);
        }
    }
}
#endregion
