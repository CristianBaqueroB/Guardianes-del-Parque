using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Objetivos de Limpieza")]
    public int collectedTrash = 0;
    public int totalTrash = 10;

    [Header("Puntuación y Clasificación")]
    public int score = 0;
    public int correctlyClassified = 0;
    public int incorrectlyClassified = 0;
    public int currentStreak = 0;
    public int highestStreak = 0;

    [Header("Tiempo")]
    public float timeRemaining = 120f;
    private bool timerRunning = false;

    [Header("Estado de Inicio")]
    private bool gameStarted = false;

    [Header("Puntaje Maximo")]
    public int highScore = 0;

    [Header("Estado del Juego")]
    public bool isPaused = false;
    public bool isGameOver = false;
    public bool isVictory = false;

    [Header("Penalizaciones")]
    public int incorrectScorePenalty = 10;
    public float incorrectTimePenalty = 5f;

    [Header("UI Elementos")]
    public TextMeshProUGUI counterText;
    public TextMeshProUGUI timerText;
    public GameObject winPanel;
    public GameObject gameOverPanel;
    public GameObject pausePanel;

    public System.Action<int, float, bool, float> OnDepositProcessed;
    public System.Action<int> OnStreakChanged;

    // Estilos OnGUI para menús
    private GUIStyle menuBoxStyle;
    private GUIStyle menuTitleStyle;
    private GUIStyle menuBtnStyle;
    private GUIStyle menuBodyStyle;
    private bool stylesInitialized = false;

    private void Awake()
    {
        instance = this;
        Time.timeScale = 1f; // Asegura velocidad normal al iniciar o reiniciar
    }

    private void Start()
    {
        // Cargar HighScore guardado
        highScore = PlayerPrefs.GetInt("HighScore", 0);

        // Desactiva textos y paneles sueltos que causaban colision visual en pantalla
        GameObject winTextObj = GameObject.Find("WinText");
        if (winTextObj != null) winTextObj.SetActive(false);

        if (counterText != null) counterText.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        // Auto-crear MainMenuManager si no existe
        if (FindAnyObjectByType<MainMenuManager>() == null)
        {
            GameObject mmObj = new GameObject("MainMenuManager_Auto");
            mmObj.AddComponent<MainMenuManager>();
        }

        // Auto-crear AmbientAudioManager si no existe
        if (FindAnyObjectByType<AmbientAudioManager>() == null)
        {
            GameObject aaObj = new GameObject("AmbientAudioManager_Auto");
            aaObj.AddComponent<AmbientAudioManager>();
        }

        UpdateUI();
    }

    private void Update()
    {
        // Tecla de Pausa (Escape o P)
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (!isGameOver && !isVictory)
            {
                TogglePause();
            }
        }

        if (!isPaused && !isGameOver && !isVictory && gameStarted)
        {
            HandleTimer();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;

        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
        }
    }

    /// <summary>Llamado por MainMenuManager cuando el jugador presiona JUGAR</summary>
    public void SetGameStarted(bool started)
    {
        gameStarted = started;
        if (started) timerRunning = true;
    }

    public bool IsGameStarted() => gameStarted;

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HandleTimer()
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

    private void UpdateTimerUI()
    {
        if (timerText != null && timerText.gameObject.activeSelf)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            string colorTag = (timeRemaining <= 20f) ? "<color=#FF5050>" : "<color=#FFFFFF>";
            timerText.text = $"<b>{colorTag}{minutes:00}:{seconds:00}</color></b>";
        }
    }

    public float GetCurrentMultiplier()
    {
        if (currentStreak >= 8) return 3.0f;
        if (currentStreak >= 5) return 2.0f;
        if (currentStreak >= 3) return 1.5f;
        if (currentStreak >= 2) return 1.2f;
        return 1.0f;
    }

    public void CollectTrash(int points, float extraTime)
    {
        RegisterDeposit(points, extraTime, true);
    }

    public void RegisterDeposit(int points, float extraTime, bool isCorrect)
    {
        float multiplier = 1f;
        int deltaPoints = 0;
        float deltaTime = 0f;

        if (isCorrect)
        {
            collectedTrash++;
            correctlyClassified++;
            currentStreak++;
            if (currentStreak > highestStreak) highestStreak = currentStreak;

            multiplier = GetCurrentMultiplier();
            deltaPoints = Mathf.RoundToInt(points * multiplier);

            float streakBonusTime = (currentStreak >= 3) ? 2.5f : 0f;
            deltaTime = extraTime + streakBonusTime;

            score += deltaPoints;
            timeRemaining += deltaTime;
        }
        else
        {
            incorrectlyClassified++;
            currentStreak = 0;
            multiplier = 1f;

            deltaPoints = -incorrectScorePenalty;
            deltaTime = -incorrectTimePenalty;

            score = Mathf.Max(0, score - incorrectScorePenalty);
            timeRemaining = Mathf.Max(0, timeRemaining - incorrectTimePenalty);
        }

        OnDepositProcessed?.Invoke(deltaPoints, deltaTime, isCorrect, multiplier);
        OnStreakChanged?.Invoke(currentStreak);

        UpdateUI();

        // Condición de victoria: Alcanzar el objetivo de residuos o pureza al 100%
        if (collectedTrash >= totalTrash)
        {
            timerRunning = false;
            WinGame();
        }
    }

    public void UpdateUI()
    {
        if (counterText != null && counterText.gameObject.activeSelf)
        {
            string streakStr = "";
            if (currentStreak >= 2)
            {
                float mult = GetCurrentMultiplier();
                streakStr = $" | <color=#FFD166>Racha x{currentStreak} (x{mult:F1} Pts)</color>";
            }

            counterText.text =
                $"<b>Clasificados:</b> {collectedTrash}/{totalTrash}\n" +
                $"<b>Puntos:</b> {score}{streakStr}";
        }
    }

    public void WinGame()
    {
        if (isVictory) return;
        isVictory = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (counterText != null) counterText.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Guardar HighScore
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (counterText != null) counterText.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Guardar HighScore aunque sea derrota
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }
    }

    #region Menús OnGUI (Pausa, Victoria y Game Over)
    private void InitMenuStyles()
    {
        if (stylesInitialized) return;

        menuBoxStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(20, 20, 20, 20)
        };

        menuTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 21,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        menuTitleStyle.normal.textColor = new Color(1f, 0.88f, 0.25f);

        menuBodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        menuBodyStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f);

        menuBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitMenuStyles();

        // 1. Menú de Pausa
        if (isPaused)
        {
            float width = 440;
            float height = 330;
            float x = (Screen.width - width) / 2f;
            float y = (Screen.height - height) / 2f;

            GUI.backgroundColor = new Color(0.06f, 0.11f, 0.16f, 0.96f);
            GUI.Box(new Rect(x, y, width, height), GUIContent.none, menuBoxStyle);

            GUILayout.BeginArea(new Rect(x + 20, y + 15, width - 40, height - 30));
            GUILayout.Label("⏸️ <b>JUEGO EN PAUSA</b>", menuTitleStyle);
            GUILayout.Space(10);

            float purity = (EcoMissionManager.Instance != null) ? EcoMissionManager.Instance.currentPurity : 0f;
            GUILayout.Label($"<b>Puntos Actuales:</b> {score}   |   <b>Pureza:</b> {Mathf.RoundToInt(purity)}%", menuBodyStyle);
            GUILayout.Label($"<b>Racha Actual:</b> x{currentStreak}   |   <b>Mejor Racha:</b> x{highestStreak}", menuBodyStyle);
            GUILayout.Space(10);

            GUILayout.Label("<b>Controles:</b>\n• [W,A,S,D] Moverse  |  [Espacio] Saltar\n• [E] Recoger residuo / Depositar en caneca\n• [Q / Rueda Ratón] Cambiar residuo en mano", menuBodyStyle);
            GUILayout.Space(15);

            if (GUILayout.Button("▶ Reanudar Partida", menuBtnStyle, GUILayout.Height(36)))
            {
                TogglePause();
            }

            GUILayout.Space(5);
            if (GUILayout.Button("🔄 Reiniciar Parque", menuBtnStyle, GUILayout.Height(36)))
            {
                RestartGame();
            }

            GUILayout.EndArea();
        }

        // 2. Pantalla de Victoria
        if (isVictory)
        {
            float width = 500;
            float height = 360;
            float x = (Screen.width - width) / 2f;
            float y = (Screen.height - height) / 2f;

            GUI.backgroundColor = new Color(0.05f, 0.22f, 0.11f, 0.97f);
            GUI.Box(new Rect(x, y, width, height), GUIContent.none, menuBoxStyle);

            GUILayout.BeginArea(new Rect(x + 24, y + 18, width - 48, height - 36));
            GUILayout.Label("PARQUE DE LA VIDA RESTAURADO!", menuTitleStyle);
            GUILayout.Space(12);

            float purity = (EcoMissionManager.Instance != null) ? EcoMissionManager.Instance.currentPurity : 100f;
            bool newRecord = (score > 0 && score >= highScore);
            string recordTag = newRecord ? "  [NUEVO RECORD!]" : $"  (Record: {highScore} pts)";
            GUILayout.Label($"Puntuacion Final: {score} Puntos{recordTag}\n" +
                            $"Residuos Clasificados: {correctlyClassified} de {totalTrash}\n" +
                            $"Pureza Ambiental Alcanzada: {Mathf.RoundToInt(purity)}%\n" +
                            $"Errores de Reciclaje: {incorrectlyClassified}\n" +
                            $"Mejor Racha de Combos: x{highestStreak}\n\n" +
                            $"Felicitaciones! Has protegido el Parque de la Vida en Valledupar.", menuBodyStyle);
            GUILayout.Space(15);

            if (GUILayout.Button("Jugar de Nuevo", menuBtnStyle, GUILayout.Height(40)))
            {
                RestartGame();
            }

            GUILayout.EndArea();
        }

        // 3. Pantalla de Game Over
        if (isGameOver)
        {
            float width = 460;
            float height = 320;
            float x = (Screen.width - width) / 2f;
            float y = (Screen.height - height) / 2f;

            GUI.backgroundColor = new Color(0.28f, 0.07f, 0.07f, 0.97f);
            GUI.Box(new Rect(x, y, width, height), GUIContent.none, menuBoxStyle);

            GUILayout.BeginArea(new Rect(x + 24, y + 18, width - 48, height - 36));
            GUILayout.Label("SE AGOTO EL TIEMPO!", menuTitleStyle);
            GUILayout.Space(12);

            bool newRecord = (score > 0 && score >= highScore);
            string recordTag = newRecord ? "  NUEVO RECORD!" : $"  (Mejor: {highScore} pts)";
            GUILayout.Label($"El parque aun necesita limpieza y proteccion comunitaria.\n\n" +
                            $"Puntos alcanzados: {score}{recordTag}\n" +
                            $"Residuos clasificados: {correctlyClassified}/{totalTrash}\n" +
                            $"Errores de clasificacion: {incorrectlyClassified}\n" +
                            $"Mejor racha: x{highestStreak}", menuBodyStyle);
            GUILayout.Space(15);

            if (GUILayout.Button("Reintentar Mision", menuBtnStyle, GUILayout.Height(40)))
            {
                RestartGame();
            }

            GUILayout.EndArea();
        }
    }
    #endregion
}
