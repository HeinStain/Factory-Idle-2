using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using Unity.Collections;
using System.Data;

public class ProducerRecipeBlock : MonoBehaviour
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


    private Machine_Producer producer;
    private Machine_Producer_Visuals producerVisuals; 
    public ItemData itemData { get; private set; } 


    [SerializeField] private GameObject selectionSquare;
    public bool isSelected => producer?.selectedRecipe == itemData;


    public void ShowText()
    {
        masteryUpgradeText.gameObject.SetActive(true);
    }

    public void HideText()
    {
        masteryUpgradeText.gameObject.SetActive(false);
    }

    public void Upgradeitem()
    {
        int currentitemMasteryLevel = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.itemMasteryLevels, titleText.text);
        float currentitemMasteryXP = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.itemMasteryXPCount, titleText.text);
        float xpToNextMasteryLevel = SkillManager.Instance.Calculate_XPToNextLevel(currentitemMasteryLevel) - currentitemMasteryXP;
        float masteryPoolXP = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.skillMasteryPoolXP, producer.GetMachineData().machineName);
        
        if (masteryPoolXP >= xpToNextMasteryLevel)
        {
            // itemData Mastery Level Up
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.skillMasteryPoolXP, producer.GetMachineData().machineName, -xpToNextMasteryLevel, true);
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.itemMasteryLevels, titleText.text, currentitemMasteryLevel + 1, false);
            SkillManager.Instance.ModifyDictionaryValue(SkillManager.Instance.itemMasteryXPCount, titleText.text, 0, false);
            SkillManager.Instance.ModifyDictionaryValue(producer.reservoirMaxes, titleText.text, currentitemMasteryLevel + 1 + 5, false);
            NotificationManager.Instance.DisplayNotification(titleText.text + " Mastery Level " + SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.itemMasteryLevels, titleText.text).ToString() + "!");

        }
        else
        {
            NotificationManager.Instance.DisplayNotification("Not Enough Mastery Pool XP");
        }
        
    }


    public void UpdateBlock(Machine_Producer_Visuals menu, ItemData i)
    {
        itemData = i;
        producerVisuals = menu; // Store the reference
        producer = menu.GetProducerScript();
        SetupInitialState();
        UpdateBlockVisuals();
    }

    private void SetupInitialState()
    {
        if (producer == null || itemData == null) return;

        titleText.text = itemData.displayName;
        itemIcon.sprite = itemData.icon;
        itemIcon.SetNativeSize();

        // Setup button listeners only once
        selectButton.onClick.RemoveAllListeners();
        if (IsItemUnlocked())
        {
            // Store reference to Machine_Producer_Visuals
            selectButton.onClick.AddListener(() => producerVisuals.SelectItem(itemData));
        }
    }

    private bool IsItemUnlocked()
    {
        if (producer == null || itemData == null) return false;

        // Use the stored unlock status instead of recalculating
        return SkillManager.Instance.GetDictionaryValue(
            SkillManager.Instance.itemUnlockedStatus, 
            itemData.displayName, 
            false  // Default to locked if not found
        );
    }

    public void UpdateBlockVisuals()
    {
        selectionSquare.SetActive(isSelected);

        // Only update the visual elements that need to change
        bool isUnlocked = IsItemUnlocked();
        
        int masteryLevel = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.itemMasteryLevels, itemData.displayName);
        masteryLevelText.text = masteryLevel.ToString();

        float currentMasteryXP = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.itemMasteryXPCount, itemData.displayName);
        float xpToNextLevel = SkillManager.Instance.Calculate_XPToNextLevel(masteryLevel);
        
        masteryUpgradeText.text = ((int)(xpToNextLevel - currentMasteryXP)).ToString() + "xp for +1 lvls";
        float progress = Mathf.Clamp01(currentMasteryXP / xpToNextLevel);
        progressBar.transform.localScale = Vector2.Lerp(new Vector2(0,1f), new Vector2(1,1f), progress);

        notUnlockedBlock.SetActive(!isUnlocked);
        if (!isUnlocked)
        {
            levelReqDisplay.text = "Unlock at Level " + itemData.unlockLevel;
        }
    }
}