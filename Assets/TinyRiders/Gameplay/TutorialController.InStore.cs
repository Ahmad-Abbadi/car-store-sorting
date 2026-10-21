using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The existing tutorial controller also teaches the first real store interaction.
public sealed partial class TutorialController
{
    [SerializeField] private bool inStore;
    public bool InStore => inStore;
    private bool storeRunning, storeCorrectPlacement;
    private float nextStoreSearch, matchStarted;
    private SortableItem_SO storeHeld;
    private SortingShelf storeWrongShelf;
    private InteractionHandler storeInteraction;
    private Camera storeView;
    private readonly List<WorldItemBase> storeCandidates = new(8);
    private readonly List<object> storeOutlineOwners = new();

    private IEnumerator BeginInStore()
    {
        if (guidance) guidance.canvas.enabled = false;
        if (skipButton) skipButton.gameObject.SetActive(false);
        var session = TinyRiders.ThemedCollectionSession.Instance;
        var save = FindFirstObjectByType<LevelSaveManager>();
        bool replay = TutorialProgress.ConsumeReplay();
        // A saved store is returning progress, even if it predates the tutorial flag.
        if (!session || (!replay && (PlayerPrefs.HasKey(TutorialProgress.StatusKey) ||
            PlayerPrefs.HasKey(save ? save.StorageKey : "LevelSave"))))
        {
            if (session && save && save.HasRestoredProgress)
            {
                while (!session.IsReady || SceneLoader.IsLoading) yield return null;
                yield return WelcomeBack();
            }
            enabled = false;
            yield break;
        }
        while (!session.IsReady || SceneLoader.IsLoading) yield return null;
        player = Player.Instance;
        if (!player || !guidance) { enabled = false; yield break; }
        storeInteraction = player.GetComponent<InteractionHandler>();
        storeView = storeInteraction ? storeInteraction.SourceCamera : Camera.main;
        if (!storeView) { enabled = false; yield break; }
        shelves = FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
        storeRunning = true;
        if (skipButton) skipButton.gameObject.SetActive(true);
        guidance.canvas.enabled = true;
        SetStep(Lesson.Pickup);
        Started?.Invoke(this);
        UpdateInStore();
        LogStoreStage("Gameplay interactive; first-pickup guidance ready.");
    }

    // Reuse the upper-center lesson card and its font; no new UI hierarchy or input blocking.
    private IEnumerator WelcomeBack()
    {
        if (!guidance || !guidance.instructionCard || !instruction) yield break;
        guidance.Hide();
        instruction.SetLocalized(Localizer.GetOr("store.welcome_back", "Welcome back! Continue sorting."));
        instruction.rectTransform.anchoredPosition = Vector2.zero;
        if (controlHint) controlHint.gameObject.SetActive(false);
        if (progress) progress.gameObject.SetActive(false);
        var group = guidance.instructionCard.GetComponent<CanvasGroup>();
        if (!group) group = guidance.instructionCard.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        guidance.canvas.enabled = true;
        for (float t = 0; t < 2f; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Min(Mathf.Clamp01(t / .2f), Mathf.Clamp01((2f - t) / .2f));
            guidance.canvas.enabled = Time.timeScale > 0 && !(pauseMenu && pauseMenu.Panel && pauseMenu.Panel.activeSelf);
            yield return null;
        }
        guidance.canvas.enabled = false;
    }

    private void UpdateInStore()
    {
        if (!storeRunning) return;
        bool visible = !SceneLoader.IsLoading && player && player.Input.IsEnabled && Time.timeScale > 0 &&
            !(pauseMenu && pauseMenu.Panel && pauseMenu.Panel.activeSelf);
        guidance.canvas.enabled = visible;
        if (!visible) { guidance.Hide(); ClearStoreHighlight(); return; }
        if (storeCorrectPlacement)
        {
            CorrectCount = 1;
            LogStoreStage("First correct placement; guidance complete.");
            FinishInStore(false);
            return;
        }
        idle += Time.deltaTime;
        var held = player.ItemHandler.CurrentEquipped;
        var data = held ? held.Data as SortableItem_SO : null;
        if (storeHeld != data)
        {
            storeHeld = data;
            nextStoreSearch = 0;
            if (data)
            {
                storeWrongShelf = null;
                matchStarted = Time.unscaledTime;
                SetStep(Lesson.Match);
                LogStoreStage("Car picked up; matching shelf guidance ready.");
            }
            else SetStep(Lesson.Pickup);
        }
        // Population and shelf searches run at most twice a second, never every frame.
        if (dirty || Time.unscaledTime >= nextStoreSearch)
        {
            dirty = false;
            nextStoreSearch = Time.unscaledTime + .5f;
            ClearStoreHighlight();
            GuidanceTarget = null;
            GuidanceAction = TutorialGuidanceAction.None;
            if (data)
            {
                GuidanceTarget = FindStoreDestination(data);
                GuidanceAction = GuidanceTarget ? TutorialGuidanceAction.Place : TutorialGuidanceAction.None;
            }
            else
            {
                if (storeWrongShelf)
                    for (int i = 0; i < storeWrongShelf.SpotCount; i++)
                    {
                        var placed = storeWrongShelf.GetOccupant(i);
                        if (!placed || storeWrongShelf.IsCorrectItem(placed.Data)) continue;
                        GuidanceTarget = placed.transform;
                        GuidanceAction = TutorialGuidanceAction.TakeBack;
                        break;
                    }
                nextCar = GuidanceTarget ? null : FindAccessibleStoreCar();
                if (nextCar)
                {
                    GuidanceTarget = nextCar.transform;
                    GuidanceAction = TutorialGuidanceAction.Pickup;
                    var group = nextCar.GetComponent<ItemVisualGroup>();
                    if (group)
                        foreach (var renderer in group.Renderers)
                        {
                            if (!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                            var owner = new object();
                            storeOutlineOwners.Add(owner);
                            SharedOutline.Show(owner, renderer, new Color(1f, .76f, .2f), 2f);
                        }
                }
            }
        }
        if (!data && GuidanceTarget && GuidanceAction != TutorialGuidanceAction.TakeBack)
        {
            float reach = storeInteraction ? storeInteraction.MaxDistance : 3f;
            GuidanceAction = (storeView.transform.position - GuidanceTarget.position).sqrMagnitude <= reach * reach
                ? TutorialGuidanceAction.Pickup : TutorialGuidanceAction.Approach;
        }
        if (data && GuidanceTarget && Time.unscaledTime - matchStarted >= .6f)
        {
            var projected = storeView.WorldToViewportPoint(GuidanceTarget.position);
            float reach = storeInteraction ? storeInteraction.MaxDistance : 3f;
            bool nearby = (storeView.transform.position - GuidanceTarget.position).sqrMagnitude <= (reach + .5f) * (reach + .5f);
            var aimed = storeInteraction && storeInteraction.Current.GO
                ? storeInteraction.Current.GO.GetComponentInParent<SortingShelf>() : null;
            SetStep(nearby && aimed == GuidanceTarget.GetComponentInParent<SortingShelf>() && aimed &&
                aimed.IsCorrectItem(data) && aimed.HasFreeSpot &&
                projected.z > 0 && projected.x > 0 && projected.x < 1 && projected.y > 0 && projected.y < 1
                ? Lesson.Place : Lesson.Match);
        }
        string key = GuidanceAction == TutorialGuidanceAction.TakeBack ? "tutorial.correct" :
            Step == Lesson.Pickup ? "tutorial.store.pickup" : Step == Lesson.Place ? "tutorial.store.place" : "tutorial.store.match";
        // During matching, teach navigation; show the placement control only within reach.
        string hint = guidance.ControlHint(Step == Lesson.Match ? TutorialGuidanceAction.Approach : GuidanceAction);
        string text = Localizer.Get(key);
        if (lastText != text + hint)
        {
            instruction.SetLocalized(text);
            controlHint.SetLocalized(hint);
            lastText = text + hint;
        }
    }

    private Transform FindStoreDestination(SortableItem_SO data)
    {
        Transform best = null;
        float distance = float.PositiveInfinity;
        foreach (var shelf in shelves)
        {
            if (!shelf || !shelf.isActiveAndEnabled || !shelf.IsCorrectItem(data)) continue;
            for (int i = 0; i < shelf.SpotCount; i++)
            {
                if (!shelf.CanAccept(data, i, out _)) continue;
                var spot = shelf.GetSpot(i);
                float candidate = (spot.position - player.transform.position).sqrMagnitude;
                if (candidate >= distance) continue;
                best = spot; distance = candidate;
            }
        }
        return best;
    }

    private WorldItemBase FindAccessibleStoreCar()
    {
        storeCandidates.Clear();
        Vector3 eye = storeView.transform.position;
        foreach (var car in WorldItemBase.All)
        {
            if (!car || !car.isActiveAndEnabled || car.gameObject.scene != gameObject.scene ||
                !(car.Data is SortableItem_SO)) continue;
            var screen = storeView.WorldToViewportPoint(car.transform.position);
            if (screen.z <= 0 || screen.x < .05f || screen.x > .95f || screen.y < .05f || screen.y > .95f) continue;
            float distance = (car.transform.position - eye).sqrMagnitude;
            if (distance > 144) continue;
            int index = 0;
            while (index < storeCandidates.Count && (storeCandidates[index].transform.position - eye).sqrMagnitude < distance) index++;
            if (index >= 8) continue;
            storeCandidates.Insert(index, car);
            if (storeCandidates.Count > 8) storeCandidates.RemoveAt(8);
        }
        foreach (var car in storeCandidates)
        {
            var collider = car.GetComponent<Collider>();
            if (!collider) continue;
            Vector3 delta = collider.bounds.center - eye;
            if (Physics.Raycast(eye, delta.normalized, out var hit, delta.magnitude + .1f, ~0, QueryTriggerInteraction.Ignore) &&
                hit.collider.GetComponentInParent<WorldItemBase>() == car && FindStoreDestination((SortableItem_SO)car.Data))
                return car;
        }
        return null;
    }

    private void ClearStoreHighlight()
    {
        foreach (var owner in storeOutlineOwners) SharedOutline.Hide(owner);
        storeOutlineOwners.Clear();
    }

    private void FinishInStore(bool skipped)
    {
        if (!storeRunning) return;
        storeRunning = false;
        ClearStoreHighlight();
        GuidanceTarget = null;
        GuidanceAction = TutorialGuidanceAction.None;
        guidance.Hide();
        guidance.canvas.enabled = false;
        if (skipButton) skipButton.gameObject.SetActive(false);
        SetStep(Lesson.Complete);
        TutorialProgress.Finish(skipped);
        Finished?.Invoke(this, skipped);
        if (pauseMenu) pauseMenu.Close();
        enabled = false;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogStoreStage(string text) => Debug.Log($"[FTUE {Time.realtimeSinceStartup:F2}s] {text}", this);
}
