using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using Unity.Collections;
using System.Data;

public class OreSelectBlock : MonoBehaviour
{
    
    

    [SerializeField] private TextMeshProUGUI titleText;
    
    [SerializeField] private Image oreIcon;


    [SerializeField] private TextMeshProUGUI masteryLevelText;
    [SerializeField] private TextMeshProUGUI masteryUpgradeText;
    [SerializeField] private GameObject progressBar;
    [SerializeField] private GameObject notUnlockedBlock;
    [SerializeField] private TextMeshProUGUI levelReqDisplay;

    [SerializeField] private Button selectButton;
    
    [SerializeField] private Button upgradeButton;

    private MiningDrill thisMiningDrill;

    private ItemData ore;

    public void ShowText()
    {
        masteryUpgradeText.gameObject.SetActive(true);
    }

    public void HideText()
    {
        masteryUpgradeText.gameObject.SetActive(false);
    }

    public void UpgradeOre()
    {
        int currentOreMasteryLevel = SkillManager.Instance.GetDictionaryIntValue(SkillManager.Instance.oreMasteryLevel, titleText.text);
        float currentOreMasteryXP = SkillManager.Instance.GetDictionaryFloatValue(SkillManager.Instance.oreMasteryXP, titleText.text);
        float xpToNextMasteryLevel = SkillManager.Instance.CalculateOreMasteryXPToNextLevel(currentOreMasteryLevel) - currentOreMasteryXP;
        float masteryPoolXP = SkillManager.Instance.GetDictionaryFloatValue(SkillManager.Instance.skillMasteryPoolXP, thisMiningDrill.GetMachineName());
        
        if (masteryPoolXP >= xpToNextMasteryLevel)
        {
            // Ore Mastery Level Up
            SkillManager.Instance.AddXP(SkillManager.Instance.skillMasteryPoolXP, thisMiningDrill.GetMachineName(), -xpToNextMasteryLevel);
            SkillManager.Instance.SetDictionaryIntValue(SkillManager.Instance.oreMasteryLevel, titleText.text, currentOreMasteryLevel + 1);
            SkillManager.Instance.SetDictionaryFloatValue(SkillManager.Instance.oreMasteryXP, titleText.text, 0);
            SkillManager.Instance.SetDictionaryIntValue(SkillManager.Instance.oreVeinMax, titleText.text, SkillManager.Instance.CalculateVeinMax(currentOreMasteryLevel +1));
            NotificationManager.Instance.DisplayNotification(titleText.text + " Mastery Level " + SkillManager.Instance.GetDictionaryIntValue(SkillManager.Instance.oreMasteryLevel, titleText.text).ToString() + "!");

        }
        else
        {
            NotificationManager.Instance.DisplayNotification("Not Enough Mastery Pool XP");
        }
        
    }
    
    public void SetOre(ItemData o)
    {
        ore = o;
    }

    public void UpdateBlock(MiningDrill miningDrill)
    {
        thisMiningDrill = miningDrill;
        titleText.text = ore.displayName;
        oreIcon.sprite = ore.icon;
        oreIcon.SetNativeSize();
        int masteryLevel = SkillManager.Instance.GetDictionaryIntValue(SkillManager.Instance.oreMasteryLevel, ore.displayName);
        masteryLevelText.text = masteryLevel.ToString();
        
        masteryUpgradeText.text = ((int)((SkillManager.Instance.CalculateOreMasteryXPToNextLevel(masteryLevel) - SkillManager.Instance.GetDictionaryFloatValue(SkillManager.Instance.oreMasteryXP, ore.displayName)))).ToString() + "xp for +1 lvls";
        float p = Mathf.Clamp01(SkillManager.Instance.GetDictionaryFloatValue(SkillManager.Instance.oreMasteryXP, ore.displayName) / SkillManager.Instance.CalculateOreMasteryXPToNextLevel(masteryLevel));
        progressBar.transform.localScale = Vector2.Lerp(new Vector2(0,1f), new Vector2(1,1f), p);

        if (SkillManager.Instance.GetDictionaryBoolValue(SkillManager.Instance.oreUnlocks, ore.displayName) == true)
        {
            notUnlockedBlock.SetActive(false);
            selectButton.onClick.AddListener(() => thisMiningDrill.SelectOre(ore));
        }
        else
        {
            notUnlockedBlock.SetActive(true);
            levelReqDisplay.text = "Unlock at Level " + ore.unlockLevel;
        }

    }
}
