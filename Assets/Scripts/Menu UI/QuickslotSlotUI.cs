using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickslotSlotUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private GameObject highlight;
    [SerializeField] private Image pulseIndicator;     // fades in and out while the menu is in assign mode

    private QuickslotBarUI bar;
    private int index;

    // Called once by the bar. Index is zero-based, shown as its number key (index + 1).
    public void Setup(QuickslotBarUI bar, int index)
    {
        this.bar = bar;
        this.index = index;

        keyText.text = (index + 1).ToString();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => this.bar.OnSlotClicked(this.index));

        SetPulse(false, 0f);
    }

    public void UpdateVisual(MachineData machine, bool selected)
    {
        icon.enabled = machine != null && machine.icon != null;
        if (icon.enabled) icon.sprite = machine.icon;

        highlight.SetActive(selected);
    }

    // Alpha goes through the CanvasRenderer so the Image's own color is left alone
    public void SetPulse(bool active, float alpha)
    {
        pulseIndicator.gameObject.SetActive(active);
        if (active) pulseIndicator.canvasRenderer.SetAlpha(alpha);
    }
}
