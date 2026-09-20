using UnityEngine;

public class TreeAgent : MonoBehaviour
{
    [Header("Genética e Traços")]
    public bool isFruitful = false; // Define se é frutífera (herdado da árvore mãe)
    public int treeTypeIndex = 0;   // 0 = Tipo 1, 1 = Tipo 2 (herdado do modelo mãe)
    private float targetMaxScale = 2.0f; // Tamanho máximo individual (ex: entre 1.8m e 2.2m)

    [Header("Ciclo de Vida (Em segundos para teste)")]
    public float currentAge = 0f;
    public float maxLifeSpan = 60f; // Tempo total de vida antes de morrer
    public float growthDuration = 15f; // Tempo que leva para atingir o tamanho máximo

    private bool isDead = false;

    // Controle interno do gerador de frutos
    private FruitGenerator fruitGenerator;
    private bool hasActivatedFruits = false;

    // Referência para o ForestManager avisar quando morrer
    public System.Action<TreeAgent> OnTreeDeath;

    void Start()
    {
        // 1. Variação única para o tamanho máximo desta árvore específica
        targetMaxScale = Random.Range(1.9f, 2.1f);

        // 2. Variação no tempo de vida total da espécie (ex: 15% para mais ou para menos)
        maxLifeSpan = Random.Range(maxLifeSpan * 0.85f, maxLifeSpan * 1.15f);

        // Se nasceu do zero absoluto sem escala definida, ajusta o tamanho inicial
        if (transform.localScale.x <= 0.1f)
        {
            transform.localScale = Vector3.one * 0.1f;
        }

        // Busca o FruitGenerator que está lá no objeto filho (na Copa)
        fruitGenerator = GetComponentInChildren<FruitGenerator>();
    }

    // Método público atualizado para aceitar tanto a genética de frutos quanto o tipo de árvore
    public void InitializeTrait(bool parentFruitful, int parentType)
    {
        isFruitful = parentFruitful;
        treeTypeIndex = parentType;

        // Distinção visual simples para diferenciar frutíferas de normais ou por tipo
        if (TryGetComponent<Renderer>(out Renderer rend))
        {
            if (isFruitful)
            {
                rend.material.color = new Color(0.8f, 0.4f, 0.1f); // Tom alaranjado/amarelado (frutífera)
            }
            else
            {
                // Diferencia levemente a cor baseada no tipo da árvore
                rend.material.color = (treeTypeIndex == 0) ? new Color(0.1f, 0.5f, 0.1f) : new Color(0.2f, 0.4f, 0.3f);
            }
        }
    }

    void Update()
    {
        if (isDead) return;

        // Envelhece a árvore
        currentAge += Time.deltaTime;

        // Fase de Crescimento (calculada com base na idade atual em relação ao tempo de crescimento)
        if (currentAge <= growthDuration)
        {
            float growthProgress = currentAge / growthDuration;
            float currentHeight = Mathf.Lerp(0.1f, targetMaxScale, growthProgress);
            transform.localScale = new Vector3(currentHeight, currentHeight, currentHeight);
        }
        else
        {
            // Garante que fica no tamanho máximo correto após crescer
            transform.localScale = new Vector3(targetMaxScale, targetMaxScale, targetMaxScale);

            // Assim que a árvore conclui o crescimento (fica adulta), ativam-se os frutos (se for frutífera)
            if (isFruitful && !hasActivatedFruits)
            {
                hasActivatedFruits = true;
                ActivateFruitGeneration();
            }
        }

        // Verifica se chegou a hora de morrer
        if (currentAge >= maxLifeSpan)
        {
            Die();
        }
    }

    void ActivateFruitGeneration()
    {
        if (fruitGenerator != null)
        {
            // Otimização: Usa TryGetComponent para evitar alocação de memória caso falhe
            if (fruitGenerator.TryGetComponent<MeshCollider>(out MeshCollider canopyCollider))
            {
                // Inicializa o gerador passando o índice da fruta correspondente e o collider da copa
                fruitGenerator.Initialize(treeTypeIndex, canopyCollider);
            }
            else
            {
                Debug.LogWarning($"[TreeAgent] O objeto da Copa não possui um MeshCollider em {gameObject.name}!");
            }
        }
    }

    void Die()
    {
        isDead = true;

        // Dispara o evento de morte para o gerenciador plantar uma muda nova nas proximidades
        OnTreeDeath?.Invoke(this);

        // Destroi o objeto atual
        Destroy(gameObject);
    }
}