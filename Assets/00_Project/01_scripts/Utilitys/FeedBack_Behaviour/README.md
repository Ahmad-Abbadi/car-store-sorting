# Game Feedback

Modular, ScriptableObject-free feedback system. Modules are serialized inline on a
`FeedbackReceiver`, so each receiver keeps its own independent settings — no shared assets.

## Layout
- `Core/` — `IFeedback<T>` base interface, `FeedbackModule` base, `FeedbackContext`, `FeedbackReceiver`.
- `Utilities/` — executors that implement `IFeedback<T>` (`TransformShaker`, `TMPColorFlasher`, `MaterialColorFlasher`) plus their settings structs.
- `Modules/` — the pieces you add on a receiver (`ShakeObjectModule`, `TMPColorFlashModule`, `MaterialColorFlashModule`).
- `Editor/` — custom inspector with an Add Module dropdown and inline settings.

## Use it
1. Add a `FeedbackReceiver` component to a GameObject.
2. Click **Add Module**, pick a module — its settings appear inline. Add as many as you want.
3. Call it from code:

```csharp
[SerializeField] FeedbackReceiver feedback;

feedback.Play();               // plays on the receiver's own GameObject
feedback.Play(someOtherObject); // plays against another source
```

## Call a utility directly (the base interface)
Every executor implements `IFeedback<T>`, so you can skip modules entirely:

```csharp
IFeedback<Transform> shake = new TransformShaker(ShakeSettings.Default);
shake.Play(transform, this);   // 'this' is any MonoBehaviour to host the coroutine
```

## Add your own module
Derive from `FeedbackModule`, give it a `settings` struct, resolve a target, and call a utility:

```csharp
[System.Serializable]
public sealed class MyModule : FeedbackModule
{
    [SerializeField] MySettings settings = MySettings.Default;
    public override void Play(FeedbackContext context) { /* ... */ }
}
```

It shows up in the Add Module dropdown automatically.
