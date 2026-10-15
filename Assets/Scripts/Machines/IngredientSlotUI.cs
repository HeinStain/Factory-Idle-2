using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IngredientSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI countText;

    public void UpdateVisual(ItemData item, bool filled)
    {
        if (icon != null)
        {
            icon.enabled = item != null;
            if (item != null)
            {
                icon.sprite = item.icon;
            }
        }

        if (countText != null)
        {
            countText.text = filled ? "1/1" : "0/1";
        }
    }
}
