using UnityEngine;

public class RandomTrashGenerator : MonoBehaviour
{
    public GameObject[] trashPrefabs;
    public Terrain terrain;
    public int trashCount = 10;
    public float yOffset = 0.17849f;
    public Vector3 scaleMultiplier = new Vector3(2f, 2f, 2f);

    void Start()
    {
        GenerateTrash();
    }

    void GenerateTrash()
    {
        Debug.Log("Generando basura...");

        // Obtiene el tamaño del terreno
        float terrainWidth = terrain.terrainData.size.x;
        float terrainLength = terrain.terrainData.size.z;

        // Genera la cantidad de basura especificada
        for (int i = 0; i < trashCount; i++)
        {
            // Selecciona un prefab aleatorio de la lista
            GameObject trashPrefab = trashPrefabs[Random.Range(0, trashPrefabs.Length)];

            // Genera una posición aleatoria en el terreno
            float randomX = Random.Range(0, terrainWidth);
            float randomZ = Random.Range(0, terrainLength);

            // Obtén la altura del terreno en esa posición
            float terrainHeight = terrain.SampleHeight(new Vector3(randomX, 0, randomZ));

            // Ajusta la posición para incluir el offset
            Vector3 trashPosition = new Vector3(randomX, terrainHeight + yOffset, randomZ);

            // Instancia la basura
            GameObject trash = Instantiate(trashPrefab, trashPosition, Quaternion.identity);

            // Aplica el tamaño definido por el multiplicador de escala
            trash.transform.localScale = Vector3.Scale(trash.transform.localScale, scaleMultiplier);

            // Asegúrate de que tiene un collider de tipo trigger
            Collider trashCollider = trash.GetComponent<Collider>();

            if (trashCollider == null)
            {
                trashCollider = trash.AddComponent<BoxCollider>();
            }

            trashCollider.isTrigger = true;

            // Asigna el tag "Trash"
            trash.tag = "Trash";
        }
    }
}
