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
    public List<ItemTag> tags;

}

public enum ItemTag
{
    //Skill Tags
    Mining,
    Timberforging,
    Aquaforming,
    Smelting,
    Refining,
    Crafting,
    Assembly,
    Printing,
    Robocraft,
    Alchemy,


    Liquid,
    Fuel,
    Sand,
    Reagent,
    Misc,

    //Item Tags
    Ore,
    Gem,
    Log,

}
