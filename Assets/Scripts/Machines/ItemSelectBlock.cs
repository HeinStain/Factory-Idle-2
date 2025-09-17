using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using Unity.Collections;
using System.Data;

public class ItemSelectBlock : MonoBehaviour
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
    private ProducerVisuals producerVisuals; 
    public ItemData item { get; private set; } 


    [SerializeField] private GameObject selectionSquare;
    public bool IsSelected => producer?.selectedRecipe == item;


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
            // item Mastery Level Up
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
    
    public void SetItem(ItemData i)
    {
        item = i;
    }

    public void UpdateBlock(ProducerVisuals menu)
    {
        producerVisuals = menu; // Store the reference
        producer = menu.producer;
        SetupInitialState();
        UpdateVisuals();
    }

    private void SetupInitialState()
    {
        if (producer == null || item == null) return;

        titleText.text = item.displayName;
        itemIcon.sprite = item.icon;
        itemIcon.SetNativeSize();

        // Setup button listeners only once
        selectButton.onClick.RemoveAllListeners();
        if (IsItemUnlocked())
        {
            // Store reference to ProducerVisuals
            selectButton.onClick.AddListener(() => producerVisuals.SelectItem(item));
        }
    }

    private bool IsItemUnlocked()
    {
        if (producer == null || item == null) return false;

        // Use the stored unlock status instead of recalculating
        return SkillManager.Instance.GetDictionaryValue(
            SkillManager.Instance.itemUnlockedStatus, 
            item.displayName, 
            false  // Default to locked if not found
        );
    }

    public void UpdateVisuals()
    {
        selectionSquare.SetActive(IsSelected);

        // Only update the visual elements that need to change
        bool isUnlocked = IsItemUnlocked();
        
        int masteryLevel = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.itemMasteryLevels, item.displayName);
        masteryLevelText.text = masteryLevel.ToString();

        float currentMasteryXP = SkillManager.Instance.GetDictionaryValue(SkillManager.Instance.itemMasteryXPCount, item.displayName);
        float xpToNextLevel = SkillManager.Instance.Calculate_XPToNextLevel(masteryLevel);
        
        masteryUpgradeText.text = ((int)(xpToNextLevel - currentMasteryXP)).ToString() + "xp for +1 lvls";
        float progress = Mathf.Clamp01(currentMasteryXP / xpToNextLevel);
        progressBar.transform.localScale = Vector2.Lerp(new Vector2(0,1f), new Vector2(1,1f), progress);

        notUnlockedBlock.SetActive(!isUnlocked);
        if (!isUnlocked)
        {
            levelReqDisplay.text = "Unlock at Level " + item.unlockLevel;
        }
    }
}