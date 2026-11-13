using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Настройки матча")]
    public Team playerTeam = Team.Red;
    public float slowMotionScale = 0.25f;     
    public float slowMotionDuration = 1.5f;   

    public bool IsMatchEnded { get; private set; }
    public Team Winner { get; private set; }
    public bool PlayerWon => IsMatchEnded && Winner == playerTeam;
    public float ElapsedTime { get; private set; }
    public int Waves { get; private set; }

    public event Action<Team> OnMatchEnded;

    readonly int[] kills = new int[2];
    readonly int[] losses = new int[2];
    readonly float[] damage = new float[2];

    public int GetKills(Team t) => kills[(int)t];
    public int GetLosses(Team t) => losses[(int)t];
    public int GetDamage(Team t) => Mathf.RoundToInt(damage[(int)t]);

    public string TimeText
    {
        get
        {
            int s = Mathf.FloorToInt(ElapsedTime);
            return string.Format("{0:00}:{1:00}", s / 60, s % 60);
        }
    }

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void OnEnable()
    {
        Health.AnyDied += HandleDied;
        Health.AnyDamaged += HandleDamaged;
    }

    void OnDisable()
    {
        Health.AnyDied -= HandleDied;
        Health.AnyDamaged -= HandleDamaged;
    }

    void Update()
    {
        if (!IsMatchEnded) ElapsedTime += Time.deltaTime;
    }

    void HandleDamaged(Health victim, float amount, Health attacker)
    {
        if (IsMatchEnded || attacker == null) return;
        damage[(int)attacker.team] += amount;
    }

    void HandleDied(Health dead, Health killer)
    {
        if (IsMatchEnded) return;

        if (dead.isBase)
        {
            EndMatch(dead.team == Team.Red ? Team.Blue : Team.Red);
            return;
        }

        losses[(int)dead.team]++;
        if (killer != null) kills[(int)killer.team]++;
    }

    void EndMatch(Team winner)
    {
        IsMatchEnded = true;
        Winner = winner;

        foreach (var s in FindObjectsByType<WaveSpawner>(FindObjectsSortMode.None))
            Waves = Mathf.Max(Waves, s.WaveNumber);

        PrintStats();
        StartCoroutine(EndRoutine());
    }

    IEnumerator EndRoutine()
    {
        Time.timeScale = slowMotionScale;
        yield return new WaitForSecondsRealtime(slowMotionDuration);

        Time.timeScale = 0f;
        OnMatchEnded?.Invoke(Winner);
    }

    public void Restart()
    {
        Time.timeScale = 1f;   
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void PrintStats()
    {
        Debug.Log(
            "=== МАТЧ ЗАВЕРШЁН ===\n" +
            "Результат: " + (PlayerWon ? "ПОБЕДА" : "ПОРАЖЕНИЕ") + " (победили: " + Winner + ")\n" +
            "Время: " + TimeText + ", волн: " + Waves + "\n" +
            "Красные: убито " + GetKills(Team.Red) + ", потеряно " + GetLosses(Team.Red) +
            ", урон " + GetDamage(Team.Red) + "\n" +
            "Синие: убито " + GetKills(Team.Blue) + ", потеряно " + GetLosses(Team.Blue) +
            ", урон " + GetDamage(Team.Blue));
    }

    // Тестовые кнопки
    [ContextMenu("Test: Win (сломать синюю базу)")]
    void TestWin() { DamageBase(Team.Blue); }

    [ContextMenu("Test: Lose (сломать красную базу)")]
    void TestLose() { DamageBase(Team.Red); }

    void DamageBase(Team team)
    {
        foreach (var h in FindObjectsByType<Health>(FindObjectsSortMode.None))
        {
            if (h.isBase && h.team == team) h.TakeDamage(99999f);
        }
    }
}