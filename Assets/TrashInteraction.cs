//using UnityEngine;

//public class TrashInteraction : MonoBehaviour
//{
//    public bool isNearPlayer = false; // Para saber si el personaje está cerca de la basura
//    public GameObject player; // Referencia al jugador
//    private PlayerInteraction playerInteraction; // Referencia al script del jugador

//    void Start()
//    {
//        // Obtener referencia al script del jugador si no se ha asignado
//        if (playerInteraction == null)
//        {
//            playerInteraction = player.GetComponent<PlayerInteraction>(); // Asigna el componente PlayerInteraction
//        }
//    }

//    // Este método se llama cuando el jugador entra en el trigger
//    void OnTriggerEnter(Collider other)
//    {
//        if (other.CompareTag("Player")) // Asegúrate de que tu personaje tenga el tag "Player"
//        {
//            isNearPlayer = true; // El jugador está cerca de la basura
//        }
//    }

//    // Este método se llama cuando el jugador sale del trigger
//    void OnTriggerExit(Collider other)
//    {
//        if (other.CompareTag("Player"))
//        {
//            isNearPlayer = false; // El jugador se aleja de la basura
//        }
//    }

//    // Llamado por el jugador para seleccionar la basura
//    public void Interact()
//    {
//        if (isNearPlayer && playerInteraction != null) // Solo si está cerca y el jugador tiene el script
//        {
//            playerInteraction.PickupTrash(gameObject); // Llama a PickupTrash en PlayerInteraction
//        }
//    }
//}
