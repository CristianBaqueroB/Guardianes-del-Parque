using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private bool isNearTrash = false;
    private GameObject trashInRange = null;

    void Update()
    {
        // Presiona E para recoger basura
        if (isNearTrash && Input.GetKeyDown(KeyCode.E))
        {
            PickUpTrash();
        }
    }

    void PickUpTrash()
    {
        if (trashInRange != null)
        {
            GameObject trashToDestroy = trashInRange;

            // Evita múltiples llamadas
            trashInRange = null;
            isNearTrash = false;

            // Obtener datos de la basura
            TrashData trashData = trashToDestroy.GetComponent<TrashData>();

            // Si tiene TrashData, usa puntos y tiempo
            if (trashData != null)
            {
                GameManager.instance.CollectTrash(
                    trashData.points,
                    trashData.timeBonus
                );
            }
            else
            {
                // Valores por defecto
                GameManager.instance.CollectTrash(1, 1f);
            }

            // Destruir basura
            Destroy(trashToDestroy);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Trash"))
        {
            isNearTrash = true;
            trashInRange = other.gameObject;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Trash"))
        {
            isNearTrash = false;
            trashInRange = null;
        }
    }
}