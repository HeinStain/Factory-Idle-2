using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Skill Data")]
public class SkillData : ScriptableObject
{
    
    public string skillName;
    public int energyCost;
    public int defaultCraftingInterval;
    public int masteryPoolSize;

}
