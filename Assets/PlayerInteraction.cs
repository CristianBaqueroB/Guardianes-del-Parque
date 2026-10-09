using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private bool isNearTrash = false;
    private GameObject trashInRange = null;

    private Animator animator;

    void Start()
    {
        // Buscar Animator del personaje hijo
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Detectar movimiento del jugador
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Calcular velocidad
        float speed = new Vector3(horizontal, 0, vertical).magnitude;

        // Enviar velocidad al Animator
        if (animator != null)
        {
            animator.SetFloat("Speed", speed * 5f);
        }

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
