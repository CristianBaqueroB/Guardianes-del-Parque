using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Basura")]
    public int collectedTrash = 0;
    public int totalTrash = 10;

    [Header("Puntos")]
    public int score = 0;

    [Header("Tiempo")]
    public float timeRemaining = 60f;
    private bool timerRunning = true;

    [Header("UI")]
    public TextMeshProUGUI counterText;
    public TextMeshProUGUI timerText;

    public GameObject winPanel;
    public GameObject gameOverPanel;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        UpdateUI();

        if (winPanel != null)
            winPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void Update()
    {
        HandleTimer();
    }

    void HandleTimer()
    {
        if (timerRunning)
        {
            timeRemaining -= Time.deltaTime;

            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                timerRunning = false;

                GameOver();
            }

            UpdateTimerUI();
        }
    }

    void UpdateTimerUI()
    {
        timerText.text =
            "Tiempo: " +
            Mathf.Ceil(timeRemaining).ToString();
    }

    public void CollectTrash(int points, float extraTime)
    {
        collectedTrash++;

        score += points;

        // Bonus de tiempo
        timeRemaining += extraTime;

        UpdateUI();

        if (collectedTrash >= totalTrash)
        {
            timerRunning = false;
            WinGame();
        }
    }

    void UpdateUI()
    {
        counterText.text =
            "Basura: " +
            collectedTrash +
            "/" +
            totalTrash +
            "\nPuntos: " +
            score;
    }

    void WinGame()
    {
        Debug.Log("¡Parque limpio!");

        if (winPanel != null)
            winPanel.SetActive(true);
    }

    void GameOver()
    {
        Debug.Log("Se acabó el tiempo");

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }
}