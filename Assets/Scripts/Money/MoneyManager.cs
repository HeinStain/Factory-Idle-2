using System;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    // oldValue, newValue
    public event Action<int, int> MoneyChanged;

    [SerializeField] private int moneyValue = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public int GetMoneyCount() => moneyValue;

    public void IncreaseMoney(int amount)
    {
        if (amount == 0) return;

        int old = moneyValue;
        moneyValue += amount;

        if (moneyValue < 0) moneyValue = 0;

        if (old != moneyValue)
            MoneyChanged?.Invoke(old, moneyValue);
    }

    public void DecreaseMoney(int amount)
    {
        if (amount == 0) return;

        int old = moneyValue;
        moneyValue -= amount;

        if (moneyValue < 0) moneyValue = 0;

        if (old != moneyValue)
            MoneyChanged?.Invoke(old, moneyValue);
    }

    public bool CheckIfEnoughMoney(int amount) => amount <= moneyValue;
}
