using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Unity.VisualScripting;
using UnityEngine.Animations;
using UnityEditor.Tilemaps;
using UnityEngine.Pool;

public class SkillManager : MonoBehaviour
{

    public static SkillManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public Dictionary<string, int> skillLevels = new Dictionary<string, int>();
    public Dictionary<string, float> skillXP = new Dictionary<string, float>();
    public Dictionary<string, float> skillMasteryPoolXP = new Dictionary<string, float>();



    public Dictionary<string, bool> itemUnlockedStatus = new Dictionary<string, bool>();
    public Dictionary<string, int> itemMasteryLevels = new Dictionary<string, int>();
    public Dictionary<string, float> itemMasteryXPCount = new Dictionary<string, float>();

    ///  This should probably be moved to be local to the Mining Drill

    //////////////////////////////////////////////////////////////////

#region Setup

    void Start()
    {
        foreach (MachineData machine in BuildManager.Instance.machineList)
        {
            skillLevels.TryAdd(machine.skillName, 1);
            skillXP.TryAdd(machine.skillName, 0);
            skillMasteryPoolXP.TryAdd(machine.skillName, 0);
        }

        foreach (ItemData item in ItemManager.Instance.itemList)
        {
            itemMasteryLevels.TryAdd(item.displayName, 1);
            itemMasteryXPCount.TryAdd(item.displayName, 0f);

            //if item is associated to a skill
            if (GetAssociatedSkillLevel(item) != 6969)
            {   
                //check if its unlocked yet and set it
                itemUnlockedStatus[item.displayName] = GetAssociatedSkillLevel(item) >= item.unlockLevel;
            }
            //if item has no skill
            else
            {
                //set the item to unlocked
                itemUnlockedStatus[item.displayName] = true;
            }
        }

        int GetAssociatedSkillLevel(ItemData item)
        {
            var skillTags = new[]
            {
                ItemTag.Mining,
                ItemTag.Timberforging,
                ItemTag.Aquaforming,
                ItemTag.Smelting,
                ItemTag.Refining,
                ItemTag.Crafting,
                ItemTag.Assembly,
                ItemTag.Printing,
                ItemTag.Robocraft,
                ItemTag.Alchemy
            };

            var matchingTag = skillTags.FirstOrDefault(tag => item.tags.Contains(tag));
            return matchingTag != default ? GetDictionaryValue(skillLevels, matchingTag.ToString()) : 6969;
        }
        /*

        // Process items that are tagged as Ore
        foreach (ItemData ore in ItemManager.Instance.itemList.Where(item => item.tags.Contains(ItemTag.Ore)))
        {
            oreMasteryLevel.TryAdd(ore.displayName, 1);
            oreMasteryXP.TryAdd(ore.displayName, 0f);
            oreVeinMax.TryAdd(ore.displayName, 6);
            oreUnlocks[ore.displayName] = miningDrillLevel >= ore.unlockLevel;
        }

        */

    }

    
#endregion

#region Dictionary Functions


    // Get dictionary values
    public T GetDictionaryValue<T>(Dictionary<string, T> dictionary, string name, T defaultValue = default)
    {
        if (dictionary.TryGetValue(name, out T value))
        {
            return value;
        }
        return defaultValue;
    }


    //Modify dictionary values
    public void ModifyDictionaryValue<T>(Dictionary<string, T> dictionary, string name, T amount, bool isAdd = false)
    {
        if (dictionary.ContainsKey(name))
        {
            T currentValue = dictionary[name];
            dictionary[name] = typeof(T) switch
            {
                var t when t == typeof(int) => (T)(object)(isAdd ? Convert.ToInt32(currentValue) + Convert.ToInt32(amount) : amount),
                var t when t == typeof(float) => (T)(object)(isAdd ? Convert.ToSingle(currentValue) + Convert.ToSingle(amount) : amount),
                var t when t == typeof(bool) => amount,
                _ => currentValue
            };
        }
    }

#endregion


#region XP Algorithms


    // Used for Skill XP and Item Mastery XP
    public int Calculate_XPToNextLevel(int skillLevel) 
    {
        float scalingFactor = GetScalingFactor(skillLevel);
        
        return Mathf.RoundToInt(83 * Mathf.Pow(1.104027812f, skillLevel - 1) * scalingFactor);
    }

    
    public float Calculate_GivenItemMasteryXP(string itemName, ItemTag skillTag)
    {   
        var relevantItemUnlockedStatus = new Dictionary<string, bool>();
        var relevantItemMasteryLevels = new Dictionary<string, int>();
        
        // Filter items based on their tags
        foreach (KeyValuePair<string, bool> item in itemUnlockedStatus)
        {
            ItemData itemData = GetItemDataFromName(item.Key); 
            if (itemData.tags.Contains(skillTag))
            {
                relevantItemUnlockedStatus[item.Key] = item.Value;
            }
        }

        foreach (KeyValuePair<string, int> item in itemMasteryLevels)
        {
            ItemData itemData = GetItemDataFromName(item.Key); 
            if (itemData.tags.Contains(skillTag))
            {
                relevantItemMasteryLevels[item.Key] = item.Value;
            }
        }

        int unlockedActions = relevantItemUnlockedStatus.Count(i => i.Value == true);

        int itemMasteryLevel = GetDictionaryValue(relevantItemMasteryLevels, itemName);

        int playerTotalMasteryForSkill = relevantItemMasteryLevels.Values.Sum();

        int totalMasteryForSkill = relevantItemMasteryLevels.Count * 99;

        int totalItemsInSkill = relevantItemMasteryLevels.Count;
        
        /////////////////

        float baseXP = (unlockedActions * playerTotalMasteryForSkill / totalMasteryForSkill) + (itemMasteryLevel * totalItemsInSkill / 10);

        float levelScaling = GetScalingFactor(itemMasteryLevel);

        return baseXP * levelScaling;
    }

    private float GetScalingFactor(int currentLevel)
    {
        return currentLevel <= 30 ? 1f : 1f + (0.5f * Mathf.Pow((currentLevel - 30) / 69f, 1.3f));
    }


#endregion

#region Unlock Checks


    public void UnlockNewOreCheck()
    {
        var oreUnlocksByLevel = new Dictionary<int, string>
        {
            { 15, "Iron Ore" },
            { 25, "Coal" },
            { 30, "Silver Ore" },
            { 40, "Gold Ore" },
            { 50, "Titanium Ore" },
            { 65, "Uranium Ore" },
            { 75, "Magicite Ore" },
            { 85, "Moltenite Ore" },
            { 95, "Nebulite Ore" }
        };

        int miningLevel = GetDictionaryValue(skillLevels, ItemTag.Mining.ToString());
        
        if (oreUnlocksByLevel.TryGetValue(miningLevel, out string oreName))
        {
            itemUnlockedStatus[oreName] = true;
        }
    }


#endregion

#region Tools


    private ItemData GetItemDataFromName(string itemName)
    {
        ItemData dsds = null;
        foreach (ItemData item in ItemManager.Instance.itemList)
        {
            if (item.displayName == itemName)
            {
                dsds = item;
            }  
        }

        return dsds;
    }

    
#endregion
}


