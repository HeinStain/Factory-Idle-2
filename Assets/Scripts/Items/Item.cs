using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{

    public SpriteRenderer spriteRenderer;
    public ItemData itemData;

    public void ItemSetup(ItemData data)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        itemData = data;
        spriteRenderer.sprite = itemData.icon;
    }
}
