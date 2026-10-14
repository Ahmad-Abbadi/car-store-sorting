using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Base upgrade data: shop info, per-level costs and the saved owned level. </summary>
public abstract class UpgradeSO : ScriptableObject
{
    private const string SaveKeyPrefix = "Upgrade_";

    [Header("Shop")]
    [Tooltip("PlayerPrefs key suffix. Keep it stable once players have saves.")]
    [SerializeField] private string saveId;

    [SerializeField] private string displayName;

    [SerializeField, TextArea(2, 4)] private string description;

    [SerializeField] private Sprite icon;

    /// <summary> Fired with this upgrade whenever its owned level changes. </summary>
    public event Action<UpgradeSO> OnLevelChanged;

    [NonSerialized] private int _level;
    [NonSerialized] private bool _loaded;

    public string SaveId => saveId;

    /// <summary> Localization key of the name: upgrade.SaveId.name. </summary>
    public string NameKey => "upgrade." + saveId + ".name";

    /// <summary> Localization key of the description: upgrade.SaveId.desc. </summary>
    public string DescriptionKey => "upgrade." + saveId + ".desc";

    /// <summary> Localized name; falls back to the typed name. </summary>
    public string DisplayName => Localizer.GetOr(NameKey, displayName);

    /// <summary> Localized description; falls back to the typed text. </summary>
    public string Description => Localizer.GetOr(DescriptionKey, description);

    /// <summary> Name typed in the Inspector, used as the English source text. </summary>
    public string SourceName => displayName;

    /// <summary> Description typed in the Inspector, used as the English source text. </summary>
    public string SourceDescription => description;
    public Sprite Icon => icon;

    /// <summary> Owned level; 0 means nothing bought yet. </summary>
    public int Level
    {
        get
        {
            if (!_loaded) Reload();
            return _level;
        }
    }

    /// <summary> Number of purchasable levels. </summary>
    public abstract int MaxLevel { get; }

    /// <summary> True once every level is owned. </summary>
    public bool IsMaxed => Level >= MaxLevel;

    /// <summary> Cost of the next level, or 0 when maxed. </summary>
    public int NextCost => IsMaxed ? 0 : GetCost(Level + 1);

    /// <summary> Cost to buy the given 1-based level; 0 if out of range. </summary>
    public abstract int GetCost(int level);

    /// <summary> Description with {value} filled by the next level's value (max level once maxed). </summary>
    public string GetDescription()
    {
        string text = Description ?? string.Empty;
        return text.Contains("{value}") ? text.Replace("{value}", FormatValue(Mathf.Clamp(Level + 1, 1, Mathf.Max(1, MaxLevel)))) : text;
    }

    /// <summary> Player-facing text of a level's key value; fills {value}. </summary>
    protected virtual string FormatValue(int level) => string.Empty;

    private string SaveKey => SaveKeyPrefix + saveId;

    /// <summary> Sets, saves and broadcasts the owned level. </summary>
    public void SetLevel(int level)
    {
        level = Mathf.Clamp(level, 0, MaxLevel);
        if (level == Level) return;

        _level = level;
        PlayerPrefs.SetInt(SaveKey, _level);
        PlayerPrefs.Save();
        OnLevelChanged?.Invoke(this);
    }

    /// <summary> Back to level 0 and deletes the saved level. </summary>
    public void ResetLevel()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        bool changed = _loaded && _level != 0;
        _level = 0;
        _loaded = true;
        if (changed) OnLevelChanged?.Invoke(this);
    }

    /// <summary> Re-reads the owned level from PlayerPrefs. </summary>
    public void Reload()
    {
        _level = Mathf.Clamp(PlayerPrefs.GetInt(SaveKey, 0), 0, MaxLevel);
        _loaded = true;
    }

    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(saveId)) saveId = name;
    }
}

/// <summary> One purchasable level: its cost and the values it grants. </summary>
[Serializable]
public struct UpgradeLevel<TValues> where TValues : struct
{
    [Min(0)] public int cost;
    public TValues values;
}

/// <summary> Upgrade whose base and per-level values use one values struct. </summary>
public abstract class UpgradeSO<TValues> : UpgradeSO where TValues : struct
{
    [Header("Values")]
    [Tooltip("Values before any level is bought.")]
    [SerializeField] private TValues baseValues;

    [Tooltip("One entry per purchasable level, in buy order.")]
    [SerializeField] private List<UpgradeLevel<TValues>> levels = new List<UpgradeLevel<TValues>>();

    public override int MaxLevel => levels.Count;

    public override int GetCost(int level) =>
        level >= 1 && level <= levels.Count ? levels[level - 1].cost : 0;

    /// <summary> Values at a level; 0 returns the base values. </summary>
    public TValues GetValues(int level)
    {
        if (level <= 0 || levels.Count == 0) return baseValues;
        return levels[Mathf.Min(level, levels.Count) - 1].values;
    }

    /// <summary> Text of the key value at a level, from FormatValues. </summary>
    protected sealed override string FormatValue(int level) => FormatValues(GetValues(level));

    /// <summary> Player-facing text of one level's key value. </summary>
    protected virtual string FormatValues(TValues values) => string.Empty;

    /// <summary> Values at the owned level. </summary>
    public TValues CurrentValues => GetValues(Level);
}
