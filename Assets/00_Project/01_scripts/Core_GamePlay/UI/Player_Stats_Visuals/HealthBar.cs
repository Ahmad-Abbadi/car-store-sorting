using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class HealthBar : MonoBehaviour
{
    private Slider slider; 
    private StatsBehaviour targetStats;

    void Awake()
    {
        slider = GetComponent<Slider>();
        slider.maxValue = StatsBehaviour.FullHealth;
    }

    public void SetStatsBehaviour(StatsBehaviour stats)
    {
        targetStats = stats;
        targetStats.OnHealthUpdate += UpdateHealth; 
        
        UpdateHealth(targetStats.Health);
    }

    public void UpdateHealth(float currentHealth)
    {
        if (slider != null)
        {
            slider.value = currentHealth;
        }
    }

    void OnDestroy()
    {
        if (targetStats != null)
        {
            targetStats.OnHealthUpdate -= UpdateHealth;
        }
    }
}