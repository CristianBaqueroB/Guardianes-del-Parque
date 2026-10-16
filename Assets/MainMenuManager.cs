using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor de pantalla de inicio y tutorial.
/// Se muestra al arrancar la escena y bloquea el juego hasta que el jugador presione JUGAR.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    // Estado
    private bool showMenu = true;
    private int tutorialPage = 0;
    private const int TOTAL_PAGES = 3;

    // Estilos
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle btnPrimary;
    private GUIStyle btnSecondary;
    private GUIStyle boxStyle;
    private GUIStyle pageStyle;
    private bool stylesInit = false;

    // Animacion de fondo
    private float animTime = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (showMenu)
        {
            // Pausar el juego hasta que el jugador presione Jugar
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Informar al GameManager que aun no se inicio
            if (GameManager.instance != null)
                GameManager.instance.SetGameStarted(false);
        }
    }

    private void Update()
    {
        animTime += Time.unscaledDeltaTime;
    }

    public void StartGame()
    {
        showMenu = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (GameManager.instance != null)
            GameManager.instance.SetGameStarted(true);
    }

    private void InitStyles()
    {
        if (stylesInit) return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        titleStyle.normal.textColor = new Color(0.35f, 1f, 0.55f);

        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        bodyStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f);

        btnPrimary = new GUIStyle(GUI.skin.button)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };

        btnSecondary = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Normal
        };

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(28, 28, 22, 22)
        };

        pageStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter
        };
        pageStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);

        stylesInit = true;
    }

    private void OnGUI()
    {
        if (!showMenu) return;

        InitStyles();

        // Overlay oscuro de fondo (semitransparente)
        GUI.color = new Color(0f, 0f, 0f, 0.78f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float panelW = Mathf.Min(620f, Screen.width - 60f);
        float panelH = 500f;
        float px = (Screen.width - panelW) / 2f;
        float py = (Screen.height - panelH) / 2f;

        // Panel principal
        GUI.backgroundColor = new Color(0.07f, 0.12f, 0.17f, 0.98f);
        GUI.Box(new Rect(px, py, panelW, panelH), GUIContent.none, boxStyle);

        GUILayout.BeginArea(new Rect(px + 28, py + 18, panelW - 56, panelH - 36));

        // Titulo animado
        float pulse = 0.85f + 0.15f * Mathf.Sin(animTime * 1.8f);
        GUI.color = new Color(1f, 1f, 1f, pulse);
        GUILayout.Label("GUARDIANES DEL PARQUE", titleStyle, GUILayout.Height(40));
        GUI.color = Color.white;

        GUILayout.Label("Parque de la Vida - Valledupar, Cesar", pageStyle, GUILayout.Height(18));
        GUILayout.Space(8);

        // Paginas de tutorial / historia
        DrawTutorialPage(tutorialPage, panelW - 56);

        GUILayout.Space(10);

        // Indicador de pagina
        GUILayout.Label($"Página {tutorialPage + 1} de {TOTAL_PAGES}", pageStyle, GUILayout.Height(18));
        GUILayout.Space(6);

        // Botones de navegacion
        GUILayout.BeginHorizontal();

        GUI.enabled = tutorialPage > 0;
        if (GUILayout.Button("< Anterior", btnSecondary, GUILayout.Height(34)))
            tutorialPage--;
        GUI.enabled = true;

        if (tutorialPage < TOTAL_PAGES - 1)
        {
            if (GUILayout.Button("Siguiente >", btnSecondary, GUILayout.Height(34)))
                tutorialPage++;
        }
        else
        {
            GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
            if (GUILayout.Button("JUGAR AHORA", btnPrimary, GUILayout.Height(34)))
                StartGame();
            GUI.backgroundColor = Color.white;
        }

        GUILayout.EndHorizontal();

        // Puntaje maximo (si existe)
        int hs = PlayerPrefs.GetInt("HighScore", 0);
        if (hs > 0)
        {
            GUILayout.Space(6);
            GUIStyle hsStyle = new GUIStyle(pageStyle) { fontSize = 12 };
            hsStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUILayout.Label($"Mejor puntaje: {hs} pts", hsStyle, GUILayout.Height(16));
        }

        GUILayout.EndArea();
    }

    private void DrawTutorialPage(int page, float width)
    {
        switch (page)
        {
            case 0:
                GUILayout.Label("HISTORIA", new GUIStyle(bodyStyle) { fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.35f, 1f, 0.55f) } }, GUILayout.Height(24));
                GUILayout.Space(4);
                GUILayout.Label(
                    "El Parque de la Vida está cerrado por culpa de personas sin amor por Valledupar que lo llenaron de basura.\n\n" +
                    "Esto causó una crisis: los turistas dicen que la ciudad está muy sucia. Las autoridades cerraron el parque mientras lo limpian, pero la seguridad no deja entrar a nadie.\n\n" +
                    "Tú amas tu ciudad y tu parque, así que estás decidido a ayudar con la causa clandestinamente. ¡Cuidado con la seguridad mientras salvas el lugar!", bodyStyle, GUILayout.Height(210));
                break;

            case 1:
                GUILayout.Label("CONTROLES", new GUIStyle(bodyStyle) { fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.85f, 0.3f) } }, GUILayout.Height(24));
                GUILayout.Space(4);
                GUILayout.Label(
                    "[W / A / S / D]  -  Mover al Guasón / Guardián\n" +
                    "[ESPACIO]        -  Saltar\n" +
                    "[E]              -  Recoger residuo del suelo\n" +
                    "[E] (en caneca)  -  Depositar residuo\n" +
                    "[Q] o Rueda      -  Cambiar residuo en mano\n" +
                    "[1] [2] [3]      -  Seleccionar slot del inventario\n" +
                    "[P] o [Escape]   -  Pausar partida\n\n" +
                    "La mochila aguanta hasta 3 residuos. ¡Llévala a los Puntos Limpios!", bodyStyle, GUILayout.Height(210));
                break;

            case 2:
                GUILayout.Label("CLASIFICACIÓN DE RESIDUOS", new GUIStyle(bodyStyle) { fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.4f, 0.8f, 1f) } }, GUILayout.Height(24));
                GUILayout.Space(4);
                GUILayout.Label(
                    "[A] Caneca AMARILLA  -  Aprovechables\n" +
                    "     Botellas plásticas y botellas de vidrio azul\n\n" +
                    "[O] Caneca VERDE     -  Orgánicos\n" +
                    "     Restos de manzana y montones vegetales\n\n" +
                    "[N] Caneca NEGRA     -  No Aprovechable\n" +
                    "     Cajas de pizza engrasadas y otros residuos\n\n" +
                    "¡Cada acierto da puntos y tiempo extra para rescatar el parque!", bodyStyle, GUILayout.Height(210));
                break;
        }
    }
}