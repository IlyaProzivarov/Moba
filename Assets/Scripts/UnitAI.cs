using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class UnitAI : MonoBehaviour
{
    [Header("Движение")]
    public float moveSpeed = 3.5f;
    public string waypointsName = "Waypoints";
    public float waypointReachDistance = 1.2f;

    [Header("Бой")]
    public float detectRange = 6f;      
    public float attackRange = 1.5f;    
    public float attackDamage = 10f;
    public float attackCooldown = 1f;

    [Header("Визуал")]
    public Transform visual;            
    public float lungeDistance = 0.35f; 

    NavMeshAgent agent;
    Health health;
    Transform[] route;
    int routeIndex;
    Health target;
    Collider targetCol;
    Health enemyBase;
    float nextAttackTime;
    float nextScanTime;
    Vector3 visualStartPos;
    bool lunging;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
    }

    void Start()
    {
        agent.speed = moveSpeed;
        agent.stoppingDistance = 0f;
        agent.autoBraking = false;

        if (visual != null) visualStartPos = visual.localPosition;

        BuildRoute();
        FindEnemyBase();
    }

    void BuildRoute()
    {
        var root = GameObject.Find(waypointsName);
        if (root == null)
        {
            Debug.LogError("UnitAI: на сцене нет объекта " + waypointsName, this);
            route = new Transform[0];
            return;
        }

        int n = root.transform.childCount;
        route = new Transform[n];
        for (int i = 0; i < n; i++)
        {
            int idx = health.team == Team.Red ? i : n - 1 - i;
            route[i] = root.transform.GetChild(idx);
        }
    }

    void FindEnemyBase()
    {
        foreach (var h in FindObjectsByType<Health>(FindObjectsSortMode.None))
        {
            if (h.isBase && h.team != health.team) enemyBase = h;
        }
    }

    void Update()
    {
        if (health.IsDead) return;

        if (target == null || target.IsDead || DistanceToTarget() > detectRange * 1.5f)
            target = null;

        if (target == null && Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + 0.25f;
            target = FindTarget();
        }

        if (target != null) FightOrChase();
        else FollowRoute();
    }

    Health FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRange);

        Health best = null;
        Collider bestCol = null;
        float bestDist = float.MaxValue;
        bool bestIsBase = true;

        foreach (var col in hits)
        {
            var h = col.GetComponentInParent<Health>();
            if (h == null || h == health || h.IsDead || h.team == health.team) continue;

            Vector3 d = col.ClosestPoint(transform.position) - transform.position;
            d.y = 0f;
            float dist = d.magnitude;

            bool better;
            if (best == null) better = true;
            else if (bestIsBase != h.isBase) better = !h.isBase; 
            else better = dist < bestDist;

            if (better)
            {
                best = h;
                bestCol = col;
                bestDist = dist;
                bestIsBase = h.isBase;
            }
        }

        targetCol = bestCol;
        return best;
    }

    float DistanceToTarget()
    {
        if (targetCol == null) return float.MaxValue;
        Vector3 d = targetCol.ClosestPoint(transform.position) - transform.position;
        d.y = 0f;
        return d.magnitude;
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

    void FollowRoute()
    {
        agent.isStopped = false;

        if (routeIndex < route.Length)
        {
            Vector3 wp = route[routeIndex].position;
            agent.SetDestination(wp);

            Vector3 d = wp - transform.position;
            d.y = 0f;
            if (d.magnitude < waypointReachDistance) routeIndex++;
        }
        else if (enemyBase != null && !enemyBase.IsDead)
        {
            agent.SetDestination(enemyBase.transform.position);
        }
    }

    void FaceTarget()
    {
        Vector3 dir = target.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}