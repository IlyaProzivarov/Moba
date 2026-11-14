using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class AbilitySlot
{
    public string name;
    public KeyCode key;
    public float cooldown;
    public RectTransform overlay;     
    public TMP_Text cooldownText;     
    [System.NonSerialized] public float readyTime;
}

[RequireComponent(typeof(HeroController))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(NavMeshAgent))]
public class HeroAbilities : MonoBehaviour
{
    [Header("—лоты: 0 = Z (Slam), 1 = X (Dash), 2 = C (Rage)")]
    public AbilitySlot[] slots =
    {
        new AbilitySlot { name = "Slam", key = KeyCode.Z, cooldown = 7f },
        new AbilitySlot { name = "Dash", key = KeyCode.X, cooldown = 5f },
        new AbilitySlot { name = "Rage", key = KeyCode.C, cooldown = 14f },
    };

    [Header("Z: Slam")]
    public float slamRadius = 4f;
    public float slamDamage = 40f;

    [Header("X: Dash")]
    public float dashDistance = 7f;
    public float dashDuration = 0.2f;
    public LayerMask groundMask;            

    [Header("C: Rage")]
    public float rageDuration = 5f;
    public float rageDamageMultiplier = 1.6f;
    public float rageSpeedMultiplier = 1.4f;
    public Renderer heroRenderer;           
    public Color rageColor = new Color(1f, 0.3f, 0.1f);

    [Header("Ёффекты")]
    public Material effectMaterial;         

    HeroController hero;
    Health health;
    NavMeshAgent agent;
    Camera cam;
    Coroutine rageRoutine;
    float baseDamage;
    float baseSpeed;

    void Awake()
    {
        hero = GetComponent<HeroController>();
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        cam = Camera.main;
        baseDamage = hero.attackDamage;
        baseSpeed = hero.moveSpeed;
        foreach (var s in slots) s.readyTime = 0f;
        health.Died += OnDied;
    }

    void OnDestroy()
    {
        if (health != null) health.Died -= OnDied;
    }

    void Update()
    {
        UpdateUI();

        if (health.IsDead || hero.ControlLocked || Time.timeScale == 0f) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (Input.GetKeyDown(slots[i].key)) TryCast(i);
        }
    }

    void TryCast(int i)
    {
        var s = slots[i];
        if (Time.time < s.readyTime) return;   

        bool cast = false;
        switch (i)
        {
            case 0: CastSlam(); cast = true; break;
            case 1: cast = CastDash(); break;
            case 2: CastRage(); cast = true; break;
        }

        if (cast) s.readyTime = Time.time + s.cooldown;
    }

    // Z: удар по площади

    void CastSlam()
    {
        Vector3 center = transform.position;
        var damaged = new HashSet<Health>();   

        foreach (var col in Physics.OverlapSphere(center, slamRadius, ~0, QueryTriggerInteraction.Ignore))
        {
            var h = col.GetComponentInParent<Health>();
            if (h == null || h == health || h.IsDead || h.team == health.team) continue;
            if (damaged.Add(h)) h.TakeDamage(slamDamage, health);
        }

        StartCoroutine(Shockwave(center, slamRadius, new Color(1f, 0.55f, 0.2f, 0.6f)));
    }

    IEnumerator Shockwave(Vector3 center, float radius, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(go.GetComponent<Collider>());
        go.transform.position = center + Vector3.up * 0.05f;

        var r = go.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (effectMaterial != null) r.sharedMaterial = effectMaterial;

        var block = new MaterialPropertyBlock();
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.35f;
            float k = Mathf.Clamp01(t);

            float size = radius * 2f * Mathf.Lerp(0.2f, 1f, k);
            go.transform.localScale = new Vector3(size, 0.02f, size);

            Color c = color;
            c.a = color.a * (1f - k);
            block.SetColor("_BaseColor", c);
            r.SetPropertyBlock(block);

            yield return null;
        }
        Destroy(go);
    }

    // X: рывок

    bool CastDash()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return false;

        Vector3 dir = transform.forward;
        if (cam != null && Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition),
                out RaycastHit hit, 200f, groundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 d = hit.point - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.25f) dir = d.normalized;
        }
        dir.y = 0f;
        dir.Normalize();

        StartCoroutine(DashRoutine(dir));
        return true;
    }

    IEnumerator DashRoutine(Vector3 dir)
    {
        hero.ControlLocked = true;
        hero.StopActions();
        transform.rotation = Quaternion.LookRotation(dir);

        Vector3 start = transform.position;
        Vector3 end = start + dir * dashDistance;
        if (NavMesh.Raycast(start, end, out NavMeshHit navHit, NavMesh.AllAreas))
            end = navHit.position;  

        Vector3 total = end - start;
        float t = 0f;
        while (t < 1f && !health.IsDead)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, dashDuration);
            float k = Mathf.Clamp01(t);
            float eased = 1f - (1f - k) * (1f - k);   

            Vector3 want = start + total * eased;
            if (agent.enabled && agent.isOnNavMesh)
                agent.Move(want - transform.position);

            yield return null;
        }

        hero.ControlLocked = false;
    }

    // C: €рость 

    void CastRage()
    {
        if (rageRoutine != null) StopCoroutine(rageRoutine);
        rageRoutine = StartCoroutine(RageRoutine());
    }

    IEnumerator RageRoutine()
    {
        ApplyRage(true);
        yield return new WaitForSeconds(rageDuration);
        ApplyRage(false);
        rageRoutine = null;
    }

    void ApplyRage(bool on)
    {
        hero.attackDamage = baseDamage * (on ? rageDamageMultiplier : 1f);
        agent.speed = baseSpeed * (on ? rageSpeedMultiplier : 1f);

        if (heroRenderer == null) return;
        if (on)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", rageColor);
            heroRenderer.SetPropertyBlock(block);
        }
        else
        {
            heroRenderer.SetPropertyBlock(null);
        }
    }

    // —мерть 

    void OnDied(Health h)
    {
        if (rageRoutine != null)
        {
            StopCoroutine(rageRoutine);
            rageRoutine = null;
        }
        ApplyRage(false);
        hero.ControlLocked = false;
    }

    // ѕанель 

    void UpdateUI()
    {
        foreach (var s in slots)
        {
            float remain = Mathf.Max(0f, s.readyTime - Time.time);
            float ratio = s.cooldown > 0f ? remain / s.cooldown : 0f;

            if (s.overlay != null)
            {
                s.overlay.gameObject.SetActive(ratio > 0f);
                s.overlay.localScale = new Vector3(1f, ratio, 1f);
            }

            if (s.cooldownText != null)
            {
                if (remain <= 0f) s.cooldownText.text = "";
                else if (remain >= 1f) s.cooldownText.text = Mathf.CeilToInt(remain).ToString();
                else s.cooldownText.text = remain.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}