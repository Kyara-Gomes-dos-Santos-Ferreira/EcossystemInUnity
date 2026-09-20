using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class ProceduralWaterGenerator : MonoBehaviour
{
    private Terrain terrain;

    [Header("Configurações da Água")]
    [Tooltip("Altura exata (em unidades do mundo) onde a água deve ficar.")]
    public float waterHeight = 1.7f;

    [Tooltip("Material da água (coloque o material com o shader de água aqui).")]
    public Material waterMaterial;

    private GameObject waterPlane;

    [ContextMenu("Gerar Água Procedural")]
    public void GenerateWater()
    {
        if (terrain == null) terrain = GetComponent<Terrain>();
        TerrainData terrainData = terrain.terrainData;

        // Se já existir um plano de água anterior, remove para atualizar
        Transform existingWater = transform.Find("ProceduralWaterPlane");
        if (existingWater != null)
        {
            DestroyImmediate(existingWater.gameObject);
        }

        // Cria um Quad primitivo para representar a superfície da água
        waterPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        waterPlane.name = "ProceduralWaterPlane";
        waterPlane.transform.SetParent(transform);

        // Centraliza a água na metade do terreno
        float centerX = terrainData.size.x / 2f;
        float centerZ = terrainData.size.z / 2f;

        waterPlane.transform.localPosition = new Vector3(centerX, waterHeight, centerZ);

        // Escala para cobrir todo o terreno
        waterPlane.transform.localScale = new Vector3(terrainData.size.x, terrainData.size.z, 1f);

        // Roda o Quad para ficar deitado na horizontal
        waterPlane.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // Aplica o material de água, se houver
        Renderer waterRenderer = waterPlane.GetComponent<Renderer>();
        if (waterMaterial != null && waterRenderer != null)
        {
            waterRenderer.material = waterMaterial;
        }

        // Remove o Collider com segurança e sem alocação (Otimizado)
        if (waterPlane.TryGetComponent<Collider>(out Collider waterCollider))
        {
            DestroyImmediate(waterCollider);
        }

        Debug.Log($"Água procedural gerada na altura {waterHeight} cobrindo {terrainData.size.x}x{terrainData.size.z}!");
    }
}