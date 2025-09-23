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



#region Setup

    private bool isInitialized = false;

    void Start()
    {
        InitializeSystem();
    }

    private void InitializeSystem()
    {
        if (isInitialized) return;

        // Initialize skill-related dictionaries
        foreach (MachineData machine in BuildManager.Instance.machineList)
        {
            if (machine.skill != null)
            {
                skillLevels.TryAdd(machine.skill.skillName, 1);
                skillXP.TryAdd(machine.skill.skillName, 0);
                skillMasteryPoolXP.TryAdd(machine.skill.skillName, 0);
            }
        }

        // Initialize item-related dictionaries
        foreach (ItemData item in ItemManager.Instance.itemList)
        {
            itemMasteryLevels.TryAdd(item.displayName, 1);
            itemMasteryXPCount.TryAdd(item.displayName, 0f);
            itemUnlockedStatus.TryAdd(item.displayName, false);
        }

        isInitialized = true;
        InitializeUnlockStates();
    }

    private void InitializeUnlockStates()
    {
        foreach (var skillPair in skillLevels)
        {
            int currentSkillLevel = skillPair.Value;
            string skillName = skillPair.Key;

            if (System.Enum.TryParse(skillName, out ItemTag skillTag))
            {
                var relevantItems = ItemManager.Instance.itemList
                    .Where(item => item.tags.Contains(skillTag))
                    .ToList();

                foreach (var item in relevantItems)
                {
                    itemUnlockedStatus[item.displayName] = currentSkillLevel >= item.unlockLevel;
                }
            }
        }
    }

    
#endregion

#region Dictionary Functions


    // Get dictionary values
    public T GetDictionaryValue<T>(Dictionary<string, T> dictionary, string name, T defaultValue = default)
    {
        if (dictionary == null)
        {
            //Debug.LogError($"Dictionary is null when checking for {name}");
            return defaultValue;
        }

        if (string.IsNullOrEmpty(name))
        {
            //Debug.LogError("Name parameter is null or empty");
            return defaultValue;
        }

        if (dictionary.TryGetValue(name, out T value))
        {
            return value;
        }
        else
        {
            //Debug.LogWarning($"No value found in dictionary for key: {name}");
            return defaultValue;
        }
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
        // Initialize collection dictionaries
        var relevantItemUnlockedStatus = new Dictionary<string, bool>();
        var relevantItemMasteryLevels = new Dictionary<string, int>();
        
        // Gather relevant items for this skill
        foreach (KeyValuePair<string, bool> item in itemUnlockedStatus)
        {
            ItemData itemData = GetItemDataFromName(item.Key); 
            if (itemData?.tags.Contains(skillTag) == true)
            {
                relevantItemUnlockedStatus[item.Key] = item.Value;
            }
        }

        foreach (KeyValuePair<string, int> item in itemMasteryLevels)
        {
            ItemData itemData = GetItemDataFromName(item.Key); 
            if (itemData?.tags.Contains(skillTag) == true)
            {
                relevantItemMasteryLevels[item.Key] = item.Value;
            }
        }

        // Calculate base values
        float unlockedActions = relevantItemUnlockedStatus.Count(i => i.Value == true);
        float itemMasteryLevel = GetDictionaryValue(relevantItemMasteryLevels, itemName);
        float playerTotalMasteryForSkill = relevantItemMasteryLevels.Values.Sum();
        float totalMasteryForSkill = relevantItemMasteryLevels.Count * 99f; // Using float
        float totalItemsInSkill = relevantItemMasteryLevels.Count;

        // Calculate XP with proper floating point math
        float unlockedMultiplier = (unlockedActions * playerTotalMasteryForSkill) / totalMasteryForSkill;
        float masteryMultiplier = (itemMasteryLevel * totalItemsInSkill) / 10f;
        float baseXP = unlockedMultiplier + masteryMultiplier;

        // Apply level scaling
        float levelScaling = GetScalingFactor(Mathf.RoundToInt(itemMasteryLevel));
        float finalXP = baseXP * levelScaling;

        // Ensure minimum XP gain
        float minimumXP = 0.1f;
        finalXP = Mathf.Max(finalXP, minimumXP);

        return finalXP;
    }

    private float GetScalingFactor(int currentLevel)
    {
        return currentLevel <= 30 ? 1f : 1f + (0.5f * Mathf.Pow((currentLevel - 30) / 69f, 1.3f));
    }


#endregion

#region Unlock Checks



    public void CheckSkillUnlocks(string skillName)
    {
        if (!isInitialized)
        {
            InitializeSystem();
            return;
        }

        int currentSkillLevel = GetDictionaryValue(skillLevels, skillName);
        
        if (!System.Enum.TryParse(skillName, out ItemTag skillTag))
        {
            Debug.LogError($"Invalid skill name: {skillName}");
            return;
        }

        var relevantItems = ItemManager.Instance.itemList
            .Where(item => item.tags.Contains(skillTag))
            .ToList();

        foreach (var item in relevantItems)
        {
            bool shouldBeUnlocked = currentSkillLevel >= item.unlockLevel;
            itemUnlockedStatus[item.displayName] = shouldBeUnlocked;
            /*
            Debug.Log($"Checking {item.displayName} unlock state: " +
                     $"Required Level: {item.unlockLevel}, " +
                     $"Current Level: {currentSkillLevel}, " +
                     $"Unlocked: {shouldBeUnlocked}");
            */
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


