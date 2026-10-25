using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MachineSelectTile : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private GameObject selectionBox;
    [SerializeField] private Button selectButton;

    [Header("Quickslot Badge")]
    [SerializeField] private GameObject badge;
    [SerializeField] private TextMeshProUGUI badgeText;

    public MachineData Machine { get; private set; }

    // Called by the menu after instantiating the tile
    public void Setup(MachineSelectMenu menu, MachineData machine)
    {
        Machine = machine;

        titleText.text = machine.machineName;
        icon.enabled = machine.icon != null;
        if (icon.enabled) icon.sprite = machine.icon;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => menu.Select(machine));

        SetSelected(false);
        RefreshBadge();
    }

    public void SetSelected(bool selected) => selectionBox.SetActive(selected);

    // Shows the quickslot number (index + 1) holding this machine, hidden when it has none
    public void RefreshBadge()
    {
        for (int i = 0; i < BuildManager.QuickslotCount; i++)
        {
            if (BuildManager.Instance.GetQuickslot(i) == Machine)
            {
                badge.SetActive(true);
                badgeText.text = (i + 1).ToString();
                return;
            }
        }
        badge.SetActive(false);
    }
}
