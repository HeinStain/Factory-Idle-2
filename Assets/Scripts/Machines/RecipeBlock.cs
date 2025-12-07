using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using Unity.Collections;
using System.Data;
using UnityEngine.Pool;

public class RecipeBlock : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI masteryLevelText;
    [SerializeField] private TextMeshProUGUI masteryUpgradeText;
    [SerializeField] private GameObject progressBar;
    [SerializeField] private GameObject notUnlockedBlock;
    [SerializeField] private TextMeshProUGUI levelReqDisplay;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private GameObject selectionSquare;

        // Data
    private IRecipeMenu menu;          // owning menu (producer or furnace)
    private Machine machine;           // underlying machine (base class)

    public ItemData itemData { get; private set; }
    public bool isSelected => menu?.GetSelectedRecipe() == itemData;



#region Public API

    /// <summary>
    /// Called by the menu after instantiating the block.
    /// </summary>
    public void UpdateBlock(IRecipeMenu menu, ItemData item)
    {
        this.menu    = menu;
        this.machine = menu.GetMachineScript();

        itemData = item;

        SetupInitialState();
        UpdateBlockVisuals();
    }

#endregion

#region Button Helpers

    public void ShowText()  => masteryUpgradeText.gameObject.SetActive(true);
    public void HideText()  => masteryUpgradeText.gameObject.SetActive(false);


    public void Upgradeitem()
    {
        var sm    = SkillManager.Instance;
        var skill = machine.GetMachineData().skill;

        int   currentLevel  = sm.GetDictionaryValue(sm.itemMasteryLevels, titleText.text);
        float currentXP     = sm.GetDictionaryValue(sm.itemMasteryXPCount, titleText.text);
        float xpToNextLevel = sm.Calculate_XPToNextLevel(currentLevel) - currentXP;
        float masteryPoolXP = sm.GetDictionaryValue(sm.skillMasteryPoolXP, skill);
        
        if (masteryPoolXP < xpToNextLevel)
        {
            NotificationManager.Instance.DisplayNotification("Not Enough Mastery Pool XP");
            return;
        }

        // Item Mastery Level Up
        sm.ModifyDictionaryValue(sm.skillMasteryPoolXP, skill, -xpToNextLevel, true);
        sm.ModifyDictionaryValue(sm.itemMasteryLevels, titleText.text, currentLevel + 1, false);
        sm.ModifyDictionaryValue(sm.itemMasteryXPCount, titleText.text, 0, false);

        // Producer-only reservoir bonus
        if (machine is Machine_Producer p)
        {
            sm.ModifyDictionaryValue(p.reservoirMaxes,
                                     titleText.text,
                                     currentLevel + 1 + 5,
                                     false);
        }
        
        NotificationManager.Instance.DisplayNotification
            ($"{titleText.text} Mastery Level {sm.GetDictionaryValue(sm.itemMasteryLevels, titleText.text)}!");

        UpdateBlockVisuals();
    }

#endregion

#region Setup

    private void SetupInitialState()
    {
        if (machine == null || itemData == null) return;

        titleText.text = itemData.displayName;
        itemIcon.sprite = itemData.icon;
        itemIcon.SetNativeSize();

        // Buttons
        selectButton.onClick.RemoveAllListeners();

        if (IsItemUnlocked())
            // Click = tell menu to select this recipe
            selectButton.onClick.AddListener(() => 
                menu.SelectItem(itemData));
        else
            // Locked – disable actions
            selectButton.onClick.AddListener(() => 
                NotificationManager.Instance.DisplayNotification("Recipe locked"));
    }

    private bool IsItemUnlocked()
    {
        if (itemData == null) return false;

        var sm = SkillManager.Instance;

        // Use stored unlock status 
        return sm.GetDictionaryValue(
            sm.itemUnlockedStatus, 
            itemData.displayName, 
            false  // Default to locked if not found
        );
    }

#endregion

#region Visual Updates

    public void UpdateBlockVisuals()
    {        
        if (itemData == null) return;

        selectionSquare.SetActive(isSelected);

        bool isUnlocked = IsItemUnlocked();
        
        var sm = SkillManager.Instance;
        
        int masteryLevel       = sm.GetDictionaryValue(sm.itemMasteryLevels, itemData.displayName);
        float currentXP       = sm.GetDictionaryValue(sm.itemMasteryXPCount, itemData.displayName);
        float xpToNextLevel    = sm.Calculate_XPToNextLevel(masteryLevel);
        float progress         = Mathf.Clamp01(currentXP / xpToNextLevel);
  
        masteryLevelText.text   = masteryLevel.ToString();
        masteryUpgradeText.text   = $"{(int)(xpToNextLevel - currentXP)}xp for +1 lvls";
        progressBar.transform.localScale = Vector2.Lerp(new Vector2(0,1f), new Vector2(1,1f), progress);

        notUnlockedBlock.SetActive(!isUnlocked);
        if (!isUnlocked) levelReqDisplay.text = $"Unlock at Level {itemData.unlockLevel}";
    }

#endregion
}