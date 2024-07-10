using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MoneyVisual : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyValueText;

    private void Update()
    {
        moneyValueText.text = MoneyManager.Instance.GetMoneyCount().ToString();
    }
}
