using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class MiningDrill : Machine
{
    
    [SerializeField] private int ticksToMine = 15;
    [SerializeField] private BoxCollider2D clickBox;

    private bool activated = false;

    protected override void MachineSetup()
    {

    }

    private void Update()
    {
        if (activated)
        {
            if (tickTimer >= ticksToMine)
            {
                tickTimer = 0;
                Mine();
            }
            ProgressBarVisuals();
        }
        
        HandleVisuals();
    }


    private void Mine()
    {
        ItemManager.SpawnObject(ItemManager.Instance.itemPrefab, ItemManager.Instance.oresList[Random.Range(0, ItemManager.Instance.oresList.Count)], new Vector3(0,0,0), Quaternion.identity);
    }

    #region Menu

    [Header("Menu")]
    
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject folder1Content;
    [SerializeField] private GameObject menuItemPrefab;

    private ItemData selectedOre;

    private void GenerateItemsList()
    {
        if (folder1Content.transform.childCount > 0)
        {
            foreach (Transform child in folder1Content.transform)
            {
                Destroy(child.gameObject);
            }
        }
        
        List<ItemData> sortedOres = ItemManager.Instance.oresList.OrderBy (ore => ore.value).ToList();
        
        foreach (ItemData ore in sortedOres)
        {
            if (ore.tags.Contains(ItemTag.Ore))
            {
                GameObject oreBlock = Instantiate(menuItemPrefab, folder1Content.transform);
                oreBlock.transform.Find("Title/Title Text").GetComponent<TextMeshProUGUI>().text = ore.displayName;
                oreBlock.transform.Find("Icon Frame/Icon").GetComponent<Image>().sprite = ore.icon;
                oreBlock.transform.Find("Icon Frame/Icon").GetComponent<Image>().SetNativeSize();
                oreBlock.transform.Find("Mastery/Level Text").GetComponent<TextMeshProUGUI>().text = SkillManager.Instance.GetDictionaryIntValue(SkillManager.Instance.oreMasteryLevel, ore.displayName).ToString();

                if (SkillManager.Instance.GetDictionaryBoolValue(SkillManager.Instance.oreUnlocks, ore.displayName) == true)
                {
                    oreBlock.transform.Find("Not Unlocked Block").gameObject.SetActive(false);
                    oreBlock.transform.Find("Button").GetComponent<Button>().onClick.AddListener(() => SelectOre(ore.displayName));
                }
                else
                {
                    oreBlock.transform.Find("Not Unlocked Block").gameObject.SetActive(true);
                    oreBlock.transform.Find("Not Unlocked Block/Level Display").gameObject.GetComponent<TextMeshProUGUI>().text = "Unlock at Level " + ore.unlockLevel;
                }
            }
        }
    }
    
    public void SelectOre(string name)
    {
        foreach (ItemData ore in ItemManager.Instance.oresList)
        {
            if (ore.displayName == name)
            {
                selectedOre = ore;
                UpdateVisuals();
            }   
        }
    }

    public void StartCrafting(string name)
    {
        Debug.Log("Crafting " + name);
        tickTimer = 0;
        activated = true;
    }

    [Header("Selection Menu")]

    [SerializeField] private GameObject noOreSelectedFrame;

    [SerializeField] private Image selectedIcon;
    [SerializeField] private TextMeshProUGUI selectedTitleText;
    [SerializeField] private TextMeshProUGUI selectedMasteryLevel;
    [SerializeField] private TextMeshProUGUI selectedMasteryProgressText;

    [SerializeField] private TextMeshProUGUI selectedVeinCount;
    [SerializeField] private TextMeshProUGUI selectedOreValue;

    [SerializeField] private TextMeshProUGUI selectedTimeToCraft;
    [SerializeField] private TextMeshProUGUI selectedXPValue;
    [SerializeField] private TextMeshProUGUI selectedMasteryValue;
    [SerializeField] private TextMeshProUGUI selectedMasteryPoolValue;

    [SerializeField] private TextMeshProUGUI selectedProgressPercent;
    [SerializeField] private GameObject selectedMasteryProgressBar;
    [SerializeField] private GameObject selectedProgressBar;

    [SerializeField] private Button craftButton;


    public void UpdateVisuals()
    {
        if (selectedOre != null)
        {
            noOreSelectedFrame.SetActive(false);

            int masteryLevel = SkillManager.Instance.GetDictionaryIntValue(SkillManager.Instance.oreMasteryLevel, selectedOre.displayName);
            
            selectedIcon.gameObject.SetActive(true);
            selectedIcon.sprite = selectedOre.icon;
            selectedIcon.SetNativeSize();

            selectedTitleText.text = selectedOre.displayName;
            selectedMasteryLevel.text = masteryLevel.ToString();
            selectedMasteryProgressText.text = "0/" + SkillManager.Instance.CalculateOreMasteryXPToNextLevel(masteryLevel).ToString();
            
            selectedVeinCount.text = SkillManager.Instance.CalculateVeinCount(masteryLevel).ToString() + "/" + SkillManager.Instance.CalculateVeinCount(masteryLevel).ToString();
            selectedOreValue.text = selectedOre.value.ToString();

            selectedTimeToCraft.text = (ticksToMine / 5).ToString("F2");
            selectedXPValue.text = selectedOre.XP.ToString();
            selectedMasteryValue.text = SkillManager.Instance.CalculateOreMasteryXP(selectedOre.displayName).ToString();
            selectedMasteryPoolValue.text = SkillManager.Instance.CalculateMasteryPoolXP(selectedOre.displayName).ToString();

            float p = Mathf.Clamp01(SkillManager.Instance.GetDictionaryIntValue(SkillManager.Instance.oreMasteryXP, selectedOre.displayName) / SkillManager.Instance.CalculateOreMasteryXPToNextLevel(masteryLevel));
            selectedMasteryProgressBar.transform.localScale = Vector2.Lerp(new Vector2(0,1f), new Vector2(1,1f), p);

            ProgressBarVisuals();

            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(() => StartCrafting(selectedOre.displayName));

        }
        else
        {
            noOreSelectedFrame.SetActive(true);
            selectedIcon.gameObject.SetActive(false);

            selectedTitleText.text = "";
            selectedMasteryLevel.text = "";
            selectedMasteryProgressText.text = "";

            selectedVeinCount.text = "";
            selectedOreValue.text = "";

            selectedTimeToCraft.text = "";
            selectedXPValue.text = "";
            selectedMasteryValue.text = "";
            selectedMasteryPoolValue.text = "";

            selectedProgressPercent.text = "";
            craftButton.onClick.RemoveAllListeners();
        }

        // logic for main xp bars
    }

    private void ProgressBarVisuals()
    {
        if (activated)
        {
            selectedProgressPercent.text = ((tickTimer / ticksToMine) * 100).ToString() + "%";

            float t = Mathf.Clamp01((float)tickTimer / (float)ticksToMine);
            selectedProgressBar.transform.localScale = new Vector2(t,1f);
        }
        else
        {
            selectedProgressPercent.text = "0%";
            selectedProgressBar.transform.localScale = new Vector2(0f,2.5f);
        }
    }

    // Called from MouseManager
    public void OpenMenu()
    {
        menuCanvas.SetActive(true);
        GameManager.Instance.SetMenuOpen(true);
        UpdateVisuals();
        GenerateItemsList();
    }

    // Called from close button
    public void CloseMenu()
    {
        menuCanvas.SetActive(false);
        GameManager.Instance.SetMenuOpen(false);
    }

    #endregion

    #region Visuals

    [Header("Visuals")]
    [SerializeField] private GameObject frame;
    [SerializeField] private GameObject tunnel;
    [SerializeField] private GameObject equipment;

    private void HandleVisuals()
    {
        
    }



    #endregion
}
