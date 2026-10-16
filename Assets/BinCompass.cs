using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mini-brújula de canecas: muestra flechas en pantalla indicando la dirección y distancia
/// a cada Punto Limpio (RecycleBin) cuando el jugador tiene la mochila llena o necesita depositar.
/// Se dibuja vía OnGUI para máxima compatibilidad sin depender de Canvas.
/// </summary>
public class BinCompass : MonoBehaviour
{
    public static BinCompass Instance { get; private set; }

    [Header("Configuración de Brújula")]
    [Tooltip("Siempre mostrar la brújula (si false, solo cuando mochila tiene ítems)")]
    public bool alwaysShow = false;

    [Tooltip("Radio del círculo de la brújula en pantalla (px)")]
    public float compassRadius = 72f;

    [Tooltip("Posición X del centro de la brújula (desde derecha)")]
    public float rightMargin = 120f;

    [Tooltip("Posición Y del centro de la brújula (desde arriba)")]
    public float topMargin = 200f;

    // Estado interno
    private List<RecycleBin> bins = new List<RecycleBin>();
    private PlayerInventory playerInventory;
    private Camera mainCam;

    // Estilos OnGUI
    private GUIStyle labelStyle;
    private GUIStyle distanceStyle;
    private bool stylesInit = false;

    // Texturas procedurales
    private static Texture2D circleTexBg;
    private static Texture2D arrowTex;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        mainCam = Camera.main;
        RefreshBinList();

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInventory = player.GetComponent<PlayerInventory>();
            if (playerInventory == null)
                playerInventory = player.GetComponentInChildren<PlayerInventory>();
        }

        EnsureTextures();
    }

    private void RefreshBinList()
    {
        bins.Clear();
        var found = FindObjectsByType<RecycleBin>(FindObjectsInactive.Exclude);
        foreach (var b in found) bins.Add(b);
    }

    private static void EnsureTextures()
    {
        if (circleTexBg == null)
        {
            int size = 128;
            circleTexBg = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float outerR = center - 1;
            float innerR = outerR - 5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center, dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= outerR && dist >= innerR)
                        circleTexBg.SetPixel(x, y, new Color(1f, 1f, 1f, 0.55f));
                    else if (dist < innerR)
                        circleTexBg.SetPixel(x, y, new Color(0.06f, 0.10f, 0.16f, 0.82f));
                    else
                        circleTexBg.SetPixel(x, y, Color.clear);
                }
            circleTexBg.Apply();
        }

        if (arrowTex == null)
        {
            int size = 32;
            arrowTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - size / 2f) / (size / 2f);
                    float ny = (y - size / 2f) / (size / 2f);
                    float halfWidth = 0.55f * (1f - ny) * 0.5f;
                    bool inside = (Mathf.Abs(nx) <= halfWidth) && ny >= -0.85f && ny <= 0.9f;
                    arrowTex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            arrowTex.Apply();
        }
    }

    private void InitStyles()
    {
        if (stylesInit) return;

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter,
            wordWrap = false
        };

        distanceStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.UpperCenter
        };
        distanceStyle.normal.textColor = new Color(1f, 0.95f, 0.6f);

        stylesInit = true;
    }

    private void OnGUI()
    {
        if (GameManager.instance != null &&
            (GameManager.instance.isGameOver || GameManager.instance.isVictory || GameManager.instance.isPaused))
            return;

        bool show = alwaysShow;
        if (!show && playerInventory != null)
            show = !playerInventory.IsEmpty;

        if (!show || bins.Count == 0) return;

        InitStyles();
        EnsureTextures();

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        float cx = Screen.width - rightMargin;
        float cy = topMargin;

        float drawSize = compassRadius * 2f;
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(cx - compassRadius, cy - compassRadius, drawSize, drawSize), circleTexBg, ScaleMode.StretchToFill, true);

        GUI.color = new Color(1f, 1f, 1f, 0.7f);
        GUI.Label(new Rect(cx - 32, cy - 8, 64, 16), "CANECAS", labelStyle);
        GUI.color = Color.white;

        Vector3 playerPos = transform.position;
        Vector3 playerForward = transform.forward;
        playerForward.y = 0;
        playerForward.Normalize();

        for (int i = 0; i < bins.Count; i++)
        {
            RecycleBin bin = bins[i];
            if (bin == null) continue;

            Vector3 tobin = bin.transform.position - playerPos;
            tobin.y = 0;
            float dist = tobin.magnitude;
            if (dist < 0.5f) continue;

            float angle = Vector3.SignedAngle(playerForward, tobin.normalized, Vector3.up);
            float rad = angle * Mathf.Deg2Rad;

            float arrowRadius = compassRadius * 0.72f;
            float ax = cx + Mathf.Sin(rad) * arrowRadius;
            float ay = cy - Mathf.Cos(rad) * arrowRadius;

            Color binColor = bin.binColor;
            binColor.a = 0.95f;
            GUI.color = binColor;

            float arrowSize = 18f;
            Rect arrowRect = new Rect(ax - arrowSize / 2f, ay - arrowSize / 2f, arrowSize, arrowSize);

            Matrix4x4 matrixBackup = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, new Vector2(ax, ay));
            GUI.DrawTexture(arrowRect, arrowTex, ScaleMode.StretchToFill, true);
            GUI.matrix = matrixBackup;

            GUI.color = Color.white;
            string distLabel = dist < 10f ? "<10m" : $"{Mathf.RoundToInt(dist)}m";
            string catInitial = GetCategoryInitial(bin.acceptedCategory);
            GUI.color = binColor;
            GUI.Label(new Rect(ax - 24, ay + 10, 48, 14), $"{catInitial} {distLabel}", distanceStyle);
            GUI.color = Color.white;
        }

        if (playerInventory != null && playerInventory.IsFull)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * 4f);
            GUI.color = new Color(1f, 0.55f + pulse * 0.35f, 0.1f, 0.9f);
            GUI.Label(new Rect(cx - 50, cy + compassRadius + 6f, 100, 18), "MOCHILA LLENA!", labelStyle);
            GUI.color = Color.white;
        }
    }

    private string GetCategoryInitial(TrashCategory cat)
    {
        switch (cat)
        {
            case TrashCategory.Aprovechables: return "Aprov";
            case TrashCategory.Organico: return "Org";
            case TrashCategory.NoAprovechable: return "N/A";
            default: return "?";
        }
    }
}