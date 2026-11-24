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
    private GUIStyle slotBoxStyle;
    private GUIStyle slotTitleStyle;
    private GUIStyle slotHintStyle;
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

        slotBoxStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 11,
            alignment = TextAnchor.UpperCenter,
            wordWrap = true,
            richText = true,
            padding = new RectOffset(6, 6, 6, 6)
        };
        slotBoxStyle.normal.textColor = Color.white;

        slotTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight,
            richText = true
        };
        slotTitleStyle.normal.textColor = Color.white;

        slotHintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleRight
        };
        slotHintStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

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

        float scale = Mathf.Clamp(Screen.height / 720f, 0.75f, 2.5f);
        float virtualW = Screen.width / scale;
        float virtualH = Screen.height / scale;
        Matrix4x4 prevMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

        // 1. Prompt de Interacción (Centro inferior, por encima de los indicadores de canecas)
        if (!string.IsNullOrEmpty(currentPrompt) && promptText == null)
        {
            float width = Mathf.Max(360, currentPrompt.Length * 11);
            float height = 44;
            float x = (virtualW - width) / 2f;
            float y = virtualH - 122f;

            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.88f);
            GUI.Box(new Rect(x, y, width, height), currentPrompt, promptBoxStyle);
        }

        // 2. Notificación y Dato Pedagógico (Centro superior, debajo del temporizador)
        if (!string.IsNullOrEmpty(activeNotification) && notificationText == null)
        {
            float width = 540;
            float height = string.IsNullOrEmpty(activeFact) ? 45 : 78;
            float x = (virtualW - width) / 2f;
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

        // 3. Hotbar Visual de Mochila / Inventario (Esquina inferior derecha)
        if (cachedInventory != null && inventoryText == null)
        {
            int maxCap = Mathf.Max(1, cachedInventory.MaxCapacity);
            float slotW = 104f;
            float slotH = 66f;
            float spacing = 6f;
            float totalW = maxCap * slotW + (maxCap - 1) * spacing;
            float startX = virtualW - totalW - 20f;
            float startY = virtualH - slotH - 20f;

            // Encabezado encima de los slots
            GUI.Label(
                new Rect(startX, startY - 22f, totalW, 20f),
                $"🎒 Mochila ({cachedInventory.CurrentCount}/{maxCap})   <color=#FFD166>[Q / Rueda / 1-3]</color>",
                slotTitleStyle
            );

            for (int i = 0; i < maxCap; i++)
            {
                float sx = startX + i * (slotW + spacing);
                bool hasItem = i < cachedInventory.Items.Count;
                bool isSelected = hasItem && (i == cachedInventory.SelectedIndex);

                Color catColor = new Color(0.4f, 0.45f, 0.5f);
                string slotContent = $"<color=#8899AA>[{i + 1}]</color>\n<i>Vacío</i>";

                if (hasItem)
                {
                    var item = cachedInventory.Items[i];
                    string catHex = "#FFFFFF";
                    switch (item.category)
                    {
                        case TrashCategory.Aprovechables:
                            catColor = new Color(0.2f, 0.6f, 1f);
                            catHex = "#66B2FF";
                            break;
                        case TrashCategory.Organico:
                            catColor = new Color(0.4f, 0.88f, 0.15f);
                            catHex = "#80F040";
                            break;
                        case TrashCategory.NoAprovechable:
                            catColor = new Color(0.62f, 0.62f, 0.62f);
                            catHex = "#CCCCCC";
                            break;
                    }

                    string activeTag = isSelected ? "\n<color=#FFE600><b>[EN MANO]</b></color>" : "";
                    slotContent = $"<color=#FFE600><b>[{i + 1}]</b></color> <color={catHex}><b>{item.name}</b></color>{activeTag}";
                }

                GUI.backgroundColor = isSelected
                    ? new Color(0.18f, 0.28f, 0.38f, 0.96f)
                    : new Color(0.07f, 0.10f, 0.15f, 0.85f);

                GUI.Box(new Rect(sx, startY, slotW, slotH), slotContent, slotBoxStyle);

                // Barra superior de color por categoría (o borde dorado si está seleccionado)
                if (hasItem || isSelected)
                {
                    GUI.color = isSelected ? new Color(1f, 0.88f, 0.2f, 0.95f) : catColor;
                    GUI.DrawTexture(new Rect(sx + 2f, startY + 2f, slotW - 4f, 4f), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
            }
        }

        GUI.matrix = prevMatrix;
    }
}