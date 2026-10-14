using UnityEngine;
using System;

public class StatsBehaviour : MonoBehaviour,IDamageable
{
    public const float FullHealth = 100f; 
    public const float FullStamina = 100f; 

    [Header("Auto Heal")]
    [SerializeField] private bool _autoHealEnabled = true; 
    [SerializeField] private float _autoHealDelay = 5f; 
    [SerializeField] private float _autoHealPerSecond = 5f; 

    private float _health;
    private float _stamina;
    private float _timeSinceDamage;
    private bool _isDead;

    // Fixed typo: OnStaminaUdpate -> OnStaminaUpdate
    public event Action<float> OnHealthUpdate; 
    public event Action<float> OnStaminaUpdate; 

    /// <summary> Published once when health reaches zero </summary>
    public static event Action<StatsBehaviour> OnDeath; 

    public bool IsDead => _isDead; 

    void Start()
    {
        SetUpUI();
        
        // Initialize stats. Doing this after SetUpUI ensures the UI 
        // catches the initial event updates.
        Health = FullHealth;
        Stamina = FullStamina;
    }

    void Update()
    {
        AutoHeal();
    }

    private void SetUpUI()
    {
        var healthBar = FindFirstObjectByType<HealthBar>(); 
        if (healthBar != null) healthBar.SetStatsBehaviour(this); 

        var staminaBar = FindFirstObjectByType<StaminaBar>(); 
        if (staminaBar != null) staminaBar.SetStatsBehaviour(this); 
    }

    /// <summary> Regenerates health over time after a damage-free delay </summary>
    private void AutoHeal()
    {
        if (!_autoHealEnabled) return; 

        // Dead or already full: nothing to regenerate
        if (_isDead || _health >= FullHealth) return; 

        _timeSinceDamage += Time.deltaTime; 
        if (_timeSinceDamage < _autoHealDelay) return; 

        Health += _autoHealPerSecond * Time.deltaTime; 
    }

    public void TakeDamage(float amount, GameObject source)
    {
        // Ignore damage once dead
        if (_isDead) return; 

        // Damage restarts the auto heal delay
        _timeSinceDamage = 0f; 
        Health -= amount; 
    }

    /// <summary> Publishes death a single time per life </summary>
    private void PublishDeath()
    {
        if (_isDead) return; 

        _isDead = true; 
        OnDeath?.Invoke(this); 
    }

    /// <summary> Restores full health/stamina and clears the death flag. </summary>
    public void ResetStats()
    {
        _timeSinceDamage = 0f;
        Health = FullHealth;   // setter clears _isDead when above zero
        Stamina = FullStamina;
    }

    public float Health
    {
        get => _health;
        set
        {
            // Clamp prevents health from dropping below 0 or exceeding max
            _health = Mathf.Clamp(value, 0f, FullHealth); 
            OnHealthUpdate?.Invoke(_health); 

            // Death publishes once; any revive clears the flag
            if (_health <= 0f) PublishDeath(); 
            else _isDead = false; 
        }
    }
    
    public float Stamina
    {
        get => _stamina; 
        set
        {
            _stamina = Mathf.Clamp(value, 0f, FullStamina); 
            OnStaminaUpdate?.Invoke(_stamina); 
        }
    } 
}
