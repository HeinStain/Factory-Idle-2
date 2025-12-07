using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Item Data")]
public class ItemData : ScriptableObject
{
    
    public string displayName;
    public Sprite icon;
    public int value;
    public int unlockLevel;
    public int XP;

    public SkillData skillData;
  //  public List<ItemTag> tags;

    public List<ItemData> ingredients;

}

/*
public enum ItemTag
{
    Liquid,
    Fuel,
    Sand,
    Reagent,
    Misc,

    //Item Tags
    Ore,
    Gem,
    Log,

    Glass,
    Bar,
}
*/
