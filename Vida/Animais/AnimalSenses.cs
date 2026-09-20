using UnityEngine;

public class AnimalSenses : MonoBehaviour
{
    private AnimalLife animalLife;

    [Header("Visão (Cone Frontal)")]
    public float visaoDistanciaBase = 20f;
    public float visaoAnguloConeBase = 60f; // Ângulo total do cone (ex: 60 graus à frente)
    [HideInInspector] public float visaoDistanciaReal;
    [HideInInspector] public float visaoAnguloConeReal;

    [Header("Audição (Raio Esférico)")]
    public float audicaoRaioBase = 15f;
    [HideInInspector] public float audicaoRaioReal;

    [Header("Paladar (Interação Direta / Contato)")]
    public float distanciaPaladar = 1.5f; // Distância para provar/comer algo

    void Awake()
    {
        animalLife = GetComponent<AnimalLife>();
        GerarSentidosUnicosDeNascimento();
    }

    void Update()
    {
        AtualizarSentidosPorCrescimento();
    }

    void GerarSentidosUnicosDeNascimento()
    {
        // Cada indivíduo nasce com uma variação aleatória nos sentidos (ex: entre 80% e 120% da base)
        float fatorVisao = Random.Range(0.8f, 1.2f);
        float fatorAudicao = Random.Range(0.8f, 1.2f);

        visaoDistanciaBase *= fatorVisao;
        audicaoRaioBase *= fatorAudicao;
    }

    void AtualizarSentidosPorCrescimento()
    {
        if (animalLife == null) return;

        // Calcula o progresso da vida do animal (0 a 1)
        float progressoVida = Mathf.Clamp01(animalLife.idade / animalLife.idadeMaximaReal);

        // Lógica de crescimento e envelhecimento dos sentidos:
        // - Dos 0% aos 30% da vida (fase de crescimento): Aumenta até +50% dos valores base.
        // - Dos 30% aos 80% da vida (fase adulta): Mantém o ápice.
        // - Dos 80% aos 100% da vida (velhice): Reduz os 50% de volta ao valor original.

        float multiplicadorSentido;

        if (progressoVida <= 0.3f)
        {
            // Crescendo de 1.0x até 1.5x (+50%)
            float t = progressoVida / 0.3f;
            multiplicadorSentido = Mathf.Lerp(1f, 1.5f, t);
        }
        else if (progressoVida <= 0.8f)
        {
            // Fase adulta (mantém o ápice de 1.5x)
            multiplicadorSentido = 1.5f;
        }
        else
        {
            // Velhice (de 1.5x voltando para 1.0x)
            float t = (progressoVida - 0.8f) / 0.2f;
            multiplicadorSentido = Mathf.Lerp(1.5f, 1.0f, t);
        }

        // Aplica os multiplicadores aos raios e distâncias reais
        visaoDistanciaReal = visaoDistanciaBase * multiplicadorSentido;
        visaoAnguloConeReal = visaoAnguloConeBase; // O ângulo pode se manter ou variar se quiser
        audicaoRaioReal = audicaoRaioBase * multiplicadorSentido;
    }

    // --- MÉTODOS DE DETECÇÃO (Para uso futuro na FSM) ---

    public bool EstaNaVisao(Transform alvo)
    {
        Vector3 direcaoParaAlvo = (alvo.position - transform.position).normalized;
        float distanciaParaAlvo = Vector3.Distance(transform.position, alvo.position);

        if (distanciaParaAlvo <= visaoDistanciaReal)
        {
            // Verifica se está dentro do cone de visão
            float angulo = Vector3.Angle(transform.forward, direcaoParaAlvo);
            if (angulo <= visaoAnguloConeReal / 2f)
            {
                // Opcional: Adicionar Raycast aqui para checar se há barreiras físicas (paredes, árvores)
                return true;
            }
        }
        return false;
    }

    public bool EstaNaAudicao(Transform alvo)
    {
        float distanciaParaAlvo = Vector3.Distance(transform.position, alvo.position);
        return distanciaParaAlvo <= audicaoRaioReal;
    }

    // O Paladar é ativado quando o animal interage fisicamente com um objeto/alimento
    public void ProvarAlimento(GameObject objetoAlvo)
    {
        float distancia = Vector3.Distance(transform.position, objetoAlvo.transform.position);
        if (distancia <= distanciaPaladar)
        {
            // Exemplo de lógica de paladar:
            // - Identifica o tipo do objeto
            // - Verifica se está podre (se houver componente de apodrecimento) ou se é comestível para o seu AnimalType
            Debug.Log($"{gameObject.name} provou {objetoAlvo.name} com o paladar.");
        }
    }

    // Desenha os gizmos no Editor do Unity para visualizarmos o cone de visão e o raio de audição
    void OnDrawGizmosSelected()
    {
        // Raio da Audição (Amarelo)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, audicaoRaioReal > 0 ? audicaoRaioReal : audicaoRaioBase);

        // Cone de Visão (Azul)
        Gizmos.color = Color.cyan;
        Vector3 direcaoEsquerda = Quaternion.Euler(0, -visaoAnguloConeBase / 2f, 0) * transform.forward;
        Vector3 direcaoDireita = Quaternion.Euler(0, visaoAnguloConeBase / 2f, 0) * transform.forward;
        float distVisao = visaoDistanciaReal > 0 ? visaoDistanciaReal : visaoDistanciaBase;

        Gizmos.DrawRay(transform.position, direcaoEsquerda * distVisao);
        Gizmos.DrawRay(transform.position, direcaoDireita * distVisao);
    }
}