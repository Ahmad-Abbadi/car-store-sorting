using System;
using UnityEngine;

/// <summary> Scene coin wallet: earns from right shelf placements, saved in PlayerPrefs. </summary>
public class CoinsManager : MonoBehaviour
{
    public static CoinsManager Instance { get; private set; }

    [Tooltip("Coins given per right item placed, taken back when removed.")]
    [SerializeField, Min(0)] private int coinsPerRightItem = 1;

    [Tooltip("PlayerPrefs key the balance is saved under.")]
    [SerializeField] private string saveKey = "Coins";

    /// <summary> Fired with the new balance whenever it changes. </summary>
    public static event Action<int> OnCoinsChanged;

    /// <summary> Current balance; never below zero. </summary>
    public int Coins { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Coins = Mathf.Max(0, PlayerPrefs.GetInt(saveKey, 0));
    }

    private void OnEnable()
    {
        SortingShelf.OnRightItemPlaced += HandleRightItemPlaced;
        SortingShelf.OnRightItemRemoved += HandleRightItemRemoved;
    }

    private void OnDisable()
    {
        SortingShelf.OnRightItemPlaced -= HandleRightItemPlaced;
        SortingShelf.OnRightItemRemoved -= HandleRightItemRemoved;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary> True when the balance covers the amount. </summary>
    public bool CanAfford(int amount) => amount >= 0 && Coins >= amount;

    /// <summary> Adds coins to the balance. </summary>
    public void Add(int amount)
    {
        if (amount > 0) SetCoins(Coins + amount);
    }

    /// <summary> Removes coins, stopping at zero. </summary>
    public void Remove(int amount)
    {
        if (amount > 0) SetCoins(Coins - amount);
    }

    /// <summary> Spends coins only if affordable. False when refused. </summary>
    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount)) return false;

        SetCoins(Coins - amount);
        return true;
    }

    /// <summary> Clamps, saves and broadcasts the new balance. </summary>
    private void SetCoins(int value)
    {
        value = Mathf.Max(0, value);
        if (value == Coins) return;

        Coins = value;
        PlayerPrefs.SetInt(saveKey, Coins);
        PlayerPrefs.Save();
        OnCoinsChanged?.Invoke(Coins);
    }

    private void HandleRightItemPlaced(SortingShelf shelf, SortableItem_SO data) => Add(coinsPerRightItem);

    private void HandleRightItemRemoved(SortingShelf shelf, SortableItem_SO data) => Remove(coinsPerRightItem);

    /// <summary>Clears the wallet when a completed store is replayed.</summary>
    public void ResetForReplay()
    {
        Coins = 0;
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
        OnCoinsChanged?.Invoke(Coins);
    }

#if UNITY_EDITOR
    /// <summary> Test-only: wipes the saved balance to zero. </summary>
    [ContextMenu("Reset Saved Coins")]
    private void ResetSavedCoins()
    {
        PlayerPrefs.DeleteKey(saveKey); // also works in edit mode
        SetCoins(0);
    }
#endif
}
