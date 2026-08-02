using UnityEngine;
using System;

public enum RessourceType { Coin, Poop, Dirt };

public class InventoryManager : MonoBehaviour
{
    public int StartCoins = 0;
    int coins = 0;
    public int Coins => coins;
    public int StartPoop = 0;
    int poop = 0;
    public int Poop => poop;
    public int StartDirt = 0;
    int dirt = 0;
    public int Dirt => dirt;

    void Start()
    {
        IncreaseCoins(StartCoins);
        IncreasePoop(StartPoop);
        IncreaseDirt(StartDirt);
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

    public void IncreaseDirt(int value)
    {
        dirt += value;
    }

    public void DecreaseDirt(int value)
    {
        dirt -= value;
    }

    public bool HasEnoughDirt(int value)
    {
        return dirt >= value;
    }
}
