using System;
using UnityEngine;

public class BossHealth : MonoBehaviour
{
    [SerializeField] int maxHealth = 500;
    public int CurrentHealth { get; private set; }
    public bool IsPhaseTwo { get; private set; }

    public event Action<int, int> OnHealthChanged;
    public event Action OnPhaseTwo;
    public event Action OnDeath;

    void Awake() { CurrentHealth = maxHealth; }

    public void TakeDamage(int amount)
    {
        if (CurrentHealth <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (!IsPhaseTwo && CurrentHealth <= maxHealth / 2)
        {
            IsPhaseTwo = true;
            OnPhaseTwo?.Invoke();
        }

        if (CurrentHealth == 0) OnDeath?.Invoke();
    }
}