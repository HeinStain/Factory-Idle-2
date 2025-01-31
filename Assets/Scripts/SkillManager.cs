using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Unity.VisualScripting;
using UnityEngine.Animations;

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


    public Dictionary<string, int> oreMasteryLevel = new Dictionary<string, int>();
    public Dictionary<string, float> oreMasteryXP = new Dictionary<string, float>();
    public Dictionary<string, bool> oreUnlocks = new Dictionary<string, bool>();
    public Dictionary<string, int> oreVeinMax = new Dictionary<string, int>();

#region Setup

    void Start()
    {
        foreach (MachineData machine in BuildManager.Instance.machineList)
        {
            skillLevels.TryAdd(machine.machineName, 1);
            skillXP.TryAdd(machine.machineName, 0);
            skillMasteryPoolXP.TryAdd(machine.machineName, 0);
        }

        // Process items that are tagged as Ore
        int miningDrillLevel = GetDictionaryValue(skillLevels, "Mining Drill");
        foreach (ItemData ore in ItemManager.Instance.itemList.Where(item => item.tags.Contains(ItemTag.Ore)))
        {
            oreMasteryLevel.TryAdd(ore.displayName, 1);
            oreMasteryXP.TryAdd(ore.displayName, 0f);
            oreVeinMax.TryAdd(ore.displayName, 6);
            oreUnlocks[ore.displayName] = miningDrillLevel >= ore.unlockLevel;
        }
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

    // Specific method to add XP
    public void AddXP(Dictionary<string, float> dictionary, string name, float amount)
    {
        ModifyDictionaryValue(dictionary, name, amount, true);
    }

#endregion

#region Ore Calculations

    public float CalculateGivenOreMasteryXP(string oreName)
    {
        int unlockedOres = 0;
        
        if (oreUnlocks.ContainsKey(oreName))
        {
            if (oreUnlocks[oreName] == true)
            {
                unlockedOres++;
            }
        }

        int currentLevel = GetDictionaryValue(oreMasteryLevel, oreName);

        float result = (float)(((unlockedOres * oreMasteryLevel.Values.Sum() / (oreMasteryLevel.Count * 99)) + (currentLevel * oreMasteryLevel.Count / 10) * 1.5)); //actionTime * 0.5 * (1 + bonus);
        return result;           
    }

    public int CalculateOreMasteryXPToNextLevel(int currentLevel)
    {
        return Mathf.RoundToInt(83 * Mathf.Pow(1.104027812f, (currentLevel - 1)));
    }

    public int CalculateXPToNextLevel(int currentLevel)
    {
        return Mathf.FloorToInt((1f / 4f) * (currentLevel + 300 * Mathf.Pow(2, currentLevel / 7f)));
    }

    public int CalculateVeinMax(int currentLevel)
    {
        return currentLevel + 5;
    }

    public float CalculateGivenMasteryPoolXP(string oreName)
    {
        return CalculateGivenOreMasteryXP(oreName) / 4;
        
    }

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

        int miningLevel = GetDictionaryValue(skillLevels, "Mining Drill");
        
        if (oreUnlocksByLevel.TryGetValue(miningLevel, out string oreName))
        {
            oreUnlocks[oreName] = true;
        }
    }

#endregion

}
