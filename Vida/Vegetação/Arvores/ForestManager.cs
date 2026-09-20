using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class ForestManager : MonoBehaviour
{
    private Terrain terrain;
    private TerrainData terrainData;

    [Header("Configurações de Spawn")]
    public GameObject[] treePrefabs; // Array para múltiplos tipos/modelos de árvores
    public int initialTreeCount = 150; // Quantidade de árvores na primeira geração

    [Header("Regras de Bioma e Altura")]
    public float waterHeightLimit = 0.085f; // Altura mínima (mesma da água) para não afogar as árvores
    public float maxTerrainHeight = 18f;  // Altura máxima para evitar o topo seco das montanhas

    [Header("Probabilidade Genética Inicial")]
    [Range(0f, 1f)]
    public float fruitfulChance = 0.3f; // 30% de chance da árvore inicial ser frutífera

    private readonly List<TreeAgent> activeTrees = new();
    private Transform treesContainer; // Objeto pai "Árvores"

    [ContextMenu("Gerar Primeira Geração de Árvores")]
    public void GenerateInitialForest()
    {
        if (terrain == null) terrain = GetComponent<Terrain>();
        terrainData = terrain.terrainData;

        if (treePrefabs == null || treePrefabs.Length == 0)
        {
            Debug.LogError("Nenhum prefab de árvore foi configurado no ForestManager!");
            return;
        }

        // Configura o Container Pai "Árvores" na Hierarchy
        SetupTreesContainer();

        // Limpa árvores anteriores se já existirem na cena
        foreach (var tree in activeTrees)
        {
            if (tree != null) DestroyImmediate(tree.gameObject);
        }
        activeTrees.Clear();

        int spawnedCount = 0;
        int safetyCounter = 0; // Evita loop infinito caso os limites estejam muito restritos

        while (spawnedCount < initialTreeCount && safetyCounter < initialTreeCount * 10)
        {
            safetyCounter++;

            // Sorteia uma posição X e Z aleatória dentro do tamanho do terreno
            float randomX = Random.Range(10f, terrainData.size.x - 10f);
            float randomZ = Random.Range(10f, terrainData.size.z - 10f);

            // Descobre a altura exata (Y) do terreno naquela coordenada X, Z
            float worldY = terrain.SampleHeight(new Vector3(randomX, 0f, randomZ)) + terrain.transform.position.y;

            // Valida as regras de bioma: não pode nascer na água/lagos e nem muito alto nas montanhas
            if (worldY > waterHeightLimit && worldY < maxTerrainHeight)
            {
                Vector3 spawnPosition = new(randomX, worldY, randomZ);

                // Sorteia aleatoriamente qual dos modelos de árvore vai nascer (Tipo 0, Tipo 1, etc.)
                int chosenTypeIndex = Random.Range(0, treePrefabs.Length);
                GameObject selectedPrefab = treePrefabs[chosenTypeIndex];

                // Instancia a árvore
                GameObject treeObj = Instantiate(selectedPrefab, spawnPosition, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                treeObj.transform.SetParent(treesContainer); // Organiza dentro do container "Árvores"

                if (treeObj.TryGetComponent<TreeAgent>(out TreeAgent treeAgent))
                {
                    // Sorteia se esta árvore da primeira geração é frutífera ou não
                    bool isFruitfulInitial = Random.value <= fruitfulChance;

                    // Inicializa a árvore passando a genética de frutos e o tipo dela
                    treeAgent.InitializeTrait(isFruitfulInitial, chosenTypeIndex);

                    // --- DESINCRONIZAÇÃO DA PRIMEIRA GERAÇÃO ---
                    treeAgent.currentAge = Random.Range(0f, treeAgent.maxLifeSpan * 0.7f);

                    // Inscreve no evento de morte para nascer uma nova muda quando ela expirar
                    treeAgent.OnTreeDeath += HandleTreeDeath;

                    activeTrees.Add(treeAgent);
                }

                spawnedCount++;
            }
        }

        Debug.Log($"Floresta inicial gerada com sucesso! Total de árvores: {activeTrees.Count}");
    }

    // Cria ou busca o objeto pai "Árvores" para manter o Inspector limpo
    private void SetupTreesContainer()
    {
        Transform existingContainer = transform.Find("Árvores");
        if (existingContainer != null)
        {
            treesContainer = existingContainer;
        }
        else
        {
            GameObject containerObj = new("Árvores");
            containerObj.transform.SetParent(transform);
            containerObj.transform.localPosition = Vector3.zero;
            treesContainer = containerObj.transform;
        }
    }

    // Callback executado automaticamente quando uma árvore morre
    private void HandleTreeDeath(TreeAgent deadTree)
    {
        // Remove da lista de ativas
        activeTrees.Remove(deadTree);

        // Quantas mudas vão nascer? (Padrão 1, mas com 5% de chance nascem 2)
        int spawnCount = (Random.value <= 0.05f) ? 2 : 1;

        for (int i = 0; i < spawnCount; i++)
        {
            // Regra de regeneração: Spawna a muda nas proximidades
            Vector3 deathPos = deadTree.transform.position;

            // Joga a nova muda ligeiramente deslocada do tronco da antiga (num raio de 3 metros)
            Vector2 offset2D = Random.insideUnitCircle * 3f;
            Vector3 spawnPos = new(deathPos.x + offset2D.x, 0f, deathPos.z + offset2D.y);

            // Recalcula a altura Y correta no terreno para a nova muda
            spawnPos.y = terrain.SampleHeight(spawnPos) + terrain.transform.position.y;

            // Só nasce se estiver acima da água
            if (spawnPos.y > waterHeightLimit)
            {
                // A filha herda exatamente o mesmo modelo/prefab da árvore mãe!
                GameObject parentPrefab = treePrefabs[Mathf.Clamp(deadTree.treeTypeIndex, 0, treePrefabs.Length - 1)];

                GameObject newTreeObj = Instantiate(parentPrefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                newTreeObj.transform.SetParent(treesContainer); // Organizado dentro do container "Árvores"

                if (newTreeObj.TryGetComponent<TreeAgent>(out TreeAgent newAgent))
                {
                    // Herda o traço genético da mãe (frutos e o mesmo tipo de árvore)
                    newAgent.InitializeTrait(deadTree.isFruitful, deadTree.treeTypeIndex);
                    newAgent.OnTreeDeath += HandleTreeDeath;

                    activeTrees.Add(newAgent);
                }
            }
        }
    }
}