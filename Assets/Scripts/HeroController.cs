using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class HeroController : MonoBehaviour
{
    [Header("Движение")]
    public float moveSpeed = 5.5f;
    public LayerMask groundMask;            

    [Header("Бой")]
    public float attackRange = 2.2f;
    public float attackDamage = 25f;
    public float attackCooldown = 0.8f;
    public float aggroRange = 5f;           

    [Header("Возрождение")]
    public float respawnDelay = 8f;
    public Transform respawnPoint;
    public GameObject[] hideOnDeath;        

    [Header("Визуал")]
    public Transform visual;
    public float lungeDistance = 0.4f;

    NavMeshAgent agent;
    Health health;
    Collider col;
    Camera cam;

    Health target;
    Collider targetCol;
    bool chasing;
    bool forcedTarget;      
    bool moveCommand;       
    float nextAttackTime;
    float nextScanTime;
    Vector3 visualStartPos;
    bool lunging;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        col = GetComponent<Collider>();
    }

    void Start()
    {
        cam = Camera.main;
        agent.speed = moveSpeed;
        agent.acceleration = 40f;
        agent.angularSpeed = 720f;
        agent.stoppingDistance = 0.1f;

        if (visual != null) visualStartPos = visual.localPosition;
        health.Died += OnDied;
    }

    void OnDestroy()
    {
        if (health != null) health.Died -= OnDied;
    }

    void Update()
    {
        if (health.IsDead) return;

        ReadInput();

        if (moveCommand)
        {
            if (Arrived()) moveCommand = false;
            return;
        }

        ValidateTarget();

        if (!chasing && Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + 0.25f;
            AcquireTarget();
        }

        if (chasing) FightOrChase();
    }

    void ReadInput()
    {
        if (Time.timeScale == 0f || cam == null) return;

        if (Input.GetKeyDown(KeyCode.S))
        {
            moveCommand = false;
            ClearTarget();
            return;
        }

        if (!Input.GetMouseButtonDown(1)) return;   

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
        {
            var h = hit.collider.GetComponentInParent<Health>();
            if (h != null && !h.IsDead && h.team != health.team)
            {
                SetTarget(h, hit.collider, true);
                agent.isStopped = false;
                return;
            }
        }

        if (Physics.Raycast(ray, out hit, 200f, groundMask, QueryTriggerInteraction.Ignore))
            MoveTo(hit.point);
    }

    void MoveTo(Vector3 point)
    {
        if (NavMesh.SamplePosition(point, out NavMeshHit nav, 2f, NavMesh.AllAreas))
            point = nav.position;

        ClearTarget();
        moveCommand = true;
        agent.isStopped = false;
        agent.SetDestination(point);
    }

    bool Arrived()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f;
    }

    void SetTarget(Health h, Collider c, bool forced)
    {
        target = h;
        targetCol = c;
        forcedTarget = forced;
        chasing = true;
        moveCommand = false;
    }

    void ClearTarget()
    {
        target = null;
        targetCol = null;
        forcedTarget = false;
        chasing = false;
        if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
    }

    void ValidateTarget()
    {
        if (!chasing) return;
        if (target == null || target.IsDead ||
            (!forcedTarget && DistanceToTarget() > aggroRange * 1.5f))
            ClearTarget();
    }

    float DistanceToTarget()
    {
        if (targetCol == null) return float.MaxValue;
        Vector3 d = targetCol.ClosestPoint(transform.position) - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    void AcquireTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, aggroRange);

        Health best = null;
        Collider bestCol = null;
        float bestDist = float.MaxValue;
        bool bestIsBase = true;

        foreach (var c in hits)
        {
            var h = c.GetComponentInParent<Health>();
            if (h == null || h == health || h.IsDead || h.team == health.team) continue;

            Vector3 d = c.ClosestPoint(transform.position) - transform.position;
            d.y = 0f;
            float dist = d.magnitude;

            bool better;
            if (best == null) better = true;
            else if (bestIsBase != h.isBase) better = !h.isBase;
            else better = dist < bestDist;

            if (better)
            {
                best = h;
                bestCol = c;
                bestDist = dist;
                bestIsBase = h.isBase;
            }
        }

        if (best != null) SetTarget(best, bestCol, false);
    }

    void FightOrChase()
    {
        if (DistanceToTarget() <= attackRange)
        {
            agent.isStopped = true;
            FaceTarget();

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;
                target.TakeDamage(attackDamage, health);
                if (visual != null && !lunging) StartCoroutine(Lunge());
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(target.transform.position);
        }
    }

    void FaceTarget()
    {
        Vector3 dir = target.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
    }

    IEnumerator Lunge()
    {
        lunging = true;
        Vector3 end = visualStartPos + Vector3.forward * lungeDistance;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.12f;
            visual.localPosition = Vector3.Lerp(
                visualStartPos, end, Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI));
            yield return null;
        }
        visual.localPosition = visualStartPos;
        lunging = false;
    }

    void OnDied(Health h)
    {
        moveCommand = false;
        ClearTarget();
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        SetAlive(false);
        yield return new WaitForSeconds(respawnDelay);

        Vector3 pos = respawnPoint != null ? respawnPoint.position : transform.position;
        transform.position = pos;

        health.Revive();
        SetAlive(true);

        agent.Warp(pos);
        agent.isStopped = false;
    }

    void SetAlive(bool alive)
    {
        col.enabled = alive;
        agent.enabled = alive;
        foreach (var go in hideOnDeath)
            if (go != null) go.SetActive(alive);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}