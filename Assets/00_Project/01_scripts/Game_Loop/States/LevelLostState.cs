using UnityEngine;

/// <summary> Level lost: time frozen, cursor forced free for the death menu. </summary>
public class LevelLostState : GameStateBase
{
    /// <summary> Guarantees a visible, unlocked cursor regardless of inspector wiring. </summary>
    protected override void OnEnter()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
