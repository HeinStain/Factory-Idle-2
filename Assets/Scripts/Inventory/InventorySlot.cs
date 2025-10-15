using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour
{

    [Header("UI")]
    [SerializeField] private Image selectionBox;
    [SerializeField] private Image slotFrame;
    [SerializeField] private Image itemIcon;
    [SerializeField] private GameObject numberBubble;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Button selectButton;

    [Header("Data")]
    private ItemData itemData;
    private int count;


/*

    private void Awake()
    {
        InventoryManager.Instance.OnSlotChanged += UpdateBlock;
    }

    private void OnDestroy() 
    {
        InventoryManager.Instance.OnSlotChanged -= UpdateBlock;
    }

*/

    public void UpdateBlock(ItemData data, int amount)
    {
        if (data == null) return;
        
        itemData = data;
        count = amount;

        slotFrame.enabled = true;
        itemIcon.enabled = true;
        itemIcon.sprite = itemData.icon;
        itemIcon.SetNativeSize();
        numberBubble.SetActive(true);
        countText.text = count.ToString();                                            

        selectButton.onClick.RemoveAllListeners();                                                        
        selectButton.onClick.AddListener(() => InventoryManager.Instance.SlotSelected(this));
    }

    public void SelectSlot() => selectionBox.enabled = true;

    public void DeselectSlot() => selectionBox.enabled = false;

}
