using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class WaveSpawner : MonoBehaviour
{
    [Header("Что спавнить")]
    public GameObject unitPrefab;

    [Header("Время")]
    public float firstWaveDelay = 3f;      
    public float waveInterval = 20f;       
    public float delayBetweenUnits = 0.6f; 

    [Header("Размер волны")]
    public int unitsPerWave = 3;           
    public int wavesPerBonusUnit = 3;      
    public int maxUnitsPerWave = 6;
    public float spawnSpread = 0.8f;       

    public int WaveNumber { get; private set; }
    public bool Active { get; private set; } = true;

    Transform container;

    void Start()
    {
        var existing = GameObject.Find("Units");
        container = existing != null ? existing.transform : new GameObject("Units").transform;

        Health.AnyDied += OnAnyDied;
        StartCoroutine(SpawnLoop());
    }

    void OnDestroy()
    {
        Health.AnyDied -= OnAnyDied;
    }

    void OnAnyDied(Health dead, Health killer)
    {
        if (dead.isBase) Active = false;
    }

    IEnumerator SpawnLoop()
    {
        if (unitPrefab == null)
        {
            Debug.LogError("WaveSpawner: не назначен Unit Prefab", this);
            yield break;
        }

        yield return new WaitForSeconds(firstWaveDelay);

        while (Active)
        {
            WaveNumber++;
            int bonus = (WaveNumber - 1) / Mathf.Max(1, wavesPerBonusUnit);
            int count = Mathf.Min(unitsPerWave + bonus, maxUnitsPerWave);

            for (int i = 0; i < count && Active; i++)
            {
                SpawnUnit();
                yield return new WaitForSeconds(delayBetweenUnits);
            }

            yield return new WaitForSeconds(waveInterval);
        }
    }

    void SpawnUnit()
    {
        Vector2 offset = Random.insideUnitCircle * spawnSpread;
        Vector3 pos = transform.position + new Vector3(offset.x, 0f, offset.y);

        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            pos = hit.position;
        else
            Debug.LogWarning("WaveSpawner: рядом со спавном нет NavMesh", this);

        Instantiate(unitPrefab, pos, Quaternion.identity, container);
    }
}