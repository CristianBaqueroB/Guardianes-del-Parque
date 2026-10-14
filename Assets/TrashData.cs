using UnityEngine;

public enum TrashCategory
{
    Aprovechables,
    Organico,
    NoAprovechable
}

public class TrashData : MonoBehaviour
{
    [Header("Identificación")]
    public string trashName = "Residuo";
    public TrashCategory category = TrashCategory.Aprovechables;

    // Campo heredado para retrocompatibilidad
    [HideInInspector]
    public string trashType = "";

    [Header("Recompensas")]
    public int points = 10;
    public float timeBonus = 5f;

    [Header("Dato Pedagógico (Valledupar Limpia)")]
    [TextArea(2, 4)]
    public string educationalFact = "Separar los residuos correctamente ayuda a conservar el Parque de la Vida.";

    private void Awake()
    {
        // Migración automática si venía configurado como string en escenas anteriores
        if (!string.IsNullOrEmpty(trashType))
        {
            string lower = trashType.ToLower();
            if (lower.Contains("organ") || lower.Contains("comida") || lower.Contains("cascara"))
            {
                category = TrashCategory.Organico;
                if (trashName == "Residuo") trashName = "Desecho Orgánico";
            }
            else if (lower.Contains("pila") || lower.Contains("bateria") || lower.Contains("medicin") || lower.Contains("colilla"))
            {
                category = TrashCategory.NoAprovechable;
                if (trashName == "Residuo") trashName = "Residuo No Aprovechable";
            }
            else
            {
                category = TrashCategory.Aprovechables;
                if (trashName == "Residuo") trashName = trashType;
            }
        }
        else if (trashName == "Residuo")
        {
            // Asigna un nombre y categoría por defecto según el gameObject
            string goName = gameObject.name.ToLower();
            if (goName.Contains("bottle") || goName.Contains("botella") || goName.Contains("bag") || goName.Contains("bolsa") || goName.Contains("soda") || goName.Contains("vidrio") || goName.Contains("can") || goName.Contains("lata"))
            {
                category = TrashCategory.Aprovechables;
                if (goName.Contains("vidrio") || goName.Contains("soda")) trashName = "Botella de Vidrio";
                else if (goName.Contains("bag") || goName.Contains("bolsa")) trashName = "Bolsa de Plástico";
                else if (goName.Contains("can") || goName.Contains("lata")) trashName = "Lata de Aluminio";
                else trashName = "Botella Plástica";
            }
            else if (goName.Contains("pile") || goName.Contains("food") || goName.Contains("organ"))
            {
                category = TrashCategory.Organico;
                trashName = "Desechos Orgánicos";
            }
            else if (goName.Contains("battery") || goName.Contains("pila") || goName.Contains("cigarette") || goName.Contains("colilla"))
            {
                category = TrashCategory.NoAprovechable;
                trashName = "Residuo No Aprovechable";
            }
        }
    }

    public string GetCategoryDisplayName()
    {
        switch (category)
        {
            case TrashCategory.Aprovechables: return "Aprovechables";
            case TrashCategory.Organico: return "Orgánico";
            case TrashCategory.NoAprovechable: return "No Aprovechable";
            default: return "General";
        }
    }

    public Color GetCategoryColor()
    {
        switch (category)
        {
            case TrashCategory.Aprovechables: return new Color(0.2f, 0.6f, 1f);  // Azul (Aprovechables)
            case TrashCategory.Organico: return new Color(0.2f, 0.8f, 0.2f);     // Verde (Orgánico)
            case TrashCategory.NoAprovechable: return new Color(0.4f, 0.4f, 0.4f); // Gris (No Aprovechable)
            default: return Color.white;
        }
    }
}