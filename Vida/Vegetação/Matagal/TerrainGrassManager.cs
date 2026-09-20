using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainGrassManager : MonoBehaviour
{
    private Terrain terrain;
    private TerrainData terrainData;

    [Header("Configurações de Geração")]
    [Tooltip("Índice da camada de capim 2D no Terrain (0 para a primeira textura adicionada).")]
    public int detailLayerIndex = 0;

    [Header("Regras de Bioma e Altura")]
    public float minHeight = 1f;
    public float maxTerrainHeight = 12f;

    [Header("Distribuição e Densidade")]
    [Tooltip("Frequência do ruído. Valores maiores criam mais manchas de grama.")]
    public float noiseScale = 0.15f;
    [Tooltip("Limite mínimo do ruído para nascer grama (quanto menor, mais espalhado).")]
    public float noiseThreshold = 0.15f;
    [Tooltip("Quantidade mínima e máxima de tufos por célula.")]
    public int minDensity = 4;
    public int maxDensity = 12;

    void Start()
    {
        terrain = GetComponent<Terrain>();
        terrainData = terrain.terrainData;

        GenerateProceduralGrass();
    }

    [ContextMenu("Gerar Capim Proceduralmente")]
    public void GenerateProceduralGrass()
    {
        if (terrainData == null) terrainData = terrain.terrainData;

        int detailWidth = terrainData.detailWidth;
        int detailHeight = terrainData.detailHeight;

        // Cria a matriz cobrindo todo o mapa de detalhes do terrain
        int[,] detailMap = new int[detailHeight, detailWidth];

        for (int z = 0; z < detailHeight; z++)
        {
            for (int x = 0; x < detailWidth; x++)
            {
                float normX = (float)x / detailWidth;
                float normZ = (float)z / detailHeight;

                float worldX = normX * terrainData.size.x;
                float worldZ = normZ * terrainData.size.z;

                float worldY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + terrain.transform.position.y;

                if (worldY >= minHeight && worldY <= maxTerrainHeight)
                {
                    // Ruído ajustado com a escala configurável
                    float noise = Mathf.PerlinNoise(worldX * noiseScale, worldZ * noiseScale);

                    if (noise > noiseThreshold)
                    {
                        // Densidade maior e controlável para o capim cobrir o terreno
                        detailMap[z, x] = Random.Range(minDensity, maxDensity + 1);
                    }
                    else
                    {
                        detailMap[z, x] = 0;
                    }
                }
                else
                {
                    detailMap[z, x] = 0;
                }
            }
        }

        // IMPORTANTE para textura 2D: Passamos a origem (0,0) e o tamanho total (detailWidth, detailHeight)
        terrainData.SetDetailLayer(0, 0, detailLayerIndex, detailMap);

        // Sincroniza o terrain para forçar o desenho da textura 2D na tela
        terrain.Flush();

        Debug.Log("Campos de capim 2D gerados e aplicados com sucesso!");
    }

    // --- MÉTODOS PARA OS HERBÍVOROS ---
    public int CheckGrassAtPosition(Vector3 worldPosition)
    {
        int detailX = Mathf.FloorToInt((worldPosition.x / terrainData.size.x) * terrainData.detailWidth);
        int detailZ = Mathf.FloorToInt((worldPosition.z / terrainData.size.z) * terrainData.detailHeight);

        if (detailX >= 0 && detailX < terrainData.detailWidth && detailZ >= 0 && detailZ < terrainData.detailHeight)
        {
            int[,] patch = terrainData.GetDetailLayer(detailX, detailZ, 1, 1, detailLayerIndex);
            return patch[0, 0];
        }
        return 0;
    }

    public void EatGrassAtPosition(Vector3 worldPosition, int amountToRemove)
    {
        int detailX = Mathf.FloorToInt((worldPosition.x / terrainData.size.x) * terrainData.detailWidth);
        int detailZ = Mathf.FloorToInt((worldPosition.z / terrainData.size.z) * terrainData.detailHeight);

        if (detailX >= 0 && detailX < terrainData.detailWidth && detailZ >= 0 && detailZ < terrainData.detailHeight)
        {
            int[,] patch = terrainData.GetDetailLayer(detailX, detailZ, 1, 1, detailLayerIndex);
            patch[0, 0] = Mathf.Max(0, patch[0, 0] - amountToRemove);

            terrainData.SetDetailLayer(detailX, detailZ, detailLayerIndex, patch);
            terrain.Flush();
        }
    }
}