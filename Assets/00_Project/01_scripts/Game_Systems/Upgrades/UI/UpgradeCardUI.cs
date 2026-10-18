using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary> One upgrade card: icon, title, description, level pips and a buy button. </summary>
public class UpgradeCardUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private Button buyButton;
    [SerializeField] private Image buyButtonImage;
    [SerializeField] private TMP_Text buyLabel;

    [Tooltip("Pips are spawned here, one per level.")]
    [SerializeField] private Transform pipsRoot;
    [SerializeField] private Image pipPrefab;

    [Header("Button")]
    [SerializeField] private string priceFormat = "{0}$";
    [SerializeField] private string maxLabel = "MAX";
    [SerializeField] private Color affordableColor = new Color32(0x3C, 0xE0, 0x2E, 0xFF);
    [SerializeField] private Color unaffordableColor = new Color32(0x6E, 0x72, 0x86, 0xFF);
    [SerializeField] private Color maxedColor = new Color32(0xFF, 0x8A, 0x1E, 0xFF);

    [Header("Pips")]
    [SerializeField] private Color pipOwnedColor = new Color32(0xFF, 0xD2, 0x3C, 0xFF);
    [SerializeField] private Color pipEmptyColor = new Color32(0x1A, 0x1C, 0x28, 0xFF);

    [Header("Buy Punch")]
    [SerializeField] private Vector3 buyPunchScale = new Vector3(0.15f, 0.15f, 0f);
    [SerializeField] private float buyPunchDuration = 0.25f;

    private readonly List<Image> _pips = new List<Image>();
    private UpgradeSO _upgrade;
    private Tween _punch;

    /// <summary> Upgrade this card shows. </summary>
    public UpgradeSO Upgrade => _upgrade;

    /// <summary> Buy button of this card. </summary>
    public Button BuyButton => buyButton;

    private void Awake()
    {
        if (buyButton != null) buyButton.onClick.AddListener(Buy);
    }

    private void OnEnable()
    {
        CoinsManager.OnCoinsChanged += HandleCoinsChanged;
        Localizer.LanguageChanged += Relocalize;
        Relocalize();
    }

    private void OnDisable()
    {
        _punch?.Complete();
        _punch?.Kill();
        CoinsManager.OnCoinsChanged -= HandleCoinsChanged;
        Localizer.LanguageChanged -= Relocalize;
    }

    /// <summary> Redraws the title and the state-dependent texts. </summary>
    private void Relocalize()
    {
        if (_upgrade != null && title != null) title.SetLocalized(_upgrade.DisplayName);
        Refresh();
    }

    private void OnDestroy()
    {
        if (_upgrade != null) _upgrade.OnLevelChanged -= HandleLevelChanged;
        if (buyButton != null) buyButton.onClick.RemoveListener(Buy);
    }

    /// <summary> Shows an upgrade: static info, pips, then current state. </summary>
    public void Bind(UpgradeSO upgrade)
    {
        if (_upgrade != null) _upgrade.OnLevelChanged -= HandleLevelChanged;
        _upgrade = upgrade;
        if (_upgrade == null) return;

        _upgrade.OnLevelChanged += HandleLevelChanged;
        if (icon != null) icon.sprite = _upgrade.Icon;
        if (title != null) title.SetLocalized(_upgrade.DisplayName);
        BuildPips();
        Refresh();
    }

    /// <summary> Updates the description, button (price / grey / MAX) and pip fill. </summary>
    public void Refresh()
    {
        if (_upgrade == null) return;

        if (description != null) description.SetLocalized(_upgrade.GetDescription());

        bool maxed = _upgrade.IsMaxed;
        bool canBuy = UpgradeManager.Instance != null && UpgradeManager.Instance.CanPurchase(_upgrade);

        if (buyLabel != null) buyLabel.SetLocalized(maxed ? Localizer.FromEnglish("upgrade", maxLabel) : string.Format(priceFormat, _upgrade.NextCost));
        var theme = ToyShop.UI.ToyShopTheme.Current;
        if (buyButtonImage != null) buyButtonImage.color = maxed ? theme.gold : canBuy ? theme.blue : theme.muted;
        if (buyLabel != null) buyLabel.color = canBuy ? Color.white : theme.navy;
        if (buyButton != null) buyButton.interactable = canBuy;

        int owned = _upgrade.Level;
        for (int i = 0; i < _pips.Count; i++) _pips[i].color = i < owned ? theme.mint : theme.muted;
    }

    /// <summary> Buys the next level through the manager; punches on success. </summary>
    private void Buy()
    {
        if (UpgradeManager.Instance == null || !UpgradeManager.Instance.TryPurchase(_upgrade)) return;

        // Reward motion belongs to the card; the button has its own press feedback.
        _punch?.Complete();
        _punch = transform.DOPunchScale(new Vector3(.025f, .025f, 0), .2f, 2, 0.2f)
            .SetUpdate(true) // the panel pauses time
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }

    /// <summary> One pip per level; hidden for single-level upgrades. </summary>
    private void BuildPips()
    {
        foreach (Image pip in _pips) if (pip != null) Destroy(pip.gameObject);
        _pips.Clear();
        if (pipsRoot == null || pipPrefab == null) return;

        int count = _upgrade.MaxLevel;
        pipsRoot.gameObject.SetActive(count > 1);
        if (count <= 1) return;

        for (int i = 0; i < count; i++)
        {
            Image pip = Instantiate(pipPrefab, pipsRoot);
            pip.gameObject.SetActive(true);
            _pips.Add(pip);
        }
    }

    private void HandleLevelChanged(UpgradeSO upgrade) => Refresh();

    private void HandleCoinsChanged(int coins) => Refresh();
}
