using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CarriedTrash
{
    public string name;
    public TrashCategory category;
    public int points;
    public float timeBonus;
    public string educationalFact;

    public CarriedTrash(TrashData data)
    {
        name = data.trashName;
        category = data.category;
        points = data.points;
        timeBonus = data.timeBonus;
        educationalFact = data.educationalFact;
    }
}

public class PlayerInventory : MonoBehaviour
{
    [Header("Capacidad de Carga")]
    [SerializeField] private int maxCapacity = 3;

    [Header("Residuos Actuales")]
    [SerializeField] private List<CarriedTrash> carriedItems = new List<CarriedTrash>();

    [Header("Selección Activa")]
    [SerializeField] private int selectedIndex = 0;

    public event Action OnInventoryChanged;

    public int MaxCapacity => maxCapacity;
    public int CurrentCount => carriedItems.Count;
    public bool IsFull => carriedItems.Count >= maxCapacity;
    public bool IsEmpty => carriedItems.Count == 0;
    public IReadOnlyList<CarriedTrash> Items => carriedItems;
    public int SelectedIndex => selectedIndex;

    public CarriedTrash SelectedItem
    {
        get
        {
            if (IsEmpty) return null;
            ClampSelectedIndex();
            return carriedItems[selectedIndex];
        }
    }

    private void ClampSelectedIndex()
    {
        if (carriedItems.Count == 0)
        {
            selectedIndex = 0;
        }
        else
        {
            selectedIndex = Mathf.Clamp(selectedIndex, 0, carriedItems.Count - 1);
        }
    }

    public void CycleNext()
    {
        if (carriedItems.Count <= 1) return;
        selectedIndex = (selectedIndex + 1) % carriedItems.Count;
        OnInventoryChanged?.Invoke();
    }

    public void CyclePrevious()
    {
        if (carriedItems.Count <= 1) return;
        selectedIndex = (selectedIndex - 1 + carriedItems.Count) % carriedItems.Count;
        OnInventoryChanged?.Invoke();
    }

    public void SelectSlot(int index)
    {
        if (index >= 0 && index < carriedItems.Count && index != selectedIndex)
        {
            selectedIndex = index;
            OnInventoryChanged?.Invoke();
        }
    }

    public bool TryAddTrash(TrashData trash)
    {
        if (IsFull) return false;

        carriedItems.Add(new CarriedTrash(trash));
        // Si es el primer ítem, asegurarse de que quede seleccionado
        if (carriedItems.Count == 1)
        {
            selectedIndex = 0;
        }
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Extrae el residuo actualmente seleccionado por el jugador.
    /// Permite que el jugador decida qué depositar y asuma el acierto o la penalización.
    /// </summary>
    public CarriedTrash ExtractSelectedItem()
    {
        if (IsEmpty) return null;

        ClampSelectedIndex();
        CarriedTrash item = carriedItems[selectedIndex];
        carriedItems.RemoveAt(selectedIndex);
        ClampSelectedIndex();
        OnInventoryChanged?.Invoke();
        return item;
    }

    /// <summary>
    /// Extrae preferentemente un residuo que coincida con la categoría solicitada (retrocompatibilidad).
    /// </summary>
    public CarriedTrash ExtractItemForBin(TrashCategory targetCategory)
    {
        if (IsEmpty) return null;

        // Si el seleccionado coincide, extraer ese
        if (SelectedItem != null && SelectedItem.category == targetCategory)
        {
            return ExtractSelectedItem();
        }

        int matchIndex = carriedItems.FindIndex(i => i.category == targetCategory);
        int indexToExtract = (matchIndex != -1) ? matchIndex : selectedIndex;

        if (indexToExtract >= carriedItems.Count) indexToExtract = 0;

        CarriedTrash item = carriedItems[indexToExtract];
        carriedItems.RemoveAt(indexToExtract);
        ClampSelectedIndex();
        OnInventoryChanged?.Invoke();
        return item;
    }

    public void ClearInventory()
    {
        carriedItems.Clear();
        selectedIndex = 0;
        OnInventoryChanged?.Invoke();
    }
}
