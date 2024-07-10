using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MoneyManager : MonoBehaviour
{

    public static MoneyManager Instance { get; private set; }

    private int moneyValue = 0;

    private void Awake()
    {  
        Instance = this;
    }

    public int GetMoneyCount()
    {
        return moneyValue;
    }

    public void IncreaseMoney(int amount)
    {
        moneyValue += amount;
    }

    public bool CheckIfEnoughMoney(int amount)
    {
        return amount <= moneyValue ? true:false;
    }

    public void DecreaseMoney(int amount)
    {
        moneyValue -= amount;
    }
}
