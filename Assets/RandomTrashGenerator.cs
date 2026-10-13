using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomTrashGenerator : MonoBehaviour
{
    public static RandomTrashGenerator Instance { get; private set; }

    [Header("Configuración de Prefabs")]
    public GameObject[] trashPrefabs;
    public Terrain terrain;

    [Header("Cantidades y Límites")]
    public int targetTrashCount = 18;
    public float yOffset = 0.25f;
    public Vector3 scaleMultiplier = new Vector3(2f, 2f, 2f);

    [Header("Límites de Generación del Parque")]
    public float minX = 25f;
    public float maxX = 180f;
    public float minZ = 25f;
    public float maxZ = 350f;

    [Header("Respawn Dinámico")]
    public bool enableDynamicRespawn = true;
    public float respawnCheckInterval = 5f;

    [Header("Efectos Visuales en Residuos")]
    public bool addFloatingAnimation = true;
    public bool addCategoryIndicator = true;

    // Lista de residuos activos actualmente en el mundo
    private List<GameObject> activeTrashList = new List<GameObject>();
    private float nextRespawnTime = 0f;

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
        if (terrain == null)
        {
            terrain = Terrain.activeTerrain;
        }

        SpawnInitialTrash();
    }

    private void Update()
    {
        if (enableDynamicRespawn && Time.time >= nextRespawnTime)
        {
            nextRespawnTime = Time.time + respawnCheckInterval;
            CheckAndReplenishTrash();
        }
    }

    /// <summary>
    /// Limpia residuos destruidos de la lista y genera nuevos si el parque tiene menos del objetivo
    /// </summary>
    private void CheckAndReplenishTrash()
    {
        activeTrashList.RemoveAll(t => t == null);

        int missing = targetTrashCount - activeTrashList.Count;
        if (missing > 0)
        {
            for (int i = 0; i < missing; i++)
            {
                SpawnSingleTrash();
            }
        }
    }

    private void SpawnInitialTrash()
    {
        if (terrain == null || trashPrefabs == null || trashPrefabs.Length == 0)
        {
            Debug.LogWarning("[RandomTrashGenerator] Terreno o lista de prefabs vacíos.");
            return;
        }

        // Limpiar huérfanos previos si los hubiera
        activeTrashList.RemoveAll(t => t == null);

        for (int i = 0; i < targetTrashCount; i++)
        {
            SpawnSingleTrash();
        }
    }

    private bool SpawnSingleTrash()
    {
        if (trashPrefabs == null || trashPrefabs.Length == 0) return false;

        GameObject prefab = trashPrefabs[Random.Range(0, trashPrefabs.Length)];
        if (prefab == null) return false;

        // Intentar encontrar una posición válida sobre el terreno (sin colisionar con edificios ni árboles)
        Vector3 spawnPos;
        if (!TryGetValidGroundPosition(out spawnPos))
        {
            return false;
        }

        GameObject trashObj = Instantiate(prefab, spawnPos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
        trashObj.transform.localScale = Vector3.Scale(trashObj.transform.localScale, scaleMultiplier);
        trashObj.tag = "Trash";

        // Asegurar Collider Trigger
        Collider col = trashObj.GetComponent<Collider>();
        if (col == null)
        {
            col = trashObj.AddComponent<BoxCollider>();
        }
        col.isTrigger = true;

        // Asegurar TrashData
        TrashData data = trashObj.GetComponent<TrashData>();
        if (data == null)
        {
            data = trashObj.AddComponent<TrashData>();
            ConfigureTrashDataByCategory(data, trashObj.name);
        }

        // Añadir animación sutil de flotación y rotación
        if (addFloatingAnimation && trashObj.GetComponent<FloatingTrash>() == null)
        {
            FloatingTrash floater = trashObj.AddComponent<FloatingTrash>();
            floater.rotationSpeed = 35f;
            floater.floatSpeed = 2f;
            floater.floatHeight = 0.15f;
        }

        // Añadir indicador visual de color según categoría
        if (addCategoryIndicator)
        {
            CreateTrashGlowMarker(trashObj, data);
        }

        activeTrashList.Add(trashObj);
        return true;
    }

    private bool TryGetValidGroundPosition(out Vector3 groundPos)
    {
        groundPos = Vector3.zero;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            float rx = Random.Range(minX, maxX);
            float rz = Random.Range(minZ, maxZ);

            // Raycast desde arriba hacia abajo para asegurar que caiga sobre el terreno y no sobre un techo
            RaycastHit hit;
            Vector3 rayStart = new Vector3(rx, 150f, rz);
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 300f))
            {
                // Si golpea el Terreno directamente o una zona transitable
                if (hit.collider is TerrainCollider || hit.collider.CompareTag("Untagged"))
                {
                    groundPos = hit.point + Vector3.up * yOffset;
                    return true;
                }
            }
            else if (terrain != null)
            {
                float y = terrain.SampleHeight(new Vector3(rx, 0, rz));
                groundPos = new Vector3(rx, y + yOffset, rz);
                return true;
            }
        }

        return false;
    }

    private void ConfigureTrashDataByCategory(TrashData data, string objName)
    {
        string lower = objName.ToLower();

        // --- Plasticos ---
        if (lower.Contains("bag") || lower.Contains("bolsa"))
        {
            data.category = TrashCategory.Plastico;
            data.trashName = "Bolsa de Plastico";
            data.points = 10;
            data.timeBonus = 4f;
            data.educationalFact = "Las bolsas plasticas tardan hasta 500 anos en degradarse. En el Parque de la Vida bloquean el drenaje y afectan la fauna.";
        }
        else if (lower.Contains("watter") || lower.Contains("water") || lower.Contains("plastic") || lower.Contains("bottle"))
        {
            data.category = TrashCategory.Plastico;
            data.trashName = "Botella Plastica";
            data.points = 12;
            data.timeBonus = 4f;
            data.educationalFact = "Colombia genera mas de 2 millones de toneladas de plastico al ano. Reciclar una botella ahorra energia suficiente para encender un bombillo por 6 horas.";
        }
        else if (lower.Contains("straw") || lower.Contains("paja") || lower.Contains("pitillo"))
        {
            data.category = TrashCategory.Plastico;
            data.trashName = "Pitillo Plastico";
            data.points = 8;
            data.timeBonus = 3f;
            data.educationalFact = "Los pitillos son uno de los residuos que mas dano causan a aves y peces. Valledupar impulsa el uso de pitillos de bambu o papel.";
        }
        // --- Vidrio ---
        else if (lower.Contains("soda") || lower.Contains("vidrio") || lower.Contains("glass") || lower.Contains("jar"))
        {
            data.category = TrashCategory.Vidrio;
            data.trashName = "Botella de Vidrio";
            data.points = 15;
            data.timeBonus = 5f;
            data.educationalFact = "El vidrio es 100% reciclable infinitas veces sin perder calidad. Un frasco de vidrio reciclado ahorra suficiente energia para calentar agua por 30 minutos.";
        }
        // --- Organicos ---
        else if (lower.Contains("pile") || lower.Contains("food") || lower.Contains("organ") || lower.Contains("cascara") || lower.Contains("fruit"))
        {
            string[] organicItems = { "Cascara de Fruta", "Residuo de Comida", "Desecho Organico", "Hoja Seca" };
            string[] organicFacts = {
                "Los residuos organicos se convierten en abono (compost) que nutre la vegetacion del Parque de la Vida.",
                "El 60% de los residuos domesticos en Valledupar son organicos. Separarlos reduce drasticamente los vertederos.",
                "El compost organico mejora hasta un 30% la capacidad del suelo para retener agua, vital en el calido clima de Valledupar.",
                "Las hojas y ramas compostadas alimentan los suelos de los 160 especies de arboles del Parque de la Vida."
            };
            int idx = Random.Range(0, organicItems.Length);
            data.category = TrashCategory.Organico;
            data.trashName = organicItems[idx];
            data.points = 8;
            data.timeBonus = 3f;
            data.educationalFact = organicFacts[idx];
        }
        // --- No Aprovechable (pilas, medicamentos, colillas, latas) ---
        else if (lower.Contains("battery") || lower.Contains("pila") || lower.Contains("bateria"))
        {
            data.category = TrashCategory.NoAprovechable;
            data.trashName = "Pila Usada";
            data.points = 20;
            data.timeBonus = 7f;
            data.educationalFact = "Una sola pila contamina hasta 600 litros de agua subterranea. En Valledupar existen puntos de acopio especiales para pilas y baterias.";
        }
        else if (lower.Contains("medicine") || lower.Contains("medic") || lower.Contains("pill"))
        {
            data.category = TrashCategory.NoAprovechable;
            data.trashName = "Medicamento Vencido";
            data.points = 20;
            data.timeBonus = 7f;
            data.educationalFact = "Los medicamentos vencidos no deben tirarse a la basura normal. Las farmacias de Valledupar tienen puntos de recoleccion RESPEL.";
        }
        else if (lower.Contains("can") || lower.Contains("lata") || lower.Contains("metal"))
        {
            data.category = TrashCategory.NoAprovechable;
            data.trashName = "Lata de Aluminio";
            data.points = 15;
            data.timeBonus = 5f;
            data.educationalFact = "El aluminio puede reciclarse indefinidamente. Reciclar una lata ahorra 95% de la energia que se usaria para fabricar una nueva.";
        }
        else if (lower.Contains("cigarette") || lower.Contains("colilla") || lower.Contains("cigarro"))
        {
            data.category = TrashCategory.NoAprovechable;
            data.trashName = "Colilla de Cigarrillo";
            data.points = 12;
            data.timeBonus = 4f;
            data.educationalFact = "Las colillas son el residuo mas recogido en playas y parques. Contienen hasta 7.000 sustancias toxicas que contaminan el suelo y el agua.";
        }
        // --- Default ---
        else
        {
            // Seleccionar aleatoriamente entre varios tipos genericos
            int pick = Random.Range(0, 4);
            switch (pick)
            {
                case 0:
                    data.category = TrashCategory.Plastico;
                    data.trashName = "Empaque Plastico";
                    data.points = 10; data.timeBonus = 4f;
                    data.educationalFact = "Clasifica los plasticos en la Caneca Azul. El Parque de la Vida merece estar libre de residuos.";
                    break;
                case 1:
                    data.category = TrashCategory.Organico;
                    data.trashName = "Residuo Organico";
                    data.points = 8; data.timeBonus = 3f;
                    data.educationalFact = "Los organicos en la caneca verde se convierten en abono para los jardines del parque.";
                    break;
                case 2:
                    data.category = TrashCategory.Vidrio;
                    data.trashName = "Fragmento de Vidrio";
                    data.points = 12; data.timeBonus = 4f;
                    data.educationalFact = "El vidrio roto en el parque es peligroso para personas, ninos y animales. Separa y recicla.";
                    break;
                default:
                    data.category = TrashCategory.NoAprovechable;
                    data.trashName = "Residuo Mezclado";
                    data.points = 10; data.timeBonus = 4f;
                    data.educationalFact = "Los residuos no aprovechables deben ir a la caneca gris. No mezcles con organicos ni plasticos.";
                    break;
            }
        }
    }

    /// <summary>
    /// Crea un marcador visual sobre el residuo:
    /// - Columna de particulas de color flotante (muy visible en el pasto)
    /// - TextMesh con inicial de categoria como respaldo
    /// </summary>
    private void CreateTrashGlowMarker(GameObject trash, TrashData data)
    {
        Color catColor = data.GetCategoryColor();

        // 1. Sistema de particulas de columna luminosa
        GameObject psObj = new GameObject("TrashGlowParticles");
        psObj.transform.SetParent(trash.transform);
        psObj.transform.localPosition = new Vector3(0, 0.3f, 0);

        ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(catColor.r, catColor.g, catColor.b, 0.9f),
            new Color(1f, 1f, 1f, 0.6f));
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.08f;

        var emission = ps.emission;
        emission.rateOverTime = 12;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.08f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(catColor.r, catColor.g, catColor.b), 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        ps.Play();

        // 2. Etiqueta de categoria (inicial) siempre orientada a la camara
        GameObject markerObj = new GameObject("CategoryMarker");
        markerObj.transform.SetParent(trash.transform);
        markerObj.transform.localPosition = new Vector3(0, 0.55f, 0);

        TextMesh tm = markerObj.AddComponent<TextMesh>();
        tm.text = GetCategorySymbol(data.category);
        tm.fontSize = 22;
        tm.characterSize = 0.05f;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = catColor;

        markerObj.AddComponent<BillboardLookAtCamera>();
    }

    private string GetCategorySymbol(TrashCategory cat)
    {
        switch (cat)
        {
            case TrashCategory.Plastico: return "[P]";
            case TrashCategory.Organico: return "[O]";
            case TrashCategory.Vidrio: return "[V]";
            case TrashCategory.NoAprovechable: return "[N]";
            default: return "[?]";
        }
    }
}
