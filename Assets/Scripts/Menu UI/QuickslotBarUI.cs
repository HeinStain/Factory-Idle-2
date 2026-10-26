using System;
using UnityEngine;

public class QuickslotBarUI : MonoBehaviour
{
    // Shown and hidden. Lives under this object so the bar can be toggled without disabling the script.
    [SerializeField] private GameObject bar;
    [SerializeField] private QuickslotSlotUI[] slots = new QuickslotSlotUI[BuildManager.QuickslotCount];

    [Header("Pulse")]
    [SerializeField] private float pulseSpeed = 5f;     // radians per second
    [SerializeField, Range(0f, 1f)] private float pulseMinAlpha = 0.2f;
    [SerializeField, Range(0f, 1f)] private float pulseMaxAlpha = 1f;

    // Raised instead of selecting while the machine menu is open
    public event Action<int> OnSlotClickedInMenu;

    private bool pulsing;


    private void OnValidate()
    {
        // Keep one slot per quickslot in the Inspector
        if (slots == null || slots.Length != BuildManager.QuickslotCount) Array.Resize(ref slots, BuildManager.QuickslotCount);
    }

    private void Start()
    {
        for (int i = 0; i < slots.Length; i++) slots[i].Setup(this, i);

        BuildManager.Instance.OnSelectedChanged += Refresh;
        BuildManager.Instance.OnQuickslotsChanged += Refresh;
        BuildManager.Instance.OnBuildModeChanged += OnVisibilityStateChanged;
        BuildManager.Instance.OnBuildModeChanged += OnBuildModeChanged;
        GameManager.Instance.OnMachineMenuChanged += OnVisibilityStateChanged;

        Refresh();
        UpdateVisibility();
    }

    private void OnDestroy()
    {
        if (BuildManager.Instance != null)
        {
            BuildManager.Instance.OnSelectedChanged -= Refresh;
            BuildManager.Instance.OnQuickslotsChanged -= Refresh;
            BuildManager.Instance.OnBuildModeChanged -= OnVisibilityStateChanged;
            BuildManager.Instance.OnBuildModeChanged -= OnBuildModeChanged;
        }
        if (GameManager.Instance != null) GameManager.Instance.OnMachineMenuChanged -= OnVisibilityStateChanged;
    }

    // Unscaled so the pulse keeps going while the game is paused
    private void Update()
    {
        if (!pulsing) return;

        float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed));
        foreach (QuickslotSlotUI slot in slots) slot.SetPulse(true, alpha);
    }

    private void OnVisibilityStateChanged(bool _) => UpdateVisibility();

    // The Selected Indicator depends on build mode
    private void OnBuildModeChanged(bool _) => Refresh();

    // Shown while the machine menu is open or in build mode
    private void UpdateVisibility()
    {
        bar.SetActive(GameManager.Instance.IsMachineMenuOpen || GameManager.Instance.IsBuildMode);
    }

    // A slot shows as selected only while build mode is on and it holds the held machine
    private void Refresh()
    {
        BuildManager buildManager = BuildManager.Instance;
        bool buildMode = GameManager.Instance.IsBuildMode;
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].UpdateVisual(buildManager.GetQuickslot(i), buildMode && i == buildManager.SelectedQuickslot);
        }
    }

    // Pulses every slot while the menu waits for a slot to be picked
    public void SetPulsing(bool on)
    {
        pulsing = on;
        if (!on) foreach (QuickslotSlotUI slot in slots) slot.SetPulse(false, 0f);
    }

    // While the machine menu is open the menu decides what a click does (display-only outside assign mode).
    // Empty slots do nothing when selecting.
    public void OnSlotClicked(int index)
    {
        if (GameManager.Instance.IsMachineMenuOpen)
        {
            OnSlotClickedInMenu?.Invoke(index);
            return;
        }
        BuildManager.Instance.SelectQuickslot(index);
    }
}
