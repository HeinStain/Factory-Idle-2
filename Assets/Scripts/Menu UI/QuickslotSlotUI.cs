using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickslotSlotUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private GameObject highlight;     // marks the held machine's slot, and pulses in assign mode

    private QuickslotBarUI bar;
    private int index;
    private CanvasRenderer highlightRenderer;
    private bool selected;
    private bool pulsing;

    // Called once by the bar. Index is zero-based, shown as its number key (index + 1).
    public void Setup(QuickslotBarUI bar, int index)
    {
        this.bar = bar;
        this.index = index;

        keyText.text = (index + 1).ToString();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => this.bar.OnSlotClicked(this.index));

        highlightRenderer = highlight.GetComponent<CanvasRenderer>();
        SetPulse(false, 1f);
    }

    public void UpdateVisual(MachineData machine, bool selected)
    {
        icon.enabled = machine != null && machine.icon != null;
        if (icon.enabled) icon.sprite = machine.icon;

        // Remembered so ending a pulse can fall back to it. A pulsing slot stays lit.
        this.selected = selected;
        highlight.SetActive(selected || pulsing);
    }

    // Alpha goes through the CanvasRenderer so the Image's own color is left alone.
    // Turning the pulse off restores full alpha and the selected state.
    public void SetPulse(bool active, float alpha)
    {
        pulsing = active;
        highlightRenderer.SetAlpha(active ? alpha : 1f);
        highlight.SetActive(selected || pulsing);
    }
}
