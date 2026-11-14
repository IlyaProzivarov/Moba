using System.Collections.Generic;
using UnityEngine;

public class HealZone : MonoBehaviour
{
    public float radius = 6f;
    public float healPercentPerSecond = 0.08f;   
    public float tickInterval = 0.5f;
    public Transform disc;                        

    Health owner;
    float nextTick;

    void Awake()
    {
        owner = GetComponentInParent<Health>();
    }

    void Start()
    {
        if (owner == null)
        {
            Debug.LogError("HealZone: у родителя нет компонента Health", this);
            enabled = false;
            return;
        }

        if (disc != null) disc.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
        nextTick = Time.time + tickInterval;
    }

    void Update()
    {
        if (owner.IsDead || Time.time < nextTick) return;
        nextTick = Time.time + tickInterval;

        var healed = new HashSet<Health>();
        foreach (var c in Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            var h = c.GetComponentInParent<Health>();
            if (h == null || h.isBase || h.IsDead || h.team != owner.team) continue;
            if (!healed.Add(h)) continue;

            if (h.Current < h.maxHealth)
                h.Heal(h.maxHealth * healPercentPerSecond * tickInterval);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}