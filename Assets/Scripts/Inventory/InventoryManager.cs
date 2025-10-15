using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{

    public static InventoryManager Instance { get; private set; }


    private int inventorySize = 24;

    [SerializeField] private GameObject inventoryCanvas;
    [SerializeField] private GameObject itemsFolder;
    [SerializeField] private GameObject inventorySlotPrefab;

    private Dictionary<ItemData, int> inventoryItems = new();
    private Dictionary<ItemData, InventorySlot> slots = new();
    private readonly Queue<InventorySlot> emptySlots = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void GenerateInventorySlots()
    {
        // Clear existing items
        foreach (Transform child in itemsFolder.transform) { Destroy(child.gameObject); }
        slots.Clear();
        emptySlots.Clear();

        // Spawn filled slots first
        int filled = 0;
        foreach (var kvp in inventoryItems)
        {
            // Spawn Slot with Item
            GameObject itemSlotObj = Instantiate(inventorySlotPrefab, itemsFolder.transform);
            InventorySlot inventorySlot = itemSlotObj.GetComponent<InventorySlot>();
            inventorySlot.UpdateBlock(kvp.Key, kvp.Value);
            slots[kvp.Key] = inventorySlot;
            filled++;
        }

        // Spawn empties after
        int emptiesToMake = Mathf.Max(0, inventorySize - filled);
        for (int i = 0; i < emptiesToMake; i++)
        {
            GameObject itemSlotObj = Instantiate(inventorySlotPrefab, itemsFolder.transform);
            InventorySlot inventorySlot = itemSlotObj.GetComponent<InventorySlot>();
            emptySlots.Enqueue(inventorySlot);
        }
    }

#region Add/Remove Items

    public void AddItem(Item item)
    {
        if (item == null || item.itemData == null) return;
        ItemData data = item.itemData;

        inventoryItems.TryGetValue(data, out int count);
        inventoryItems[data] = count += 1;

        if (slots.TryGetValue(data, out InventorySlot slot))
        {
            slot.UpdateBlock(data, count);
        }
        else
        {
            // NEW item type
            if (inventoryCanvas.activeSelf && emptySlots.Count > 0)
            {
                // Reuse an existing empty slot so it appears before the empties block
                InventorySlot empty = emptySlots.Dequeue();
                empty.UpdateBlock(data, count);
                slots[data] = empty;
            }
            else
            {
                // Menu closed (or no empties spawned): just create; it’ll be placed correctly on next OpenMenu -> GenerateInventorySlots()
                GameObject go = Instantiate(inventorySlotPrefab, itemsFolder.transform);
                InventorySlot newSlot = go.GetComponent<InventorySlot>();
                newSlot.UpdateBlock(data, count);
                slots[data] = newSlot;
            }
        }

        Debug.Log(item.itemData.displayName + " added to Inventory");
    }

    public bool TryRemove(ItemData data, int amount)
    {
        if (data == null || amount <= 0) return false;
        if (!inventoryItems.TryGetValue(data, out var count) || count < amount) return false;

        count -= amount;
        if (count <= 0) 
        {
            inventoryItems.Remove(data);
            if (slots.TryGetValue(data, out InventorySlot slot))
            {
                Destroy(slot.gameObject);
                slots.Remove(data);
            } 
        }
        else 
        {
            inventoryItems[data] = count;
            if (slots.TryGetValue(data, out InventorySlot slot)) slot.UpdateBlock(data, count);
        }

        return true;
    }

#endregion

#region Slot Selection

    public void SlotSelected(InventorySlot slot)
    {
        foreach (var kvp in slots)
        {
            if (kvp.Value != slot) 
            {
                kvp.Value.DeselectSlot();
            }
            else
            {
                kvp.Value.SelectSlot();
            }
        }
    }

#endregion

#region Open/Close Menu

    // Called from MouseManager
    public void OpenMenu()
    {
        inventoryCanvas.SetActive(true);
        GameManager.Instance.SetMenuOpen(true);
        GenerateInventorySlots();
    }

    // Called from close button
    public void CloseMenu()
    {
        inventoryCanvas.SetActive(false);
        GameManager.Instance.SetMenuOpen(false);
    }

#endregion

#region Debug - Test Print

    public void TestList()
    {
        if (inventoryItems == null) { Debug.Log("Error: No Inventory List"); return; }
        if (inventoryItems.Count == 0) { Debug.Log("Inventory is empty"); return; }

        var itemsList = new System.Text.StringBuilder();
        itemsList.AppendLine($"InventoryItems ({inventoryItems.Count})");

        int index = 0;
        foreach (var kvp in inventoryItems)  // kvp.Key = ItemData, kvp.Value = quantity
        {
            var item = kvp.Key;
            var qty  = kvp.Value;
            itemsList.AppendLine($"[{index++}] {(item == null ? "null" : item.ToString())} x{qty}");
        }

        Debug.Log(itemsList.ToString());
    }

#endregion











}
