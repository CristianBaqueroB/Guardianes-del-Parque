using System.Collections;
using UnityEngine;

public class RecycleBin : MonoBehaviour
{
    [Header("Configuración de Caneca")]
    public TrashCategory acceptedCategory = TrashCategory.Aprovechables;
    public string binName = "Caneca de Aprovechables";

    [Header("Recompensas y Penalizaciones")]
    public int correctBonusPoints = 20;
    public float timeBonus = 6f;
    public int incorrectPenaltyPoints = 10;
    public float incorrectTimePenalty = 5f;

    [Header("Efectos Opcionales")]
    public ParticleSystem depositParticles;
    public AudioSource audioSource;
    public AudioClip correctSfx;
    public AudioClip incorrectSfx;

    [Header("Identificador Visual")]
    public Color binColor = new Color(0.2f, 0.6f, 1f);

    private bool isPlayerNear = false;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Coroutine activeAnimRoutine;

    // Clips procedurales cacheados
    private static AudioClip proceduralSuccessClip;
    private static AudioClip proceduralFailClip;

    private void Awake()
    {
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;

        EnsureTriggerCollider();
        EnsureAudioSource();
    }

    private void Start()
    {
        // Si no se asignó nombre, dar uno por defecto según la categoría
        if (string.IsNullOrEmpty(binName) || binName == "Caneca")
        {
            switch (acceptedCategory)
            {
                case TrashCategory.Aprovechables: 
                    binName = "Punto Limpio - Aprovechables (Blanco)"; 
                    binColor = new Color(0.2f, 0.6f, 1f);
                    break;
                case TrashCategory.Organico: 
                    binName = "Punto Limpio - Orgánicos (Verde)"; 
                    binColor = new Color(0.2f, 0.85f, 0.2f);
                    break;
                //case TrashCategory.Vidrio: 
                //    binName = "Punto Limpio - Vidrio (Amarillo)"; 
                //    binColor = new Color(1f, 0.85f, 0.1f);
                //    break;
                case TrashCategory.NoAprovechable: 
                    binName = "Punto Limpio - No Aprovechable (Negro)"; 
                    binColor = new Color(0.3f, 0.3f, 0.3f);
                    break;
            }
        }

        CreateFloatingSign();
    }

    /// <summary>
    /// Garantiza que exista un Collider en modo trigger para que el Player lo detecte sin requerir setup manual
    /// </summary>
    private void EnsureTriggerCollider()
    {
        Collider existingCol = GetComponent<Collider>();
        if (existingCol == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(0.015f, 0.015f, 0.015f); // Adaptado a escalas grandes como 100
            box.center = new Vector3(0f, 0.005f, 0f);
        }
        else if (!existingCol.isTrigger)
        {
            // Si tiene collider sólido pero no trigger, añadir un SphereCollider trigger adicional
            SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.015f;
        }
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0.5f;
                audioSource.playOnAwake = false;
            }
        }
    }

    /// <summary>
    /// Crea un letrero o indicador 3D sobre la caneca para identificarla a distancia
    /// </summary>
    private void CreateFloatingSign()
    {
        Transform existingSign = transform.Find("FloatingBinSign");
        if (existingSign != null) return;

        GameObject signObj = new GameObject("FloatingBinSign");
        signObj.transform.SetParent(transform);
        // Colocar sobre la caneca (tomando en cuenta escala de transform)
        signObj.transform.localPosition = new Vector3(0, 0.012f, 0);

        TextMesh tm = signObj.AddComponent<TextMesh>();
        tm.text = $"[{GetAcceptedCategoryName().ToUpper()}]";
        tm.fontSize = 28;
        tm.characterSize = 0.001f;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = binColor;

        // Script simple para orientarse a la cámara principal
        signObj.AddComponent<BillboardLookAtCamera>();
    }

    public bool IsPlayerNear() => isPlayerNear;

    public struct DepositResult
    {
        public bool isCorrect;
        public string message;
        public int pointsAwarded;
        public float timeBonus;
        public float multiplier;
        public string educationalFact;
    }

    public DepositResult ProcessDeposit(CarriedTrash trash)
    {
        DepositResult result = new DepositResult();

        if (trash.category == acceptedCategory)
        {
            result.isCorrect = true;
            int basePoints = correctBonusPoints + trash.points;
            float baseBonusTime = timeBonus + trash.timeBonus;

            // Consultar multiplicador actual de GameManager si existe
            float mult = (GameManager.instance != null) ? GameManager.instance.GetCurrentMultiplier() : 1f;
            result.multiplier = mult;
            result.pointsAwarded = Mathf.RoundToInt(basePoints * mult);
            result.timeBonus = baseBonusTime;

            string comboMsg = (mult > 1f) ? $" (¡Combo x{mult:F1}!)" : "";
            result.message = $"¡Excelente! Clasificaste {trash.name} en {binName}. (+{result.pointsAwarded} pts{comboMsg})";
            result.educationalFact = trash.educationalFact;

            PlayFeedback(true, result.pointsAwarded, mult);
        }
        else
        {
            result.isCorrect = false;
            result.pointsAwarded = -incorrectPenaltyPoints;
            result.timeBonus = -incorrectTimePenalty;
            result.multiplier = 1f;
            result.message = $"¡Cuidado! {trash.name} es {trash.category}, no debe ir en {binName}. (-{incorrectPenaltyPoints} pts, -{incorrectTimePenalty}s)";
            result.educationalFact = $"Recuerda: Los residuos de tipo {trash.category} deben ir en su contenedor correspondiente.";

            PlayFeedback(false, -incorrectPenaltyPoints, 1f);
        }

        return result;
    }

    public DepositResult ProcessDeposit(TrashData trash)
    {
        return ProcessDeposit(new CarriedTrash(trash));
    }

    /// <summary>
    /// Ejecuta todos los efectos audiovisuales y de "jugosidad" (Juice):
    /// Rebote elástico, sacudida de rechazo, sonidos y textos flotantes.
    /// </summary>
    public void PlayFeedback(bool isCorrect, int deltaPoints, float multiplier)
    {
        // 1. Animación elástica de la caneca
        if (activeAnimRoutine != null) StopCoroutine(activeAnimRoutine);

        if (isCorrect)
        {
            activeAnimRoutine = StartCoroutine(SquashAndStretchRoutine());
            PlaySound(true);

            if (depositParticles != null) depositParticles.Play();
            else SpawnProceduralParticleBurst();

            SpawnFloatingText($"+{deltaPoints} PTS" + (multiplier > 1f ? $" (x{multiplier:F1} 🔥)" : ""), Color.green);
        }
        else
        {
            activeAnimRoutine = StartCoroutine(ShakeRejectionRoutine());
            PlaySound(false);

            SpawnFloatingText($"{deltaPoints} PTS ❌", new Color(1f, 0.3f, 0.3f));
        }
    }

    private void PlaySound(bool isSuccess)
    {
        if (audioSource == null) return;

        if (isSuccess)
        {
            if (correctSfx != null) audioSource.PlayOneShot(correctSfx);
            else audioSource.PlayOneShot(GetOrCreateProceduralSuccessClip());
        }
        else
        {
            if (incorrectSfx != null) audioSource.PlayOneShot(incorrectSfx);
            else audioSource.PlayOneShot(GetOrCreateProceduralFailClip());
        }
    }

    /// <summary>
    /// Animación jugosa de squash y stretch (rebote alegre)
    /// </summary>
    private IEnumerator SquashAndStretchRoutine()
    {
        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Curva elástica de rebote
            float squash = Mathf.Sin(t * Mathf.PI * 2f) * Mathf.Exp(-t * 3f) * 0.25f;

            Vector3 newScale = new Vector3(
                originalScale.x * (1f + squash * 0.5f),
                originalScale.y * (1f - squash),
                originalScale.z * (1f + squash * 0.5f)
            );

            transform.localScale = newScale;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    /// <summary>
    /// Animación de sacudida lateral (rechazo cómico/alerta)
    /// </summary>
    private IEnumerator ShakeRejectionRoutine()
    {
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Sin(elapsed * 45f) * 8f * (1f - (elapsed / duration));
            transform.localRotation = originalRotation * Quaternion.Euler(0, angle, 0);
            yield return null;
        }

        transform.localRotation = originalRotation;
    }

    /// <summary>
    /// Genera un destello temporal de partículas sin necesidad de prefabs externos
    /// </summary>
    private void SpawnProceduralParticleBurst()
    {
        GameObject burstObj = new GameObject("RecycleParticleBurst");
        burstObj.transform.position = transform.position + Vector3.up * 1.2f;

        ParticleSystem ps = burstObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.6f;
        main.startSpeed = 3f;
        main.startSize = 0.25f;
        main.startColor = binColor;
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        ps.Play();
        Destroy(burstObj, 1.2f);
    }

    /// <summary>
    /// Genera un texto 3D flotante que asciende y se desvanece
    /// </summary>
    private void SpawnFloatingText(string text, Color color)
    {
        GameObject popup = new GameObject("FloatingScorePopup");
        popup.transform.position = transform.position + Vector3.up * 1.5f;

        TextMesh tm = popup.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 32;
        tm.characterSize = 0.08f;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = color;

        popup.AddComponent<BillboardLookAtCamera>();
        popup.AddComponent<FloatingScoreFader>();
    }

    #region Procedural Audio Synthesis (Respaldo en caso de no haber AudioClips)
    private static AudioClip GetOrCreateProceduralSuccessClip()
    {
        if (proceduralSuccessClip != null) return proceduralSuccessClip;

        int sampleRate = 44100;
        float duration = 0.28f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        // Tres notas ascendentes: C5 (523Hz), E5 (659Hz), G5 (783Hz)
        float[] freqs = new float[] { 523.25f, 659.25f, 783.99f };
        int subLen = samples / 3;

        for (int i = 0; i < samples; i++)
        {
            int noteIdx = Mathf.Clamp(i / subLen, 0, 2);
            float freq = freqs[noteIdx];
            float time = (float)i / sampleRate;
            float env = 1f - ((float)(i % subLen) / subLen); // Decay rápido
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * time) * env * 0.45f;
        }

        proceduralSuccessClip = AudioClip.Create("SuccessChime", samples, 1, sampleRate, false);
        proceduralSuccessClip.SetData(data, 0);
        return proceduralSuccessClip;
    }

    private static AudioClip GetOrCreateProceduralFailClip()
    {
        if (proceduralFailClip != null) return proceduralFailClip;

        int sampleRate = 44100;
        float duration = 0.22f;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];

        // Zumbido grave descendente (140Hz -> 85Hz)
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(140f, 85f, t);
            float time = (float)i / sampleRate;
            float env = 1f - t;
            // Mezcla senoidal con armónico para sonido de "error"
            data[i] = (Mathf.Sin(2f * Mathf.PI * freq * time) * 0.6f + Mathf.Sin(4f * Mathf.PI * freq * time) * 0.4f) * env * 0.45f;
        }

        proceduralFailClip = AudioClip.Create("ErrorBuzz", samples, 1, sampleRate, false);
        proceduralFailClip.SetData(data, 0);
        return proceduralFailClip;
    }
    #endregion

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
        }
    }

    public string GetAcceptedCategoryName()
    {
        switch (acceptedCategory)
        {
            case TrashCategory.Aprovechables: return "Aprovechables";
            case TrashCategory.Organico: return "Orgánico";
            case TrashCategory.NoAprovechable: return "No Aprovechable";
            default: return "General";
        }
    }
}

/// <summary>
/// Mantiene los letreros y textos 3D siempre orientados hacia la cámara del jugador
/// </summary>
public class BillboardLookAtCamera : MonoBehaviour
{
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null)
        {
            transform.rotation = mainCam.transform.rotation;
        }
    }
}

/// <summary>
/// Anima el texto flotante de puntuación hacia arriba y lo destruye suavemente
/// </summary>
public class FloatingScoreFader : MonoBehaviour
{
    private TextMesh textMesh;
    private Color startColor;
    private float lifetime = 1.2f;
    private float timer = 0f;
    private Vector3 floatSpeed = new Vector3(0, 1.4f, 0);

    private void Start()
    {
        textMesh = GetComponent<TextMesh>();
        if (textMesh != null) startColor = textMesh.color;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        transform.position += floatSpeed * Time.deltaTime;

        if (textMesh != null)
        {
            float alpha = Mathf.Clamp01(1f - (timer / lifetime));
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
        }

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
