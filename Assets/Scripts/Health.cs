using System;
using UnityEngine;

public enum Team { Red, Blue }

public class Health : MonoBehaviour
{
    public Team team;
    public float maxHealth = 100f;
    public bool isBase = false;           
    public bool destroyOnDeath = true;    

    public float Current { get; private set; }
    public bool IsDead { get; private set; }
    public float Ratio => Current / maxHealth;

    public event Action<float, float> Changed;   
    public event Action<Health> Died;

    public static event Action<Health, Health> AnyDied;
    public static event Action<Health, float, Health> AnyDamaged;   

    void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(float amount, Health attacker = null)
    {
        if (IsDead || amount <= 0f) return;

        float dealt = Mathf.Min(Current, amount);   
        Current -= dealt;
        Changed?.Invoke(Current, maxHealth);
        AnyDamaged?.Invoke(this, dealt, attacker);

        if (Current <= 0f)
        {
            IsDead = true;
            Died?.Invoke(this);
            AnyDied?.Invoke(this, attacker);
            if (destroyOnDeath) Destroy(gameObject);
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Min(maxHealth, Current + amount);
        Changed?.Invoke(Current, maxHealth);
    }

    public void Revive()
    {
        IsDead = false;
        Current = maxHealth;
        Changed?.Invoke(Current, maxHealth);
    }

    [ContextMenu("Test: Take 100 Damage")]
    void TestDamage()
    {
        TakeDamage(100f);
        Debug.Log(name + ": " + Current + "/" + maxHealth);
    }

    [ContextMenu("Test: Heal 100")]
    void TestHeal()
    {
        Heal(100f);
        Debug.Log(name + ": " + Current + "/" + maxHealth);
    }
}