using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Standalone upgrade panel; opened by an upgrade box, closed by X or Esc. </summary>
public class UpgradePanelUI : MonoBehaviour
{
    [Tooltip("Shown window; carries the BaseUIPanel that pauses and frees the cursor.")]
    [SerializeField] private GameObject window;

    [SerializeField] private Button closeButton;

    [Tooltip("Parent the cards are spawned under.")]
    [SerializeField] private Transform cardsRoot;

    [SerializeField] private UpgradeCardUI cardPrefab;

    private readonly List<UpgradeCardUI> _cards = new List<UpgradeCardUI>();

    /// <summary> True while the window is shown. </summary>
    public bool IsOpen => window != null && window.activeSelf;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (window != null) window.SetActive(false); // start hidden
    }

    private void OnEnable() => UpgradeBox.OnOpenRequested += Open;

    private void OnDisable()
    {
        UpgradeBox.OnOpenRequested -= Open;
        PauseHandler.RemoveInterceptor(InterceptPause);
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    /// <summary> Builds cards on first open, then shows the window. </summary>
    public void Open()
    {
        if (window == null || IsOpen) return;

        if (_cards.Count == 0) BuildCards();
        window.SetActive(true);
        foreach (UpgradeCardUI card in _cards) card.Refresh();
        PauseHandler.AddInterceptor(InterceptPause);
    }

    /// <summary> Hides the window; the panel manager resumes play. </summary>
    public void Close()
    {
        if (window == null || !IsOpen) return;

        window.SetActive(false);
        PauseHandler.RemoveInterceptor(InterceptPause);
    }

    /// <summary> Esc closes this panel instead of opening the pause menu. </summary>
    private bool InterceptPause()
    {
        if (!IsOpen) return false;
        Close();
        return true;
    }

    /// <summary> One card per upgrade, in the manager's order. </summary>
    private void BuildCards()
    {
        UpgradeManager manager = UpgradeManager.Instance;
        if (manager == null || cardsRoot == null || cardPrefab == null) return;

        foreach (UpgradeSO upgrade in manager.Upgrades)
        {
            UpgradeCardUI card = Instantiate(cardPrefab, cardsRoot);
            card.gameObject.SetActive(true);
            card.Bind(upgrade);
            _cards.Add(card);
        }
    }
}
