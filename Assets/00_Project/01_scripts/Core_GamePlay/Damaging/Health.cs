using System;
using UnityEngine;

/// <summary> Reusable health + IDamageable for any creature; fires damage/death events. </summary>
public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [Tooltip("Starting and maximum hit points.")]
    [SerializeField] private float maxHealth = 100f;

    private float _current;
    private bool _isDead;

    /// <summary> Current hit points. </summary>
    public float Current => _current;
    /// <summary> Maximum hit points. </summary>
    public float Max => maxHealth;
    /// <summary> Health as a 0..1 fraction. </summary>
    public float Normalized01 => maxHealth > 0f ? Mathf.Clamp01(_current / maxHealth) : 0f;
    /// <summary> True once health has reached zero. </summary>
    public bool IsDead => _isDead;

    /// <summary> Fired on each hit (damage amount, source). </summary>
    public event Action<float, GameObject> OnDamaged;
    /// <summary> Fired once when health reaches zero (killer source). </summary>
    public event Action<GameObject> OnDied;
    /// <summary> Fired whenever health changes, as a 0..1 fraction. </summary>
    public event Action<float> OnHealthChanged;

    private void Awake() => _current = maxHealth;

    /// <summary> Applies damage, clamps at zero, and raises death once. </summary>
    public void TakeDamage(float amount, GameObject source)
    {
        if (_isDead || amount <= 0f) return;

        _current = Mathf.Max(0f, _current - amount);
        OnDamaged?.Invoke(amount, source);
        OnHealthChanged?.Invoke(Normalized01);

        if (_current <= 0f)
        {
            _isDead = true;
            OnDied?.Invoke(source);
        }
    }

    /// <summary> Restores health without exceeding the maximum. </summary>
    public void Heal(float amount)
    {
        if (_isDead || amount <= 0f) return;

        _current = Mathf.Min(maxHealth, _current + amount);
        OnHealthChanged?.Invoke(Normalized01);
    }

    /// <summary> Scales max health, keeping the current fraction intact. </summary>
    public void MultiplyMax(float multiplier)
    {
        if (multiplier <= 0f) return;

        float fraction = Normalized01;
        maxHealth *= multiplier;
        _current = maxHealth * fraction;
        OnHealthChanged?.Invoke(Normalized01);
    }

    /// <summary> Resets to full health and clears the dead flag. </summary>
    public void ResetHealth()
    {
        _current = maxHealth;
        _isDead = false;
        OnHealthChanged?.Invoke(Normalized01);
    }
}
