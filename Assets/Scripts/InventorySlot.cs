using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour
{



    [SerializeField] private Image selectionBox;
    [SerializeField] private Image slotFrame;
    [SerializeField] private Image itemIcon;
    [SerializeField] private GameObject numberBubble;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Button selectButton;

    private InventoryManager inventoryManager;
    public ItemData itemData;
    private int count;



    public void UpdateBlock(InventoryManager invMan, ItemData data, int cnt)
    {

        if (invMan == null || data == null) return;
        
        inventoryManager = invMan;
        itemData = data;
        count = cnt;

        slotFrame.enabled = true;
        itemIcon.enabled = true;
        itemIcon.sprite = itemData.icon;
        itemIcon.SetNativeSize();
        numberBubble.SetActive(true);
        countText.text = count.ToString();                                            

        // Setup button listeners only once
        selectButton.onClick.RemoveAllListeners();                                                        
        selectButton.onClick.AddListener(() => invMan.SlotSelected(this));
    }


    public void SelectSlot()
    {
        selectionBox.enabled = true;
    }


    public void DeselectSlot()
    {
        selectionBox.enabled = false;
    }   

}
