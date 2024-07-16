using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SkillManager : MonoBehaviour
{

    public static SkillManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public Dictionary<string, int> skillLevels = new Dictionary<string, int>();

    public Dictionary<string, int> oreMasteryLevel = new Dictionary<string, int>();
    public Dictionary<string, int> oreMasteryXP = new Dictionary<string, int>();
    public Dictionary<string, bool> oreUnlocks = new Dictionary<string, bool>();

#region Setup

    void Start()
    {
        foreach (MachineData machine in BuildManager.Instance.machineList)
        {
            skillLevels.Add(machine.machineName, 1);
        }

        foreach (ItemData ore in ItemManager.Instance.oresList)
        {
            oreMasteryLevel.Add(ore.displayName, 1);
            oreMasteryXP.Add(ore.displayName, 0);
            
            if (GetDictionaryIntValue(oreMasteryLevel, ore.displayName) >= ore.unlockLevel)
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
        foreach (KeyValuePair<string, int> entry in dictionary)
        {
            if (name == entry.Key)
            {
                return entry.Value;
            }
        }
        return 0;
    }

    public bool GetDictionaryBoolValue(Dictionary<string, bool> dictionary, string name)
    {
        foreach (KeyValuePair<string, bool> entry in dictionary)
        {
            if (name == entry.Key)
            {
                return entry.Value;
            }
        }
        return false;
    }

#endregion

#region Ore Calculations

    public int CalculateOreMasteryXP(string oreName)
    {
        int unlockedOres = 0;
        foreach (KeyValuePair<string, bool> entry in oreUnlocks)
        {
            if (entry.Value == true)
            {
                unlockedOres++;
            }
        }

        int currentLevel;
        currentLevel = GetDictionaryIntValue(oreMasteryLevel, oreName);

        int result = (int)(((unlockedOres * oreMasteryLevel.Values.Sum() / (oreMasteryLevel.Count * 99)) + (currentLevel * oreMasteryLevel.Count / 10) * 1.5)); //actionTime * 0.5 * (1 + bonus);
        return result;           
    }

    public int CalculateOreMasteryXPToNextLevel(int currentLevel)
    {
        return Mathf.RoundToInt(83 * Mathf.Pow(1.104027812f, (currentLevel - 1)));
    }

    public int CalculateVeinCount(int currentLevel)
    {
        return currentLevel + 5;
    }

    public int CalculateMasteryPoolXP(string oreName)
    {
        return (int)CalculateOreMasteryXP(oreName) / 4;
        
    }

#endregion

}
