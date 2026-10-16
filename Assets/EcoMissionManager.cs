using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EcoMissionType
{
    DepositAnyTrash,      // Clasificar X residuos correctamente
    DepositCategory,      // Clasificar X residuos de una categoría específica (Aprovechables, Orgánico, NoAprovechable)
    ReachStreak,          // Alcanzar una racha de aciertos consecutivos
    ReachEcoPurity,       // Alcanzar cierto porcentaje de pureza
    CollectAnyTrash       // Recoger X residuos del suelo
}

[Serializable]
public class EcoMission
{
    public string title;
    [TextArea(2, 3)]
    public string description;
    public EcoMissionType type;
    public TrashCategory targetCategory;
    public int targetCount = 2;
    public int currentProgress = 0;
    public int bonusPoints = 50;
    public float bonusTime = 15f;
    public bool isCompleted = false;
    public string educationalFact;

    public bool IsComplete => currentProgress >= targetCount;
    public float ProgressPercentage => Mathf.Clamp01((float)currentProgress / Mathf.Max(1, targetCount));
}

public class EcoMissionManager : MonoBehaviour
{
    public static EcoMissionManager Instance { get; private set; }

    [Header("Pureza Ambiental del Parque")]
    [Range(0f, 100f)]
    public float currentPurity = 25f; // Empieza al 25% (Parque descuidado)
    private float targetPurity = 25f;
    public float purityPerCorrectDeposit = 12f; // +12% por cada residuo bien clasificado
    public float purityPenaltyPerMistake = 6f;   // -6% por error de caneca
    public float purityLerpSpeed = 3f;

    [Header("Lista de Misiones Pedagógicas")]
    public List<EcoMission> missions = new List<EcoMission>();
    public int activeMissionIndex = 0;

    [Header("Efectos al Completar")]
    public AudioSource audioSource;
    public AudioClip missionCompleteSfx;

    // Estado de interfaz y animaciones
    private float missionBannerTimer = 0f;
    private string completedMissionTitle = "";
    private GUIStyle purityLabelStyle;
    private GUIStyle missionBoxStyle;
    private GUIStyle missionHeaderStyle;
    private GUIStyle bannerStyle;
    private bool stylesInit = false;

    // Procedural sound
    private static AudioClip proceduralFanfareClip;

    public EcoMission CurrentMission
    {
        get
        {
            if (missions != null && activeMissionIndex >= 0 && activeMissionIndex < missions.Count)
                return missions[activeMissionIndex];
            return null;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        targetPurity = currentPurity;
        SetupDefaultMissions();
    }

    private void Reset()
    {
        SetupDefaultMissions();
    }

    private void SetupDefaultMissions()
    {
        if (missions != null && missions.Count > 0) return;

        missions = new List<EcoMission>
        {
            new EcoMission
            {
                title = "Misión 1: Primeros Pasos",
                description = "Clasifica 2 residuos cualquiera. Puedes usar BOTELLAS, MANZANAS o CAJAS DE PIZZA y llevarlas a sus canecas.",
                type = EcoMissionType.DepositAnyTrash,
                targetCount = 2,
                bonusPoints = 40,
                bonusTime = 12f,
                educationalFact = "¡Cada residuo cuenta! Clasificar desde la fuente reduce hasta un 80% la basura en vertederos."
            },
            new EcoMission
            {
                title = "Misión 2: Botellas al Reciclaje",
                description = "Clasifica 2 objetos Aprovechables. Busca BOTELLAS DE AGUA o BOTELLAS DE VIDRIO AZUL y deposítalas en su caneca.",
                type = EcoMissionType.DepositCategory,
                targetCategory = TrashCategory.Aprovechables,
                targetCount = 2,
                bonusPoints = 50,
                bonusTime = 15f,
                educationalFact = "El plástico y el vidrio se pueden reciclar y transformar para darles una nueva vida útil."
            },
            new EcoMission
            {
                title = "Misión 3: Abono para el Parque",
                description = "Clasifica 2 residuos Orgánicos. Busca RESTOS DE MANZANA o MONTONES ORGÁNICOS y deposítalos en su caneca.",
                type = EcoMissionType.DepositCategory,
                targetCategory = TrashCategory.Organico,
                targetCount = 2,
                bonusPoints = 50,
                bonusTime = 15f,
                educationalFact = "Los residuos orgánicos se convierten en abono natural ideal para mantener verdes los jardines del Parque de la Vida."
            },
            new EcoMission
            {
                title = "Misión 4: Ojo con la Grasa",
                description = "Clasifica 1 residuo No Aprovechable. Busca una CAJA DE PIZZA y deposítala en la caneca correspondiente.",
                type = EcoMissionType.DepositCategory,
                targetCategory = TrashCategory.NoAprovechable,
                targetCount = 1,
                bonusPoints = 60,
                bonusTime = 20f,
                educationalFact = "El cartón es reciclable, pero si está engrasado como las cajas de pizza, debe ir a los No Aprovechables."
            },
            new EcoMission
            {
                title = "Misión 5: Guardián de Valledupar",
                description = "Clasifica el resto de la basura del parque sin equivocarte hasta alcanzar el 100% de Pureza Ambiental.",
                type = EcoMissionType.ReachEcoPurity,
                targetCount = 100,
                bonusPoints = 150,
                bonusTime = 30f,
                educationalFact = "¡El Parque de la Vida de Valledupar está radiante y protegido gracias a ti!"
            }
        };
    }

    private void Update()
    {
        // Interpolación suave de la barra de pureza
        currentPurity = Mathf.MoveTowards(currentPurity, targetPurity, Time.deltaTime * purityLerpSpeed * 15f);

        // Actualizar temporizador de banner de misión completada
        if (missionBannerTimer > 0f)
        {
            missionBannerTimer -= Time.deltaTime;
        }

        // Revisar si la misión activa depende de la pureza
        var active = CurrentMission;
        if (active != null && !active.isCompleted && active.type == EcoMissionType.ReachEcoPurity)
        {
            active.currentProgress = Mathf.FloorToInt(currentPurity);
            if (currentPurity >= active.targetCount)
            {
                CompleteCurrentMission();
            }
        }
    }

    /// <summary>
    /// Llamado cuando el jugador recoge un residuo del suelo
    /// </summary>
    public void OnTrashCollected(TrashData trash)
    {
        var active = CurrentMission;
        if (active != null && !active.isCompleted && active.type == EcoMissionType.CollectAnyTrash)
        {
            active.currentProgress++;
            if (active.IsComplete)
            {
                CompleteCurrentMission();
            }
        }
    }

    /// <summary>
    /// Llamado cuando el jugador deposita un residuo en una caneca
    /// </summary>
    public void OnTrashDeposited(TrashCategory category, bool isCorrect, int currentStreak)
    {
        if (isCorrect)
        {
            targetPurity = Mathf.Clamp(targetPurity + purityPerCorrectDeposit, 0f, 100f);
        }
        else
        {
            targetPurity = Mathf.Clamp(targetPurity - purityPenaltyPerMistake, 0f, 100f);
        }

        var active = CurrentMission;
        if (active == null || active.isCompleted) return;

        bool progressMade = false;

        if (isCorrect)
        {
            if (active.type == EcoMissionType.DepositAnyTrash)
            {
                active.currentProgress++;
                progressMade = true;
            }
            else if (active.type == EcoMissionType.DepositCategory && active.targetCategory == category)
            {
                active.currentProgress++;
                progressMade = true;
            }
            else if (active.type == EcoMissionType.ReachStreak)
            {
                active.currentProgress = currentStreak;
                progressMade = true;
            }
        }
        else
        {
            if (active.type == EcoMissionType.ReachStreak)
            {
                active.currentProgress = 0; // Se resetea el progreso de racha si falló
            }
        }

        if (progressMade && active.IsComplete)
        {
            CompleteCurrentMission();
        }
    }

    private void CompleteCurrentMission()
    {
        var active = CurrentMission;
        if (active == null || active.isCompleted) return;

        active.isCompleted = true;
        completedMissionTitle = active.title;
        missionBannerTimer = 4.5f;

        // Bonificaciones en GameManager
        if (GameManager.instance != null)
        {
            GameManager.instance.score += active.bonusPoints;
            GameManager.instance.timeRemaining += active.bonusTime;
            GameManager.instance.UpdateUI();
        }

        // Sonido de fanfarria
        PlayFanfare();

        // Notificación en InteractionHUD
        if (InteractionHUD.Instance != null)
        {
            InteractionHUD.Instance.ShowNotification(
                $"🏆 <b>¡MISIÓN CUMPLIDA!</b> {active.title} (+{active.bonusPoints} pts, +{active.bonusTime}s)",
                active.educationalFact,
                true,
                5f
            );
            InteractionHUD.Instance.TriggerScreenFlash(true);
        }

        // Efectos en la fuente si se alcanza 100% de pureza o última misión
        if (currentPurity >= 95f || activeMissionIndex >= missions.Count - 1)
        {
            TriggerFountainRestorationEffect();
        }

        // Avanzar a la siguiente misión
        activeMissionIndex++;
    }

    private void PlayFanfare()
    {
        if (audioSource == null) return;

        if (missionCompleteSfx != null)
        {
            audioSource.PlayOneShot(missionCompleteSfx);
        }
        else
        {
            audioSource.PlayOneShot(GetOrCreateProceduralFanfare());
        }
    }

    private void TriggerFountainRestorationEffect()
    {
        GameObject fuente = GameObject.Find("Fuente Parque de la vida");
        if (fuente != null)
        {
            // Crear destellos o partículas sobre la fuente
            GameObject auraObj = new GameObject("FuenteRestoredAura");
            auraObj.transform.position = fuente.transform.position + Vector3.up * 2f;

            ParticleSystem ps = auraObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.5f;
            main.startSpeed = 4f;
            main.startSize = 0.5f;
            main.startColor = new Color(0.2f, 0.8f, 1f, 0.8f);
            main.loop = true;

            var emission = ps.emission;
            emission.rateOverTime = 35;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 5f;

            ps.Play();
        }
    }

    public string GetPurityStatusLabel()
    {
        if (currentPurity >= 95f) return "<color=#50FFA0>✨ Parque Radiante</color>";
        if (currentPurity >= 70f) return "<color=#70E000>🌱 Saludable</color>";
        if (currentPurity >= 40f) return "<color=#FFD166>🌿 En Recuperación</color>";
        return "<color=#FF6B6B>⚠️ Muy Contaminado</color>";
    }

    public Color GetPurityBarColor()
    {
        if (currentPurity >= 80f) return new Color(0.2f, 0.9f, 0.4f);
        if (currentPurity >= 45f) return new Color(0.9f, 0.85f, 0.2f);
        return new Color(0.95f, 0.35f, 0.25f);
    }

    #region Procedural Audio Fanfare
    private static AudioClip GetOrCreateProceduralFanfare()
    {
        if (proceduralFanfareClip != null) return proceduralFanfareClip;

        int sampleRate = 44100;
        float duration = 0.6f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        // Secuencia fanfarria: C5 (523Hz) -> E5 (659Hz) -> G5 (784Hz) -> C6 (1046Hz)
        float[] freqs = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };
        int subLen = samples / 4;

        for (int i = 0; i < samples; i++)
        {
            int note = Mathf.Clamp(i / subLen, 0, 3);
            float freq = freqs[note];
            float t = (float)i / sampleRate;
            float decay = 1f - ((float)(i % subLen) / subLen);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * decay * 0.45f;
        }

        proceduralFanfareClip = AudioClip.Create("MissionFanfare", samples, 1, sampleRate, false);
        proceduralFanfareClip.SetData(data, 0);
        return proceduralFanfareClip;
    }
    #endregion

    #region HUD Drawing (OnGUI)
    private GUIStyle timerStyle;

    private void InitStyles()
    {
        if (stylesInit) return;

        purityLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        purityLabelStyle.normal.textColor = Color.white;

        missionBoxStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(10, 10, 8, 8)
        };

        missionHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        missionHeaderStyle.normal.textColor = new Color(1f, 0.9f, 0.2f);

        timerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        timerStyle.normal.textColor = Color.white;

        bannerStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        bannerStyle.normal.textColor = new Color(1f, 0.95f, 0.3f);

        stylesInit = true;
    }

    private void OnGUI()
    {
        // No dibujar HUD durante modales de pausa, victoria o derrota
        if (GameManager.instance != null && (GameManager.instance.isGameOver || GameManager.instance.isVictory || GameManager.instance.isPaused))
            return;

        InitStyles();

        // 1. Tarjeta Unificada de Estado y Pureza (Esquina Superior Izquierda)
        float hudX = 20f;
        float hudY = 20f;
        float hudWidth = 320f;
        float hudHeight = 115f;

        GUI.backgroundColor = new Color(0.06f, 0.10f, 0.16f, 0.92f);
        GUI.Box(new Rect(hudX, hudY, hudWidth, hudHeight), GUIContent.none, missionBoxStyle);

        GUILayout.BeginArea(new Rect(hudX + 12, hudY + 8, hudWidth - 24, hudHeight - 16));
        GUILayout.Label("🌿 <b>GUARDIANES DEL PARQUE</b>", missionHeaderStyle);

        int collected = (GameManager.instance != null) ? GameManager.instance.collectedTrash : 0;
        int total = (GameManager.instance != null) ? GameManager.instance.totalTrash : 10;
        int score = (GameManager.instance != null) ? GameManager.instance.score : 0;
        int streak = (GameManager.instance != null) ? GameManager.instance.currentStreak : 0;
        string streakTag = (streak >= 2) ? $" <color=#FFD166>[x{streak} 🔥]</color>" : "";

        GUILayout.Label($"<b>Clasificados:</b> {collected}/{total}   |   <b>Puntos:</b> {score}{streakTag}", purityLabelStyle);
        GUILayout.Label($"<b>Pureza:</b> {Mathf.RoundToInt(currentPurity)}% ({GetPurityStatusLabel()})", purityLabelStyle);

        GUILayout.Space(2);
        Rect barRect = GUILayoutUtility.GetRect(hudWidth - 24, 14);
        GUI.backgroundColor = new Color(0.18f, 0.22f, 0.26f, 0.95f);
        GUI.Box(barRect, GUIContent.none);

        float fillWidth = (currentPurity / 100f) * barRect.width;
        if (fillWidth > 2f)
        {
            GUI.backgroundColor = GetPurityBarColor();
            GUI.Box(new Rect(barRect.x + 1, barRect.y + 1, fillWidth - 2, barRect.height - 2), GUIContent.none);
        }
        GUILayout.EndArea();

        // 2. Temporizador Flotante (Centro Superior)
        if (GameManager.instance != null)
        {
            float timeRem = GameManager.instance.timeRemaining;
            int min = Mathf.FloorToInt(timeRem / 60f);
            int sec = Mathf.FloorToInt(timeRem % 60f);
            bool isUrgent = (timeRem <= 20f);

            float timerWidth = 150f;
            float timerHeight = 38f;
            float timerX = (Screen.width - timerWidth) / 2f;
            float timerY = 20f;

            GUI.backgroundColor = isUrgent ? new Color(0.48f, 0.12f, 0.12f, 0.95f) : new Color(0.06f, 0.10f, 0.16f, 0.92f);
            GUI.Box(new Rect(timerX, timerY, timerWidth, timerHeight), GUIContent.none, missionBoxStyle);

            string timeText = isUrgent ? $"<color=#FF6B6B>⚠️ <b>{min:00}:{sec:00}</b></color>" : $"⏱ <b>{min:00}:{sec:00}</b>";
            GUI.Label(new Rect(timerX, timerY, timerWidth, timerHeight), timeText, timerStyle);
        }

        // 3. Tarjeta de Misión Actual (Esquina Superior Derecha)
        var mission = CurrentMission;
        if (mission != null && !mission.isCompleted)
        {
            float mWidth = 360f;
            float mHeight = 125f;
            float mX = Screen.width - mWidth - 20f;
            float mY = 20f;

            GUI.backgroundColor = new Color(0.06f, 0.12f, 0.16f, 0.92f);
            GUILayout.BeginArea(new Rect(mX, mY, mWidth, mHeight), missionBoxStyle);

            GUILayout.Label($"🎯 <b>{mission.title}</b>", missionHeaderStyle);
            GUILayout.Label($"<size=11>{mission.description}</size>");

            GUILayout.Space(2);
            string progText = (mission.type == EcoMissionType.ReachEcoPurity)
                ? $"{Mathf.RoundToInt(currentPurity)}% / 100%"
                : $"{mission.currentProgress} / {mission.targetCount}";

            GUILayout.Label($"<b>Progreso:</b> {progText}  <color=#FFE600>(+{mission.bonusPoints} pts | +{mission.bonusTime}s)</color>", purityLabelStyle);

            GUILayout.EndArea();
        }

        // 4. Banner animado al completar misión (Centro de pantalla)
        if (missionBannerTimer > 0f)
        {
            float bWidth = 460f;
            float bHeight = 55f;
            float bX = (Screen.width - bWidth) / 2f;
            float bY = 125f;

            GUI.backgroundColor = new Color(0.1f, 0.45f, 0.2f, 0.95f);
            GUI.Box(new Rect(bX, bY, bWidth, bHeight), $"MISION CUMPLIDA!\n{completedMissionTitle}", bannerStyle);
        }

        // 5. Indicadores de caneca en borde inferior (solo cuando mochila llena)
        DrawBinEdgeIndicators();
    }

    private PlayerInventory cachedPlayerInv;
    private RecycleBin[] cachedBins;
    private float binCacheTime = 0f;

    private void DrawBinEdgeIndicators()
    {
        var gm = GameManager.instance;
        if (gm == null) return;

        // Buscar inventario del jugador (con cache)
        if (cachedPlayerInv == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                cachedPlayerInv = player.GetComponent<PlayerInventory>();
        }
        if (cachedPlayerInv == null || !cachedPlayerInv.IsFull) return;

        // Cache de bins
        if (cachedBins == null || Time.time > binCacheTime + 5f)
        {
            cachedBins = FindObjectsByType<RecycleBin>(FindObjectsInactive.Exclude);
            binCacheTime = Time.time;
        }
        if (cachedBins == null || cachedBins.Length == 0) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        var playerObj = cachedPlayerInv.gameObject;
        int count = cachedBins.Length;
        float boxW = 88f;
        float boxH = 40f;
        float spacing = 6f;
        float totalW = count * (boxW + spacing) - spacing;
        float startX = (Screen.width - totalW) / 2f;
        float yBase = Screen.height - boxH - 20f;

        GUIStyle binIndStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false
        };

        for (int i = 0; i < count; i++)
        {
            RecycleBin bin = cachedBins[i];
            if (bin == null) continue;

            Vector3 tobin = bin.transform.position - playerObj.transform.position;
            tobin.y = 0;
            float dist = tobin.magnitude;

            string catName = bin.GetAcceptedCategoryName();
            string distLabel = dist < 5f ? "CERCA" : $"{Mathf.RoundToInt(dist)}m";
            string label = $"{catName}\n{distLabel}";

            Color binCol = bin.binColor;
            float boxX = startX + i * (boxW + spacing);

            GUI.backgroundColor = new Color(binCol.r * 0.25f, binCol.g * 0.25f, binCol.b * 0.25f, 0.90f);
            binIndStyle.normal.textColor = binCol;
            GUI.Box(new Rect(boxX, yBase, boxW, boxH), label, binIndStyle);
        }
    }
    #endregion
}