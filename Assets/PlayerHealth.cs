using UnityEngine;

/// <summary>
/// Sistema de vida del jugador.
/// El contaminador (EnemyController) llama a TakeDamage() para reducir la salud.
/// Al llegar a 0 HP se activa GameOver.
/// Se dibuja una barra de vida via OnGUI.
/// </summary>
[RequireComponent(typeof(PlayerInteraction))]
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Header("Salud")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Invulnerabilidad post-golpe")]
    public float invincibilityDuration = 2f;
    private float invincibilityTimer = 0f;

    [Header("Regeneracion (opcional)")]
    public bool enableRegen = false;
    public float regenRate = 2f;       // HP por segundo
    public float regenDelay = 8f;      // Segundos sin dano antes de regenerar
    private float regenDelayTimer = 0f;

    // Flash de dano
    private float damageFlashTimer = 0f;
    private const float DAMAGE_FLASH_DURATION = 0.4f;

    // Estilos OnGUI
    private GUIStyle hpLabelStyle;
    private Texture2D hpBarFull;
    private Texture2D hpBarBg;
    private bool stylesInit = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (invincibilityTimer > 0f)
            invincibilityTimer -= dt;

        if (damageFlashTimer > 0f)
            damageFlashTimer -= dt;

        if (enableRegen && currentHealth < maxHealth && currentHealth > 0f)
        {
            if (regenDelayTimer > 0f)
                regenDelayTimer -= dt;
            else
                currentHealth = Mathf.Min(maxHealth, currentHealth + regenRate * dt);
        }
    }

    public bool IsAlive => currentHealth > 0f;
    public bool IsInvincible => invincibilityTimer > 0f;
    public float HealthPercent => currentHealth / maxHealth;

    /// <summary>Aplica dano al jugador. Llama esto desde EnemyController.</summary>
    public void TakeDamage(float amount)
    {
        if (!IsAlive || IsInvincible) return;
        if (GameManager.instance != null && !GameManager.instance.IsGameStarted()) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        invincibilityTimer = invincibilityDuration;
        damageFlashTimer = DAMAGE_FLASH_DURATION;
        regenDelayTimer = regenDelay;

        // Notificacion HUD
        if (InteractionHUD.Instance != null)
        {
            int remaining = Mathf.RoundToInt(currentHealth);
            InteractionHUD.Instance.ShowNotification(
                $"El contaminador te ataco! Vida: {remaining}/{Mathf.RoundToInt(maxHealth)}",
                null, false, 2.5f);
            InteractionHUD.Instance.TriggerScreenFlash(false);
        }

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    private void Die()
    {
        if (GameManager.instance != null)
            GameManager.instance.GameOver();
    }

    private void InitStyles()
    {
        if (stylesInit) return;

        hpLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        hpLabelStyle.normal.textColor = Color.white;

        hpBarBg = new Texture2D(1, 1);
        hpBarBg.SetPixel(0, 0, new Color(0.18f, 0.06f, 0.06f, 0.88f));
        hpBarBg.Apply();

        hpBarFull = new Texture2D(1, 1);
        hpBarFull.SetPixel(0, 0, new Color(0.85f, 0.15f, 0.15f, 0.92f));
        hpBarFull.Apply();

        stylesInit = true;
    }

    private void OnGUI()
    {
        if (GameManager.instance != null &&
            (GameManager.instance.isGameOver || GameManager.instance.isVictory ||
             GameManager.instance.isPaused || !GameManager.instance.IsGameStarted()))
            return;

        InitStyles();

        // Barra de vida (esquina inferior izquierda)
        float barW = 200f;
        float barH = 16f;
        float bx = 20f;
        float by = Screen.height - 52f;

        // Flash de dano (brillo rojo)
        if (damageFlashTimer > 0f)
        {
            float a = (damageFlashTimer / DAMAGE_FLASH_DURATION) * 0.45f;
            GUI.color = new Color(1f, 0f, 0f, a);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Fondo de la barra
        GUI.DrawTexture(new Rect(bx, by, barW, barH), hpBarBg);

        // Relleno segun HP actual
        float fillW = (currentHealth / maxHealth) * (barW - 2f);
        Color barColor;
        if (HealthPercent > 0.6f)      barColor = new Color(0.2f, 0.85f, 0.3f);
        else if (HealthPercent > 0.3f) barColor = new Color(0.95f, 0.75f, 0.1f);
        else                            barColor = new Color(1f, 0.2f, 0.2f);

        if (fillW > 0f)
        {
            Texture2D fillTex = new Texture2D(1, 1);
            fillTex.SetPixel(0, 0, barColor);
            fillTex.Apply();
            GUI.DrawTexture(new Rect(bx + 1, by + 1, fillW, barH - 2), fillTex);
        }

        // Etiqueta HP
        int hp = Mathf.RoundToInt(currentHealth);
        int maxHp = Mathf.RoundToInt(maxHealth);
        GUI.Label(new Rect(bx, by - 16, barW, 15), $"VIDA: {hp}/{maxHp}", hpLabelStyle);

        // Icono invulnerable
        if (IsInvincible)
        {
            GUI.color = new Color(1f, 1f, 0.3f, 0.8f);
            GUI.Label(new Rect(bx + barW + 5, by - 1, 80, 18),
                "[PROTEGIDO]", hpLabelStyle);
            GUI.color = Color.white;
        }
    }
}
