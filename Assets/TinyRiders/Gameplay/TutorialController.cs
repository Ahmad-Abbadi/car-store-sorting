using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>Small, scene-local lesson driven by real inventory and shelf occupancy.</summary>
public sealed partial class TutorialController : MonoBehaviour
{
    public enum Lesson { Approach, Pickup, Match, Practice, Complete, Place }
    public static event System.Action<TutorialController> Started;
    public static event System.Action<TutorialController, Lesson> StepReached;
    public static event System.Action<TutorialController, bool> Finished;
    private readonly HashSet<Lesson> reachedLessons = new();
    public Player player;
    public SortingShelf[] shelves;
    public SortableItem_SO firstCar, secondCar;
    public GameObject extraCars;
    public Transform pickupPoint;
    public TutorialGuidancePresenter guidance;
    public TMP_Text instruction, controlHint, progress;
    public ParticleSystem celebration;
    public UnityEngine.UI.Button skipButton;
    public PauseMenu pauseMenu;
    public Lesson Step { get; private set; }
    public int CorrectCount { get; private set; }
    public Transform GuidanceTarget { get; private set; }
    public TutorialGuidanceAction GuidanceAction { get; private set; }
    public float IdleSeconds => idle;
    private bool revealed, leaving, celebrated, dirty = true;
    private float idle, recovery;
    private string lastText;
    private WorldItemBase nextCar;

    public bool ReadyForGameplay => CorrectCount == 3 && shelves.All(s =>
        Enumerable.Range(0, s.SpotCount).All(i => s.GetOccupant(i) && s.GetOccupant(i).IsSettled));

    private void OnEnable()
    {
        StashHandler.StashChanged += OnStash;
        SortingShelf.OnRightItemPlaced += OnCorrectShelf;
        SortingShelf.OnRightItemRemoved += OnShelf;
        SortingShelf.OnWrongItemPlaced += OnShelf;
        SortingShelf.OnWrongItemRemoved += OnShelf;
        Localizer.LanguageChanged += Refresh;
        InputIcons.Changed += Refresh;
        if (skipButton) skipButton.onClick.AddListener(Skip);
    }

    private void OnDisable()
    {
        StashHandler.StashChanged -= OnStash;
        SortingShelf.OnRightItemPlaced -= OnCorrectShelf;
        SortingShelf.OnRightItemRemoved -= OnShelf;
        SortingShelf.OnWrongItemPlaced -= OnShelf;
        SortingShelf.OnWrongItemRemoved -= OnShelf;
        Localizer.LanguageChanged -= Refresh;
        InputIcons.Changed -= Refresh;
        if (skipButton) skipButton.onClick.RemoveListener(Skip);
        ClearStoreHighlight();
    }

    private void Start()
    {
        if (inStore) { StartCoroutine(BeginInStore()); return; }
        Started?.Invoke(this);
        PublishStep();
        player.StashHandler.Inventory.SetCapacity(3);
        SortingShelf.SetDistanceShelfingEnabled(false);
        Refresh();
    }

    private void OnStash(IReadOnlyList<ItemDataSO> items)
    {
        if (Player.Instance == player) { dirty = true; idle = 0; }
    }
    private void OnShelf(SortingShelf shelf, SortableItem_SO item)
    {
        if (shelves != null && System.Array.IndexOf(shelves, shelf) >= 0) { dirty = true; idle = 0; }
        if (inStore && storeRunning && storeHeld == item && !shelf.IsCorrectItem(item)) storeWrongShelf = shelf;
    }
    private void OnCorrectShelf(SortingShelf shelf, SortableItem_SO item)
    {
        OnShelf(shelf, item);
        if (inStore && storeRunning && shelf.gameObject.scene == gameObject.scene && shelf.IsCorrectItem(item))
            storeCorrectPlacement = true;
    }
    private void Refresh() { lastText = null; dirty = true; }

    private void Update()
    {
        if (inStore) { UpdateInStore(); return; }
        if (leaving || !player || Time.timeScale <= 0 || !player.Input.IsEnabled)
        { if (guidance) guidance.Hide(); return; }
        idle += Time.deltaTime;
        recovery += Time.deltaTime;
        if (recovery > 1) { recovery = 0; RecoverCars(); }
        // Shelf events fire before the placement transaction finishes. Read in Update.
        if (dirty) { dirty = false; Evaluate(); }
        if (Step == Lesson.Approach && Vector3.Distance(Flat(player.transform.position), Flat(pickupPoint.position)) < 1.7f)
            SetStep(Lesson.Pickup);
        GuidanceTarget = ResolveGuidanceTarget(out var action);
        GuidanceAction = action;
        string key = action == TutorialGuidanceAction.TakeBack ? "tutorial.correct" : "tutorial." + Step.ToString().ToLowerInvariant();
        string text = Localizer.Get(key);
        string hint = guidance ? guidance.ControlHint(action) : "";
        if (lastText != text + hint + CorrectCount)
        {
            instruction.SetLocalized(text);
            controlHint.SetLocalized(hint);
            progress.SetLocalized(Localizer.Format("tutorial.progress", CorrectCount));
            lastText = text + hint + CorrectCount;
        }
        if (ReadyForGameplay) Leave(false);
    }

    public Transform ResolveGuidanceTarget(out TutorialGuidanceAction action)
    {
        action = TutorialGuidanceAction.None;
        if (leaving || Step == Lesson.Complete || !player) return null;
        foreach (var shelf in shelves)
            for (int i = 0; i < shelf.SpotCount; i++)
            {
                var placed = shelf.GetOccupant(i);
                if (placed && !shelf.IsCorrectItem(placed.Data))
                { action = TutorialGuidanceAction.TakeBack; return placed.transform; }
            }
        var held = player.ItemHandler.CurrentEquipped;
        if (held && held.Data is SortableItem_SO data)
        {
            foreach (var shelf in shelves)
                if (shelf.IsCorrectItem(data))
                    for (int i = 0; i < shelf.SpotCount; i++)
                        if (shelf.IsSpotFree(i))
                        { action = TutorialGuidanceAction.Place; return shelf.GetSpot(i); }
            return null;
        }
        if (!IsPracticeCar(nextCar))
        {
            nextCar = null;
            float nearest = float.PositiveInfinity;
            foreach (var car in WorldItemBase.All)
            {
                if (!IsPracticeCar(car)) continue;
                float distance = (car.transform.position - player.transform.position).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance; nextCar = car;
            }
        }
        if (!nextCar) return null;
        action = Step == Lesson.Approach ? TutorialGuidanceAction.Approach : TutorialGuidanceAction.Pickup;
        return nextCar.transform;
    }

    private bool IsPracticeCar(WorldItemBase car) => car && car.isActiveAndEnabled &&
        car.gameObject.scene == gameObject.scene && (car.Data == firstCar || car.Data == secondCar);

    private static Vector3 Flat(Vector3 p) => new Vector3(p.x, 0, p.z);

    public void Evaluate()
    {
        CorrectCount = shelves.Sum(s => Enumerable.Range(0, s.SpotCount).Count(i => s.GetOccupant(i) && s.IsCorrectItem(s.GetOccupant(i).Data)));
        if (CorrectCount >= 1 && !revealed)
        {
            revealed = true;
            extraCars.SetActive(true);
        }
        if (CorrectCount == 3)
        {
            SetStep(Lesson.Complete);
            TutorialProgress.Finish(false);
            if (!celebrated)
            {
                celebrated = true;
                celebration.Play();
                var audio = SimpleAudioManager.instance;
                if (audio && audio.GameplaySounds) audio.PlayGlobal(audio.GameplaySounds.CompleteShelfSFX);
            }
        }
        else if (revealed) SetStep(Lesson.Practice);
        else if (player.StashHandler.Inventory.Count > 0) SetStep(Lesson.Match);
        else if (Step == Lesson.Match) SetStep(Lesson.Pickup);
    }

    private void SetStep(Lesson step)
    {
        if (Step == step) return;
        Step = step; idle = 0; lastText = null;
        PublishStep();
    }

    private void PublishStep()
    {
        if (reachedLessons.Add(Step)) StepReached?.Invoke(this, Step);
    }

    private void RecoverCars()
    {
        foreach (var car in WorldItemBase.All.ToArray())
        {
            if (!car || car.gameObject.scene != gameObject.scene) continue;
            var p = car.transform.position;
            if (p.y >= -.5f && p.y < 2.8f && Mathf.Abs(p.x) < 2.8f && Mathf.Abs(p.z) < 2.8f) continue;
            car.transform.position = pickupPoint.position;
            if (car.Body)
            {
                car.Body.position = car.transform.position;
                if (!car.Body.isKinematic) { car.Body.linearVelocity = Vector3.zero; car.Body.angularVelocity = Vector3.zero; }
            }
        }
        if (player.transform.position.y < -1)
            player.Motor.SetPosition(new Vector3(0, .1f, -2.2f));
    }

    public void Skip() { if (inStore) FinishInStore(true); else Leave(true); }
    private void Leave(bool skip)
    {
        if (leaving || SceneLoader.IsLoading) return;
        leaving = true;
        TutorialProgress.Finish(skip);
        Finished?.Invoke(this, skip);
        if (pauseMenu) pauseMenu.Close();
        SceneLoader.LoadWithLoadingScreen(TutorialProgress.StoreScene);
    }
}
