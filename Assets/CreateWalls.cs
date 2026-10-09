using UnityEngine;

public class CreateWalls : MonoBehaviour
{
    public GameObject wallPrefab; // Prefab del muro
    public Terrain terrain; // Referencia al terreno
    public float wallHeight = 5f;
    public float wallWidth = 2f;

    void Start()
    {
        CreateBorderWalls();
    }

    void CreateBorderWalls()
    {
        // Obtén el tamaño del terreno
        float terrainWidth = terrain.terrainData.size.x;
        float terrainLength = terrain.terrainData.size.z;

        // Crear muros en cada lado
        CreateWall(new Vector3(terrainWidth / 2, wallHeight / 2, 0), terrainWidth, true); // Lado frontal
        CreateWall(new Vector3(terrainWidth / 2, wallHeight / 2, terrainLength), terrainWidth, true); // Lado trasero
        CreateWall(new Vector3(0, wallHeight / 2, terrainLength / 2), terrainLength, false); // Lado izquierdo
        CreateWall(new Vector3(terrainWidth, wallHeight / 2, terrainLength / 2), terrainLength, false); // Lado derecho
    }

    void CreateWall(Vector3 position, float length, bool horizontal)
    {
        GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity);
        wall.transform.localScale = horizontal
            ? new Vector3(length, wallHeight, wallWidth)
            : new Vector3(wallWidth, wallHeight, length);
    }
}
