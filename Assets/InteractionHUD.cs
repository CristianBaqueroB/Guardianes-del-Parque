using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractionHUD : MonoBehaviour
{
    public static InteractionHUD Instance { get; private set; }

    [Header("Referencias UGUI Opcionales")]
    [SerializeField] private GameObject promptContainer;
    [SerializeField] private TextMeshProUGUI promptText;

    [SerializeField] private GameObject notificationContainer;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private TextMeshProUGUI educationalFactText;

    [SerializeField] private TextMeshProUGUI inventoryText;

    // Estado interno para dibujo y runtime
    private string currentPrompt = "";
    private string activeNotification = "";
    private string activeFact = "";
    private bool isNotificationSuccess = true;
    private float notificationTimer = 0f;

    // Flash de pantalla (feedback visual inmediato)
    private float screenFlashTimer = 0f;
    private float screenFlashDuration = 0.35f;
    private Color screenFlashColor = Color.clear;

    private PlayerInventory cachedInventory;

    // Estilos OnGUI
    private GUIStyle promptBoxStyle;
    private GUIStyle notifBoxStyle;
    private GUIStyle invBoxStyle;
    private bool stylesInitialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        HidePrompt();

        if (notificationContainer != null)
            notificationContainer.SetActive(false);

        AutoWireReferences();
    }

    private void Update()
    {
        if (notificationTimer > 0f)
        {
            notificationTimer -= Time.deltaTime;
            if (notificationTimer <= 0f)
            {
                activeNotification = "";
                activeFact = "";
                if (notificationContainer != null) notificationContainer.SetActive(false);
            }
        }

        if (screenFlashTimer > 0f)
        {
            screenFlashTimer -= Time.deltaTime;
        }
    }

    public void TriggerScreenFlash(bool isSuccess)
    {
        screenFlashDuration = 0.35f;
        screenFlashTimer = screenFlashDuration;
        screenFlashColor = isSuccess ? new Color(0.1f, 0.9f, 0.3f, 0.28f) : new Color(1f, 0.15f, 0.15f, 0.35f);
    }

    private void AutoWireReferences()
    {
        if (promptText == null)
        {
            var pText = transform.Find("PromptContainer/PromptText");
            if (pText != null) promptText = pText.GetComponent<TextMeshProUGUI>();
        }
        if (promptContainer == null && promptText != null)
        {
            promptContainer = promptText.transform.parent.gameObject;
        }
    }

    public void ShowPrompt(string actionText, string key = "E")
    {
        currentPrompt = $"[{key}] {actionText}";

        if (promptContainer != null) promptContainer.SetActive(true);
        if (promptText != null)
        {
            promptText.text = $"<color=#FFE600><b>[{key}]</b></color> {actionText}";
        }
    }

    public void HidePrompt()
    {
        currentPrompt = "";
        if (promptContainer != null) promptContainer.SetActive(false);
        else if (promptText != null) promptText.text = "";
    }

    public void ShowNotification(string message, string educationalFact = null, bool isSuccess = true, float duration = 3.5f)
    {
        activeNotification = message;
        activeFact = educationalFact ?? "";
        isNotificationSuccess = isSuccess;
        notificationTimer = duration;

        if (notificationContainer != null)
        {
            notificationContainer.SetActive(true);
            string colorHex = isSuccess ? "#40FF70" : "#FF5050";
            if (notificationText != null) notificationText.text = $"<color={colorHex}>{message}</color>";

            if (educationalFactText != null)
            {
                if (!string.IsNullOrEmpty(educationalFact))
                {
                    educationalFactText.gameObject.SetActive(true);
                    educationalFactText.text = $"💡 <i>{educationalFact}</i>";
                }
                else
                {
                    educationalFactText.gameObject.SetActive(false);
                }
            }
        }
    }

    public void UpdateInventoryDisplay(PlayerInventory inventory)
    {
        cachedInventory = inventory;

        if (inventoryText == null) return;

        if (inventory == null || inventory.IsEmpty)
        {
            inventoryText.text = "🎒 <b>Mochila:</b> Vacía (0/" + (inventory != null ? inventory.MaxCapacity : 3) + ")";
            return;
        }

        string result = $"🎒 <b>Mochila ({inventory.CurrentCount}/{inventory.MaxCapacity}):</b>\n";
        for (int i = 0; i < inventory.Items.Count; i++)
        {
            var item = inventory.Items[i];
            bool isSelected = (i == inventory.SelectedIndex);

            string categoryColor = "#FFFFFF";
            switch (item.category)
            {
                case TrashCategory.Aprovechables: categoryColor = "#3399FF"; break; // Azul
                case TrashCategory.Organico: categoryColor = "#70E000"; break;     // Verde
                case TrashCategory.NoAprovechable: categoryColor = "#999999"; break; // Gris
            }

            string pointer = isSelected ? "<color=#FFE600>▶</color> " : "    ";
            string activeTag = isSelected ? " <color=#FFE600>[EN MANO]</color>" : "";
            result += $"{pointer}• <color={categoryColor}><b>{item.name}</b></color>{activeTag}\n";
        }

        if (inventory.CurrentCount > 1)
        {
            result += "<size=11><color=#FFD166><i>[Q] o Rueda: Alternar residuo</i></color></size>";
        }

        inventoryText.text = result.TrimEnd();
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        promptBoxStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        promptBoxStyle.normal.textColor = Color.yellow;

        notifBoxStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };

        invBoxStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 13,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        invBoxStyle.normal.textColor = Color.white;

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        // No dibujar HUD durante modales de pausa, victoria o derrota
        if (GameManager.instance != null && (GameManager.instance.isGameOver || GameManager.instance.isVictory || GameManager.instance.isPaused))
            return;

        // 0. Flash de Pantalla (Efecto de acierto verde o error rojo)
        if (screenFlashTimer > 0f)
        {
            float alphaProgress = Mathf.Clamp01(screenFlashTimer / screenFlashDuration);
            Color drawColor = new Color(screenFlashColor.r, screenFlashColor.g, screenFlashColor.b, screenFlashColor.a * alphaProgress);
            GUI.color = drawColor;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Si no hay UGUI asignada para los textos, renderizar un HUD nativo claro y estilizado
        InitStyles();

        // 1. Prompt de Interacción (Centro inferior)
        if (!string.IsNullOrEmpty(currentPrompt) && promptText == null)
        {
            float width = Mathf.Max(360, currentPrompt.Length * 11);
            float height = 48;
            float x = (Screen.width - width) / 2f;
            float y = Screen.height - 110f;

            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.88f);
            GUI.Box(new Rect(x, y, width, height), currentPrompt, promptBoxStyle);
        }

        // 2. Notificación y Dato Pedagógico (Centro superior, debajo del temporizador)
        if (!string.IsNullOrEmpty(activeNotification) && notificationText == null)
        {
            float width = 540;
            float height = string.IsNullOrEmpty(activeFact) ? 45 : 78;
            float x = (Screen.width - width) / 2f;
            float y = 68f;

            GUI.backgroundColor = isNotificationSuccess ? new Color(0.1f, 0.35f, 0.15f, 0.92f) : new Color(0.48f, 0.12f, 0.1f, 0.92f);
            notifBoxStyle.normal.textColor = isNotificationSuccess ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.45f, 0.45f);

            string content = activeNotification;
            if (!string.IsNullOrEmpty(activeFact))
            {
                content += $"\n💡 {activeFact}";
            }

            GUI.Box(new Rect(x, y, width, height), content, notifBoxStyle);
        }

        // 3. Mini-HUD de Mochila / Inventario (Esquina inferior derecha)
        if (cachedInventory != null && inventoryText == null)
        {
            float width = 260;
            float extraHeight = (cachedInventory.CurrentCount > 1) ? 22 : 0;
            float height = 35 + (cachedInventory.CurrentCount * 22) + extraHeight;
            float x = Screen.width - width - 20;
            float y = Screen.height - height - 20;

            GUI.backgroundColor = new Color(0.08f, 0.12f, 0.16f, 0.88f);

            string invContent = $"🎒 Mochila ({cachedInventory.CurrentCount}/{cachedInventory.MaxCapacity}):\n";
            if (cachedInventory.IsEmpty)
            {
                invContent += " <i>(Vacía - Recoge basura)</i>";
            }
            else
            {
                for (int i = 0; i < cachedInventory.Items.Count; i++)
                {
                    var item = cachedInventory.Items[i];
                    bool isSelected = (i == cachedInventory.SelectedIndex);
                    string selTag = isSelected ? "▶ " : "    ";
                    string handTag = isSelected ? " [EN MANO]" : "";
                    invContent += $"{selTag}• {item.name} [{item.category}]{handTag}\n";
                }

                if (cachedInventory.CurrentCount > 1)
                {
                    invContent += "<i>[Q] o Rueda: Alternar residuo</i>";
                }
            }

            GUI.Box(new Rect(x, y, width, height), invContent.TrimEnd(), invBoxStyle);
        }
    }
}