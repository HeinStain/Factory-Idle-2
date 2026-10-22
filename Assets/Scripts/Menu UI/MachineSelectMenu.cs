using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MachineSelectMenu : MonoBehaviour
{
    // Shown and hidden. This object stays active so Update can read the B key.
    [SerializeField] private GameObject panel;

    [Header("Grid")]
    [SerializeField] private Transform tileContainer;
    [SerializeField] private MachineSelectTile tilePrefab;

    [Header("Buttons")]
    [SerializeField] private Button openButton;     // on-screen "Machines" button
    [SerializeField] private Button closeButton;    // red X
    [SerializeField] private Button placeButton;
    [SerializeField] private Button quickslotButton;

    [Header("Quickslot Assign")]
    [SerializeField] private QuickslotBarUI quickslotBar;
    [SerializeField] private TextMeshProUGUI quickslotButtonLabel;
    [SerializeField] private string pickSlotText = "Pick a slot";

    [Header("Details")]
    [SerializeField] private Image detailIcon;
    [SerializeField] private TextMeshProUGUI detailName;
    [SerializeField] private TextMeshProUGUI detailDescription;

    [Header("Produces")]
    [SerializeField] private GameObject producesRow;        // hidden, heading included, for machines that make nothing
    [SerializeField] private Transform itemContainer;
    [SerializeField] private MachineSelectItem itemPrefab;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private TextMeshProUGUI sizeText;

    private readonly List<MachineSelectTile> tiles = new();
    private readonly List<MachineSelectItem> items = new();
    private MachineData selectedMachine;

    // While on, the next bar slot or number key takes the selected machine
    private bool assignMode;
    private string quickslotButtonText;

    public bool IsOpen => panel.activeSelf;


    private void Start()
    {
        // Remove the preview items placed in the scene
        foreach (Transform child in itemContainer) Destroy(child.gameObject);

        BuildTiles();

        openButton.onClick.AddListener(Toggle);
        closeButton.onClick.AddListener(Close);
        placeButton.onClick.AddListener(PlaceSelected);
        quickslotButton.onClick.AddListener(ToggleAssignMode);

        quickslotButtonText = quickslotButtonLabel.text;
        quickslotBar.OnSlotClickedInMenu += OnBarSlotClicked;
        BuildManager.Instance.OnQuickslotsChanged += RefreshBadges;

        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (quickslotBar != null) quickslotBar.OnSlotClickedInMenu -= OnBarSlotClicked;
        if (BuildManager.Instance != null) BuildManager.Instance.OnQuickslotsChanged -= RefreshBadges;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B)) Toggle();
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Esc cancels assign mode first, then closes the menu
            if (assignMode) EndAssign();
            else if (IsOpen) Close();
        }
        else if (assignMode)
        {
            // Number keys from 1 pick quickslots from 0
            for (int i = 0; i < BuildManager.QuickslotCount; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { AssignTo(i); break; }
            }
        }
    }

#region Open and Close

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        panel.SetActive(true);
        GameManager.Instance.SetMachineMenuOpen(true);

        // Start on the held machine, else the last one viewed, else the first tile
        MachineData held = BuildManager.Instance.heldMachine;
        if (held != null) Select(held);
        else if (selectedMachine != null) Select(selectedMachine);
        else if (tiles.Count > 0) Select(tiles[0].Machine);
    }

    public void Close()
    {
        EndAssign();
        panel.SetActive(false);
        GameManager.Instance.SetMachineMenuOpen(false);
    }

#endregion

#region Grid

    // One tile per machine in machineList, grouped by category (machineList order within each category)
    private void BuildTiles()
    {
        IEnumerable<MachineData> machines = BuildManager.Instance.machineList
            .Where(m => m != null)
            .OrderBy(m => m.category);

        foreach (MachineData machine in machines)
        {
            MachineSelectTile tile = Instantiate(tilePrefab, tileContainer);
            tile.Setup(this, machine);
            tiles.Add(tile);
        }
    }

    public void Select(MachineData machine)
    {
        EndAssign();
        selectedMachine = machine;
        foreach (MachineSelectTile tile in tiles) tile.SetSelected(tile.Machine == machine);

        RefreshDetails();
    }

    private void RefreshBadges()
    {
        foreach (MachineSelectTile tile in tiles) tile.RefreshBadge();
    }

#endregion

#region Details

    private void RefreshDetails()
    {
        MachineData machine = selectedMachine;
        if (machine == null) return;

        detailIcon.enabled = machine.icon != null;
        if (detailIcon.enabled) detailIcon.sprite = machine.icon;

        detailName.text = machine.machineName;

        bool hasDescription = !string.IsNullOrWhiteSpace(machine.description);
        detailDescription.gameObject.SetActive(hasDescription);
        if (hasDescription) detailDescription.text = machine.description;

        RefreshProduces(machine);

        costText.text = "-";    // cost fields arrive in v0.2.9
        powerText.text = FormatPower(machine.energyCost);
        sizeText.text = $"{machine.width}x{machine.height}";
    }

    // Same filter the machine Visuals scripts use
    private void RefreshProduces(MachineData machine)
    {
        List<ItemData> produced = ItemManager.Instance.itemList
            .Where(item => item != null && item.producedBy == machine)
            .ToList();

        producesRow.SetActive(produced.Count > 0);

        // Reuse item icons, adding more only when needed
        while (items.Count < produced.Count) items.Add(Instantiate(itemPrefab, itemContainer));

        for (int i = 0; i < items.Count; i++)
        {
            bool used = i < produced.Count;
            items[i].gameObject.SetActive(used);
            if (used) items[i].Setup(produced[i]);
        }
    }

    // "-" for no power, kW below 1,000, MW with one decimal from 1,000 up
    private static string FormatPower(int kW)
    {
        if (kW <= 0) return "-";
        if (kW < 1000) return $"{kW} kW";
        return (kW / 1000f).ToString("0.0", CultureInfo.InvariantCulture) + " MW";
    }

#endregion

#region Buttons

    // Closes the menu and holds the selected machine in build mode
    private void PlaceSelected()
    {
        if (selectedMachine == null) return;

        Close();
        GameManager.Instance.SetBuildMode(true);
        BuildManager.Instance.SelectMachine(selectedMachine);
    }

#endregion

#region Quickslot Assign

    // Add to Quickslot: a second click cancels
    private void ToggleAssignMode()
    {
        if (assignMode) EndAssign();
        else BeginAssign();
    }

    private void BeginAssign()
    {
        if (selectedMachine == null) return;

        assignMode = true;
        quickslotButtonLabel.text = pickSlotText;
        quickslotBar.SetPulsing(true);
    }

    private void EndAssign()
    {
        if (!assignMode) return;

        assignMode = false;
        quickslotButtonLabel.text = quickslotButtonText;
        quickslotBar.SetPulsing(false);
    }

    // Moves the selected machine into the slot, then leaves assign mode
    private void AssignTo(int index)
    {
        MachineData machine = selectedMachine;
        EndAssign();
        BuildManager.Instance.AssignQuickslot(index, machine);
    }

    // Bar slots are display-only while the menu is open, except in assign mode
    private void OnBarSlotClicked(int index)
    {
        if (assignMode) AssignTo(index);
    }

#endregion
}
