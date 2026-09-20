using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class NativeTerrainGenerator : MonoBehaviour
{
    private Terrain terrain;

    [Header("Dimensões do Mapa (Recomendado 512 para evitar quebras)")]
    public int width = 512;
    public int depth = 512;
    public float heightMultiplier = 80f;

    [Header("Ruído Base e Fractal")]
    public float baseScale = 50f;
    [Range(1, 8)] public int octaves = 4;
    [Range(0, 1)] public float persistence = 0.5f;
    [Range(1, 4)] public float lacunarity = 2f;

    [Header("Semente (Seed)")]
    public float seed = 0f;

    void Start()
    {
        GenerateTerrain();
    }

    [ContextMenu("Gerar Terreno")]
    public void GenerateTerrain()
    {
        if (terrain == null) terrain = GetComponent<Terrain>();
        TerrainData terrainData = terrain.terrainData;

        int resolution = width + 1;
        if (terrainData.heightmapResolution != resolution)
        {
            terrainData.heightmapResolution = resolution;
        }

        terrainData.size = new Vector3(width, heightMultiplier, depth);

        float[,] heights = new float[resolution, resolution];

        System.Random prng = new(Mathf.RoundToInt(seed));
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + seed;
            float offsetY = prng.Next(-100000, 100000) + seed;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    // Normalização correta usando a resolução total para evitar cortes nas bordas
                    float sampleX = (x / (float)(resolution - 1)) * baseScale * frequency + octaveOffsets[i].x;
                    float sampleZ = (z / (float)(resolution - 1)) * baseScale * frequency + octaveOffsets[i].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);
                    noiseHeight += (perlinValue * 2f - 1f) * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                float finalHeight = noiseHeight / 2f + 0.5f;
                finalHeight = Mathf.Clamp01(finalHeight);
                finalHeight = Mathf.Pow(finalHeight, 2.5f);

                heights[z, x] = finalHeight;
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }
}