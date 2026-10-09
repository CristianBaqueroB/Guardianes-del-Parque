using UnityEngine;

public class GameMenuManager : MonoBehaviour
{
    public GameObject menuPanel; // El panel del menú inicial
    public GameObject player; // El objeto del jugador

    void Start()
    {
        // Desactiva al jugador inicialmente
        player.SetActive(false);

        // Asegúrate de que el menú inicial esté activo
        menuPanel.SetActive(true);
    }

    public void StartGame()
    {
        // Activa al jugador
        player.SetActive(true);

        // Oculta el menú inicial
        menuPanel.SetActive(false);
    }
}
