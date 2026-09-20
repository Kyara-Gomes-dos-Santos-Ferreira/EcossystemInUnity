using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainPainter : MonoBehaviour
{
    private Terrain terrain;

    [System.Serializable]
    public struct TerrainLayerConfig
    {
        public TerrainLayer terrainLayer;
        [Range(0f, 1f)] public float minHeight;
    }

    [Header("Camadas de Textura (Ordem da mais baixa para a mais alta)")]
    public TerrainLayerConfig[] textureLayers;

    [ContextMenu("Pintar Terreno")]
    public void PaintTerrain()
    {
        if (terrain == null) terrain = GetComponent<Terrain>();
        TerrainData terrainData = terrain.terrainData;

        if (textureLayers == null || textureLayers.Length == 0) return;

        TerrainLayer[] layers = new TerrainLayer[textureLayers.Length];
        for (int i = 0; i < textureLayers.Length; i++)
        {
            layers[i] = textureLayers[i].terrainLayer;
        }
        terrainData.terrainLayers = layers;

        int alphamapWidth = terrainData.alphamapWidth;
        int alphamapHeight = terrainData.alphamapHeight;
        int numLayers = textureLayers.Length;

        float[,,] splatmapData = new float[alphamapHeight, alphamapWidth, numLayers];

        for (int z = 0; z < alphamapHeight; z++)
        {
            for (int x = 0; x < alphamapWidth; x++)
            {
                float normalizedX = (float)x / (alphamapWidth - 1);
                float normalizedZ = (float)z / (alphamapHeight - 1);

                float height = terrainData.GetHeight(
                    Mathf.RoundToInt(normalizedX * terrainData.size.x),
                    Mathf.RoundToInt(normalizedZ * terrainData.size.z)
                );
                float normalizedHeight = height / terrainData.size.y;

                // Encontra a camada ativa com base nas alturas que você definiu no Inspector
                int activeLayer = 0;
                for (int i = 0; i < numLayers; i++)
                {
                    if (normalizedHeight >= textureLayers[i].minHeight)
                    {
                        activeLayer = i;
                    }
                }

                // Aplicação direta de peso total na camada ativa para evitar o efeito "lavado"
                for (int i = 0; i < numLayers; i++)
                {
                    splatmapData[z, x, i] = (i == activeLayer) ? 0.75f : 0f;
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, splatmapData);
        Debug.Log("Terreno pintado com texturas sólidas!");
    }
}