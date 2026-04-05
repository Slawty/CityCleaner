using UnityEngine;
using System;

public enum RessourceType { Coin, Poop };

public class InventoryManager : MonoBehaviour
{
    public int StartCoins = 0;
    int coins = 0;
    public int Coins => coins;
    public int StartPoop = 0;
    int poop = 0;
    public int Poop => poop;

    void Start()
    {
        IncreaseCoins(StartCoins);
        IncreasePoop(StartPoop);
    }

    public void IncreaseCoins(int value)
    {
        coins += value;
        Managers.UI.SetCoinValue(coins);
    }

    public void DecreaseCoins(int value)
    {
        coins -= value;
        Managers.UI.SetCoinValue(coins);
    }

    public bool HasEnoughCoins(int value)
    {
        return coins >= value;
    }

    public void IncreasePoop(int value)
    {
        poop += value;
        Managers.UI.SetPoopValue(poop);
    }

    public void DecreasePoop(int value)
    {
        poop -= value;
        Managers.UI.SetPoopValue(poop);
    }

    public bool HasEnoughPoop(int value)
    {
        return poop >= value;
    }
}
