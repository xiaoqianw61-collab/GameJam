using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏管理器 - 夜鹭便便大作战
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Text scoreText;
    public Text comboText;
    public Text roundText;
    public Text instructionText;
    public GameObject gameOverPanel;
    public Text finalScoreText;

    private int score;
    private int combo;
    private int totalTargets;
    private int targetsHit;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        UpdateUI();
    }

    public void SetTotalTargets(int count)
    {
        totalTargets = count;
        targetsHit = 0;
    }

    public void OnTargetHit(int points)
    {
        combo++;
        int bonus = combo > 1 ? combo * 5 : 0;
        score += points + bonus;
        targetsHit++;
        UpdateUI();
    }

    public void OnTargetMiss()
    {
        combo = 0;
        UpdateUI();
    }

    public void OnRoundEnd()
    {
        float hitRate = totalTargets > 0 ? (float)targetsHit / totalTargets : 0;
        string rating = hitRate >= 0.9f ? "S" : hitRate >= 0.7f ? "A" : hitRate >= 0.5f ? "B" : "C";
        if (gameOverPanel)
        {
            gameOverPanel.SetActive(true);
            if (finalScoreText)
                finalScoreText.text = $"Round Over!\nHit: {targetsHit}/{totalTargets} ({Mathf.RoundToInt(hitRate*100)}%)\nRating: {rating}\nScore: {score}";
        }
        Invoke(nameof(RestartGame), 5f);
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = $"Score: {score}";
        if (comboText) comboText.text = combo > 1 ? $"Combo x{combo}!" : "";
    }

    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
