using UnityEngine;
using UnityEngine.UI;

public class MachineSelectItem : MonoBehaviour
{
    [SerializeField] private Image icon;

    // Called by the menu for each item in the Produces row
    public void Setup(ItemData item)
    {
        icon.enabled = item.icon != null;
        if (icon.enabled) icon.sprite = item.icon;
    }
}
