using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
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
            skillLevels.Add(machine.machineName, 1);
            skillXP.Add(machine.machineName, 0);
            skillMasteryPoolXP.Add(machine.machineName, 0);
        }

        foreach (ItemData ore in ItemManager.Instance.oresList)
        {
            oreMasteryLevel.Add(ore.displayName, 1);
            oreMasteryXP.Add(ore.displayName, 0f);

           
            oreVeinMax.Add(ore.displayName, 6);
            
            if (GetDictionaryIntValue(skillLevels, "Mining Drill") >= ore.unlockLevel)
            {
                oreUnlocks.Add(ore.displayName, true);
            }
            else
            {
                oreUnlocks.Add(ore.displayName, false);
            }
        }
    }

#endregion

#region Dictionary Functions

    public int GetDictionaryIntValue(Dictionary<string, int> dictionary, string name)
    {
        if (dictionary.ContainsKey(name))
        {
            return dictionary[name];
        }
        return 0;
    }

    public bool GetDictionaryBoolValue(Dictionary<string, bool> dictionary, string name)
    {
        if (dictionary.ContainsKey(name))
        {
            return dictionary[name];
        }
        return false;
    }

    public float GetDictionaryFloatValue(Dictionary<string, float> dictionary, string name)
    {
        if (dictionary.ContainsKey(name))
        {
            return dictionary[name];
        }
        return 0;
        
    }

    public void AddXP(Dictionary<string, float> dictionary, string name, float amount)
    {
        if (dictionary.ContainsKey(name))
        {
            dictionary[name] += amount;
        }
    }

    public void SetDictionaryIntValue(Dictionary<string, int> dictionary, string name, int amount)
    {
        if (dictionary.ContainsKey(name))
        {
            dictionary[name] = amount;
        }
    }

    public void SetDictionaryFloatValue(Dictionary<string, float> dictionary, string name, float amount)
    {
        if (dictionary.ContainsKey(name))
        {
            dictionary[name] = amount;
        }
    }

    public void ChangeDictionaryIntValue(Dictionary<string, int> dictionary, string name, int amount)
    {
        if (dictionary.ContainsKey(name))
        {
            dictionary[name] += amount;
        }
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

        int currentLevel = GetDictionaryIntValue(oreMasteryLevel, oreName);

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

#endregion

}
