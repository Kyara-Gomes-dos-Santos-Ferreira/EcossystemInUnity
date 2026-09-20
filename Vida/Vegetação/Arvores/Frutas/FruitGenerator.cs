using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FruitGenerator : MonoBehaviour
{
    [Header("Configuração de Frutas")]
    [Tooltip("Lista global de prefabs de frutas disponíveis no ecossistema.")]
    public List<GameObject> availableFruitPrefabs;

    [Header("Materiais Padrão Universais")]
    public Material immatureMaterial;
    public Material rottenMaterial;

    [Header("Parâmetros de Geração")]
    public int maxFruitsOnTree = 10;      // Número máximo de frutos simultâneos na copa
    public float spawnInterval = 5f;      // Tempo entre cada tentativa de spawn

    private MeshCollider canopyMeshCollider;
    private int treeFruitTypeIndex = 0;   // Definido pela genética da árvore
    private bool isGenerating = false;

    public void Initialize(int fruitType, MeshCollider canopyCollider)
    {
        treeFruitTypeIndex = fruitType;
        canopyMeshCollider = canopyCollider;

        // Inicia o ciclo de geração de frutas quando a árvore estiver pronta
        if (!isGenerating && canopyMeshCollider != null)
        {
            isGenerating = true;
            StartCoroutine(SpawnRoutine());
        }
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (canopyMeshCollider == null) yield break;

            // Conta quantos frutos já estão grudados na árvore
            int currentFruitCount = transform.GetComponentsInChildren<FruitAgent>().Length;

            if (currentFruitCount < maxFruitsOnTree)
            {
                SpawnFruit();
            }
        }
    }

    void SpawnFruit()
    {
        if (availableFruitPrefabs == null || availableFruitPrefabs.Count == 0) return;

        if (treeFruitTypeIndex < 0 || treeFruitTypeIndex >= availableFruitPrefabs.Count) return;

        GameObject fruitPrefab = availableFruitPrefabs[treeFruitTypeIndex];
        if (fruitPrefab == null) return;

        // Sorteia um ponto aleatório na superfície do MeshCollider da copa
        Vector3 randomPoint = GetRandomPointOnMesh(canopyMeshCollider);

        // Instancia a fruta na posição sorteada, fazendo dela filha da árvore
        GameObject newFruit = Instantiate(fruitPrefab, randomPoint, Random.rotation, transform);

        // CORREÇÃO: Força a escala local inicial para evitar que herde o tamanho gigante da árvore pai
        newFruit.transform.localScale = Vector3.one * 0.5f; // Começa pequenininha como um broto

        // Configura o FruitAgent recém-criado com os materiais universais
        if (newFruit.TryGetComponent<FruitAgent>(out FruitAgent fruitAgent))
        {
            fruitAgent.fruitTypeIndex = treeFruitTypeIndex;
            fruitAgent.immatureMaterial = immatureMaterial;
            fruitAgent.rottenMaterial = rottenMaterial;
        }
    }

    Vector3 GetRandomPointOnMesh(MeshCollider meshCollider)
    {
        Mesh mesh = meshCollider.sharedMesh;
        if (mesh == null) return meshCollider.transform.position;

        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        if (triangles.Length == 0) return meshCollider.transform.position;

        int triangleIndex = Random.Range(0, triangles.Length / 3) * 3;

        Vector3 v0 = vertices[triangles[triangleIndex]];
        Vector3 v1 = vertices[triangles[triangleIndex + 1]];
        Vector3 v2 = vertices[triangles[triangleIndex + 2]];

        float r1 = Mathf.Sqrt(Random.Range(0f, 1f));
        float r2 = Random.Range(0f, 1f);

        Vector3 localPoint = (1f - r1) * v0 + (r1 * (1f - r2)) * v1 + (r1 * r2) * v2;

        return meshCollider.transform.TransformPoint(localPoint);
    }
}