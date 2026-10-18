using System;
using System.Collections.Generic;
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

    [Serializable] private sealed class WalletSave
    {
        public int version = 1, coins;
        public List<string> receipts = new List<string>();
    }
    private WalletSave wallet = new WalletSave();
    private string WalletKey => saveKey + "_RewardWallet";
    private bool walletReadable = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Coins = Mathf.Max(0, PlayerPrefs.GetInt(saveKey, 0));
        if (PlayerPrefs.HasKey(WalletKey))
        {
            try
            {
                var saved = JsonUtility.FromJson<WalletSave>(PlayerPrefs.GetString(WalletKey));
                if (saved == null || saved.version != 1 || saved.receipts == null) throw new InvalidOperationException("Unsupported wallet save");
                wallet = saved; Coins = Mathf.Max(0, wallet.coins);
            }
            catch (Exception e) { walletReadable = false; Debug.LogWarning("Reward wallet retained without modification: " + e.Message); }
        }
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current?.f10Key.wasPressedThisFrame == true)
            DebugAddCoins();
    }

    /// <summary> F10 test cheat: grants 10,000 coins through the normal saved wallet and UI events. </summary>
    public void DebugAddCoins()
    {
        SetCoins(Coins + Mathf.Min(10000, int.MaxValue - Coins));
    }
#endif

    /// <summary> True when the balance covers the amount. </summary>
    public bool CanAfford(int amount) => amount >= 0 && Coins >= amount;

    /// <summary> Adds coins to the balance. </summary>
    public void Add(int amount)
    {
        if (amount > 0) SetCoins(Coins + Math.Min(amount, int.MaxValue - Coins));
    }

    /// <summary>Balance and receipt share one persisted record; replayed claims never pay twice.</summary>
    public bool AddRewardOnce(string claimId, int amount)
    {
        if (!walletReadable || string.IsNullOrEmpty(claimId) || amount < 0) return false;
        if (wallet.receipts.Contains(claimId)) return true;
        wallet.receipts.Add(claimId);
        Coins += Math.Min(amount, int.MaxValue - Coins);
        PersistWallet();
        OnCoinsChanged?.Invoke(Coins);
        return true;
    }

    private void PersistWallet()
    {
        if (!walletReadable) return;
        wallet.coins = Coins;
        PlayerPrefs.SetString(WalletKey, JsonUtility.ToJson(wallet));
        PlayerPrefs.SetInt(saveKey, Coins); // legacy readers and existing tools
        PlayerPrefs.Save();
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
        PersistWallet();
        OnCoinsChanged?.Invoke(Coins);
    }

    private void HandleRightItemPlaced(SortingShelf shelf, SortableItem_SO data) => Add(coinsPerRightItem);

    private void HandleRightItemRemoved(SortingShelf shelf, SortableItem_SO data) => Remove(coinsPerRightItem);

    /// <summary>Clears the wallet when a completed store is replayed.</summary>
    public void ResetForReplay()
    {
        Coins = 0;
        PersistWallet();
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
