using System.Collections;
using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [Header("HUD")]
    public TMP_Text timerText;
    public TMP_Text redKillsText;
    public TMP_Text blueKillsText;

    [Header("Result screen")]
    public GameObject resultPanel;
    public CanvasGroup resultGroup;
    public TMP_Text titleText;
    public TMP_Text subtitleText;
    public TMP_Text redStatsText;
    public TMP_Text blueStatsText;
    public float fadeDuration = 0.6f;

    [Header("Title colors")]
    public Color winColor = new Color(1f, 0.85f, 0.2f);
    public Color loseColor = new Color(0.95f, 0.25f, 0.25f);

    GameManager gm;

    void Start()
    {
        gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("GameUI: на сцене нет объекта с GameManager", this);
            enabled = false;
            return;
        }

        resultPanel.SetActive(false);
        gm.OnMatchEnded += ShowResult;
        UpdateHud();
    }

    void OnDestroy()
    {
        if (gm != null) gm.OnMatchEnded -= ShowResult;
    }

    void Update()
    {
        UpdateHud();
    }

    void UpdateHud()
    {
        timerText.text = gm.TimeText;
        redKillsText.text = "Red: " + gm.GetKills(Team.Red);
        blueKillsText.text = "Blue: " + gm.GetKills(Team.Blue);
    }

    void ShowResult(Team winner)
    {
        UpdateHud();

        bool won = gm.PlayerWon;
        titleText.text = won ? "VICTORY!" : "DEFEAT";
        titleText.color = won ? winColor : loseColor;

        string who = winner == Team.Red ? "Red" : "Blue";
        subtitleText.text = "Team " + who + " wins" +
                            "   Time: " + gm.TimeText +
                            "   Waves: " + gm.Waves;

        redStatsText.text = gm.GetKills(Team.Red) + "\n" +
                            gm.GetLosses(Team.Red) + "\n" +
                            gm.GetDamage(Team.Red);
        blueStatsText.text = gm.GetKills(Team.Blue) + "\n" +
                             gm.GetLosses(Team.Blue) + "\n" +
                             gm.GetDamage(Team.Blue);

        resultPanel.SetActive(true);
        StartCoroutine(Fade());
    }

    IEnumerator Fade()
    {
        resultGroup.alpha = 0f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeDuration);
            resultGroup.alpha = Mathf.Clamp01(t);
            yield return null;
        }
    }
}