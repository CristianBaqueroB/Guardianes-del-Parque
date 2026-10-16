using UnityEngine;

public class ParkSignInteractive : MonoBehaviour
{
    [Header("Configuración del Cartel")]
    public float interactionDistance = 3.5f;

    [Header("Fotografía de la Noticia (Arrastra aquí tu imagen 'Rio')")]
    public Texture2D signImage;

    [Header("Contenido de la Noticia Ecológica")]
    [TextArea(2, 4)]
    public string newspaperHeader = "EL PILÓN VERDE • EDICIÓN ESPECIAL AMBIENTAL";
    [TextArea(3, 5)]
    public string signTitle = "El Pulso del Río Guatapurí y el Parque de la Vida";
    [TextArea(6, 12)]
    public string signStory = "El río Guatapurí es el corazón hídrico y cultural de Valledupar. Sin embargo, el mal manejo de residuos plásticos y la deforestación en su cuenca alta amenazan su caudal y la fauna local.\n\nExpertos ambientales señalan que proteger el Parque de la Vida mediante una correcta separación de residuos en los Puntos Limpios es una tarea urgente para preservar nuestras fuentes hídricas y el bienestar de los vallenatos.";

    private Transform playerTransform;
    private bool isPlayerNearby = false;
    private bool isReading = false;

    // Estilos visuales tipo periódico
    private GUIStyle paperBoxStyle;
    private GUIStyle headerStyle;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle promptStyle;
    private bool stylesInit = false;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            return;
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        isPlayerNearby = (dist <= interactionDistance);

        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            isReading = !isReading;
        }

        if (!isPlayerNearby && isReading)
        {
            isReading = false;
        }
    }

    private void InitStyles()
    {
        if (stylesInit) return;

        paperBoxStyle = new GUIStyle(GUI.skin.box);

        // --- LETRAS BLANCAS ---
        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        headerStyle.normal.textColor = Color.white; // Blanco

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        titleStyle.normal.textColor = Color.white; // Blanco

        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        bodyStyle.normal.textColor = Color.white; // Blanco

        promptStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        promptStyle.normal.textColor = new Color(1f, 0.9f, 0.3f); // Amarillo destacado

        stylesInit = true;
    }

    private void OnGUI()
    {
        if (GameManager.instance != null && (GameManager.instance.isGameOver || GameManager.instance.isVictory || GameManager.instance.isPaused))
            return;

        InitStyles();

        // 1. Aviso flotante [E]
        if (isPlayerNearby && !isReading)
        {
            float w = 280f;
            float h = 38f;
            float x = (Screen.width - w) / 2f;
            float y = Screen.height - 140f;

            GUI.backgroundColor = new Color(0.08f, 0.12f, 0.2f, 0.9f);
            GUI.Box(new Rect(x, y, w, h), "[E] Leer Artículo de Prensa", promptStyle);
        }

        // 2. Ventana emergente tipo nota de periódico con imagen
        if (isReading)
        {
            float panelW = 640f;
            float panelH = 360f;
            float panelX = (Screen.width - panelW) / 2f;
            float panelY = (Screen.height - panelH) / 2f;

            // Fondo oscuro elegante para resaltar el texto blanco
            GUI.backgroundColor = new Color(0.08f, 0.12f, 0.18f, 0.96f);
            GUI.Box(new Rect(panelX, panelY, panelW, panelH), GUIContent.none, paperBoxStyle);

            GUI.backgroundColor = new Color(1f, 0.9f, 0.3f, 1f); // Línea decorativa dorada
            GUI.Box(new Rect(panelX, panelY, panelW, 3f), GUIContent.none);
            GUI.Box(new Rect(panelX, panelY + panelH - 3f, panelW, 3f), GUIContent.none);

            GUILayout.BeginArea(new Rect(panelX + 20, panelY + 15, panelW - 40, panelH - 30));

            GUILayout.Label($"--- {newspaperHeader} ---", headerStyle);
            GUILayout.Space(4);

            GUILayout.Label(signTitle, titleStyle);
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();

            // Sección de la Imagen de la Noticia
            GUILayout.BeginVertical(GUILayout.Width(180f));
            if (signImage != null)
            {
                Rect imgRect = GUILayoutUtility.GetRect(180f, 130f);
                GUI.DrawTexture(imgRect, signImage, ScaleMode.ScaleToFit);
            }
            else
            {
                GUILayout.Box("[ Sin Fotografía ]\nArrastra la imagen 'Rio'\nen el Inspector", GUILayout.Width(180f), GUILayout.Height(120f));
            }
            GUILayout.EndVertical();

            GUILayout.Space(15);

            GUILayout.BeginVertical();
            GUILayout.Label(signStory, bodyStyle);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUIStyle footerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic
            };
            footerStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            GUILayout.Label("Presiona <b>[E]</b> o aléjate para cerrar la nota de prensa", footerStyle);

            GUILayout.EndArea();
        }
    }
}