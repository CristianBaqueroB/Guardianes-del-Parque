using UnityEngine;

public enum TrashCategory
{
    Plastico,
    Organico,
    Vidrio,
    NoAprovechable
}

public class TrashData : MonoBehaviour
{
    [Header("Identificación")]
    public string trashName = "Residuo";
    public TrashCategory category = TrashCategory.Plastico;

    // Campo heredado para retrocompatibilidad
    [HideInInspector]
    public string trashType = "";

    [Header("Recompensas")]
    public int points = 10;
    public float timeBonus = 5f;

    [Header("Dato Pedagógico (Valledupar Limpia)")]
    [TextArea(2, 4)]
    public string educationalFact = "El plástico tarda cientos de años en degradarse. ¡Sepáralo siempre!";

    private void Awake()
    {
        // Migración automática si venía configurado como string en escenas anteriores
        if (!string.IsNullOrEmpty(trashType) && category == TrashCategory.Plastico)
        {
            string lower = trashType.ToLower();
            if (lower.Contains("organ") || lower.Contains("comida") || lower.Contains("cascara"))
            {
                category = TrashCategory.Organico;
                if (trashName == "Residuo") trashName = "Desecho Orgánico";
            }
            else if (lower.Contains("vidrio") || lower.Contains("cristal"))
            {
                category = TrashCategory.Vidrio;
                if (trashName == "Residuo") trashName = "Botella de Vidrio";
            }
            else if (lower.Contains("bolsa") || lower.Contains("botella") || lower.Contains("plast"))
            {
                category = TrashCategory.Plastico;
                if (trashName == "Residuo") trashName = trashType;
            }
        }
        else if (trashName == "Residuo")
        {
            // Asigna un nombre amigable por defecto según el gameObject
            string goName = gameObject.name.ToLower();
            if (goName.Contains("bottle") || goName.Contains("botella"))
            {
                if (goName.Contains("soda") || goName.Contains("vidrio"))
                {
                    category = TrashCategory.Vidrio;
                    trashName = "Botella de Vidrio";
                }
                else
                {
                    category = TrashCategory.Plastico;
                    trashName = "Botella Plástica";
                }
            }
            else if (goName.Contains("bag") || goName.Contains("bolsa"))
            {
                category = TrashCategory.Plastico;
                trashName = "Bolsa de Plástico";
            }
            else if (goName.Contains("pile"))
            {
                category = TrashCategory.Organico;
                trashName = "Desechos Orgánicos";
            }
        }
    }

    public string GetCategoryDisplayName()
    {
        switch (category)
        {
            case TrashCategory.Plastico: return "Plástico";
            case TrashCategory.Organico: return "Orgánico";
            case TrashCategory.Vidrio: return "Vidrio";
            case TrashCategory.NoAprovechable: return "No Aprovechable";
            default: return "General";
        }
    }

    public Color GetCategoryColor()
    {
        switch (category)
        {
            case TrashCategory.Plastico: return new Color(0.2f, 0.6f, 1f);     // Azul brillante
            case TrashCategory.Organico: return new Color(0.2f, 0.8f, 0.2f);    // Verde
            case TrashCategory.Vidrio: return new Color(0.9f, 0.9f, 0.3f);      // Amarillo
            case TrashCategory.NoAprovechable: return new Color(0.4f, 0.4f, 0.4f); // Gris oscuro
            default: return Color.white;
        }
    }
}