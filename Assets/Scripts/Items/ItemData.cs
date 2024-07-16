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
    Ore,
    Gem,
}
