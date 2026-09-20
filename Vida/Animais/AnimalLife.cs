using UnityEngine;

public enum AnimalType { Herbivoro, Carnivoro, Onivoro, Piscivoro }
public enum CadeiaAlimentar { Nivel0, Nivel1, Nivel2, Nivel3, Nivel4 }

public class AnimalLife : MonoBehaviour
{
    [Header("Identificação")]
    public AnimalType animalType = AnimalType.Herbivoro;
    public CadeiaAlimentar cadeiaAlimentar = CadeiaAlimentar.Nivel0;
    public string especie = "Cervo";
    public string genero = "Macho";

    [Header("Ciclo de Vida")]
    public float idade = 0f;                    // Idade atual em segundos simulados
    public float idadeMaximaBase = 300f;        // Valor base configurado no Inspector
    [HideInInspector] public float idadeMaximaReal; // Valor final com a variação de ±15%

    [Header("Sistema de Vida (HP)")]
    public float vidaMaxima = 100f;
    public float vidaAtual;
    public float danoPorHoraCritica = 10f;       // Quanto de vida perde por hora simulada em estado crítico (fome/sede em 100)
    private float acumuladorTempoDano = 0f;

    [Header("Crescimento e Escala")]
    public float escalaMinima = 0.4f;           // Tamanho quando recém-nascido (filhote)
    [HideInInspector] public float escalaMaximaReal; // Tamanho máximo único deste indivíduo
    public float velocidadeCrescimento = 0.5f;   // Quão rápido ele chega ao tamanho máximo

    [Header("Atributos Vitais (0 a 100)")]
    public float fome = 0f;      // 100 = morrendo de fome
    public float sede = 0f;      // 100 = morrendo de sede
    public float sono = 0f;      // 100 = exausto
    public float tesao = 0f;     // 100 = quer reproduzir
    public float stamina = 100f; // 100 = revigorado

    [Header("Taxas de Metabolismo (Velocidade que os atributos sobem)")]
    public float taxaFome = 0.5f;
    public float taxaSede = 0.5f;
    public float taxaSono = 0.5f;
    public float taxaTesao = 0.5f;

    [Header("Reprodução")]
    public bool gravida = false;
    public float periodoGravidez = 0f;

    [Header("Movimentação e Física")]
    public float velocidade = 3.5f;
    public float velocidadeCorrendo = 7f;
    public float aceleracao = 5f;
    public float velocidadeRotacao = 5f;

    [Header("Personalidade / Pesos de Prioridade (Gerados no Nascimento)")]
    [HideInInspector] public float pesoFome;
    [HideInInspector] public float pesoSede;
    [HideInInspector] public float pesoSono;
    [HideInInspector] public float pesoTesao;

    [Header("Prefab de Referência")]
    public GameObject prefabReferencia;

    void Awake()
    {
        GerarAtributosUnicosDeNascimento();
        DefinirGeneroAleatorio();
        AplicarTamanhoInicial();

        vidaAtual = vidaMaxima;
    }

    void Update()
    {
        AtualizarCicloDeVida();
        AtualizarMetabolismo();
        VerificarCondicoesVitais();
    }

    void GerarAtributosUnicosDeNascimento()
    {
        // 1. Variação de ±15% na idade máxima
        float variacaoIdade = Random.Range(-0.15f, 0.15f);
        idadeMaximaReal = idadeMaximaBase * (1f + variacaoIdade);

        // 2. Variação de tamanho máximo individual (ex: entre 0.8f e 1.2f do tamanho padrão do prefab)
        float variacaoTamanho = Random.Range(0.8f, 1.2f);
        escalaMaximaReal = 1f * variacaoTamanho;

        // 3. Personalidade única
        pesoFome = Random.Range(1f, 3f);
        pesoSede = Random.Range(1f, 3f);
        pesoSono = Random.Range(1f, 3f);
        pesoTesao = Random.Range(1f, 3f);
    }

    void DefinirGeneroAleatorio()
    {
        genero = Random.value > 0.5f ? "Macho" : "Fêmea";
    }

    void AplicarTamanhoInicial()
    {
        // Começa pequeno (filhote) baseado na escala mínima definida
        transform.localScale = Vector3.one * (escalaMaximaReal * escalaMinima);
    }

    void AtualizarCicloDeVida()
    {
        // O tempo passa
        idade += Time.deltaTime;

        // Crescimento gradual até atingir a escala máxima real
        float progressoCrescimento = Mathf.Clamp01(idade / (idadeMaximaReal * 0.3f)); // Atinge o tamanho adulto aos 30% da vida
        float escalaAtual = Mathf.Lerp(escalaMinima * escalaMaximaReal, escalaMaximaReal, progressoCrescimento);
        transform.localScale = Vector3.one * escalaAtual;

        // Checagem de morte por velhice
        if (idade >= idadeMaximaReal)
        {
            MorrerDeVelhice();
        }
    }

    void AtualizarMetabolismo()
    {
        // Aumenta as necessidades vitais com o tempo multiplicadas pelos pesos da personalidade
        fome = Mathf.Clamp(fome + (taxaFome * pesoFome * Time.deltaTime), 0f, 100f);
        sede = Mathf.Clamp(sede + (taxaSede * pesoSede * Time.deltaTime), 0f, 100f);
        sono = Mathf.Clamp(sono + (taxaSono * pesoSono * Time.deltaTime), 0f, 100f);
        tesao = Mathf.Clamp(tesao + (taxaTesao * pesoTesao * Time.deltaTime), 0f, 100f);
    }

    void VerificarCondicoesVitais()
    {
        bool estaEmPrivacao = (fome >= 100f || sede >= 100f);

        if (estaEmPrivacao)
        {
            acumuladorTempoDano += Time.deltaTime;

            if (acumuladorTempoDano >= 60f) // 60 segundos reais = 1 hora simulada
            {
                vidaAtual -= danoPorHoraCritica;
                acumuladorTempoDano = 0f;

                if (vidaAtual <= 0f)
                {
                    vidaAtual = 0f;
                    string motivo = fome >= 100f ? "fome" : "sede";
                    MorrerDeFomeOuSede(motivo);
                }
            }
        }
        else
        {
            acumuladorTempoDano = 0f;
        }
    }

    void MorrerDeVelhice()
    {
        Debug.Log($"{especie} morreu de velhice com {idade:F1} segundos.");
        Destroy(gameObject);
    }

    void MorrerDeFomeOuSede(string motivo)
    {
        Debug.Log($"{especie} morreu de {motivo}.");
        Destroy(gameObject);
    }
}