using UnityEngine;
using UnityEngine.AI;

public enum AnimalState
{
    Parado,
    Vagando,
    Perseguicao,
    Fuga,
    CacandoComida,
    CacandoAgua,
    CacandoParceiro,
    Dormindo,
    Reproduzindo,
    Comendo,
    Bebendo
}

public class AnimalFSM : MonoBehaviour
{
    private AnimalLife animalLife;
    private AnimalSenses animalSenses;
    private NavMeshAgent agent;
    private Vector3 destinoAtual;
    private bool temDestinoVagando = false;
    private Transform alvoAtual; // Usado para perseguição ou fuga direcionada

    private readonly Collider[] collidersProximos = new Collider[30];

    [Header("Estado Atual")]
    public AnimalState estadoAtual = AnimalState.Vagando;

    [Header("Configurações de Comportamento")]
    public float limiarUrgenciaNecessidade = 100f; // Limiar ponderado para decidir agir sobre uma necessidade
    public float tempoMinimoTrocaEstado = 3f;      // Cooldown mínimo para evitar oscilação rápida entre estados
    private float cronometroTrocaEstado = 0f;       // Acumulador de tempo interno

    [Header("Configurações de Vagar")]
    public float raioVagarMaximo = 15f; // Distância máxima que ele escolhe para andar
    public float distanciaChegada = 1f;   // Quão perto ele precisa chegar para considerar que chegou (Vagar)
    public float raioChegadaAlvo = 2f;    // Margem de tolerância para considerar que chegou na água, comida ou alvos

    [Header("Configurações de Combate/Fuga")]
    public float distanciaFuga = 15f; // Distância que tenta correr para longe do predador

    void Awake()
    {
        animalLife = GetComponent<AnimalLife>();
        animalSenses = GetComponent<AnimalSenses>();
        agent = GetComponent<NavMeshAgent>();

        // Configura o NavMeshAgent automaticamente com base nos dados do AnimalLife
        if (agent != null && animalLife != null)
        {
            agent.speed = animalLife.velocidade;
            agent.acceleration = animalLife.aceleracao;
            agent.angularSpeed = animalLife.velocidadeRotacao * 50f;
        }
    }

    void Update()
    {
        // Atualiza o temporizador de cooldown do estado
        if (cronometroTrocaEstado > 0f)
        {
            cronometroTrocaEstado -= Time.deltaTime;
        }

        AvaliarPrioridadesETransicoes();
        ExecutarEstadoAtual();
    }

    void MudarEstado(AnimalState novoEstado)
    {
        if (estadoAtual != novoEstado)
        {
            estadoAtual = novoEstado;
            temDestinoVagando = false;
            cronometroTrocaEstado = tempoMinimoTrocaEstado; // Reseta o cooldown de 3 segundos
            if (agent != null) agent.ResetPath();
        }
    }

    void AvaliarPrioridadesETransicoes()
    {
        // Se estiver dormindo, verifica se já descansou o suficiente ou se há perigo crítico
        if (estadoAtual == AnimalState.Dormindo)
        {
            if (animalLife.sono <= 10f)
            {
                MudarEstado(AnimalState.Parado);
            }

            // Exceção severa: Fuga ignora qualquer cooldown
            if (VerificarAmeacasNaRegiao(out Transform predadorDormindo))
            {
                alvoAtual = predadorDormindo;
                MudarEstado(AnimalState.Fuga);
            }
            return;
        }

        // 1. PRIORIDADE MÁXIMA ABSOLUTA: FUGA (Interrompe qualquer coisa imediatamente, ignorando cooldown)
        if (VerificarAmeacasNaRegiao(out Transform predador))
        {
            alvoAtual = predador;
            if (estadoAtual != AnimalState.Fuga)
            {
                MudarEstado(AnimalState.Fuga);
            }
            return;
        }
        else if (estadoAtual == AnimalState.Fuga)
        {
            // Se estava fugindo mas o predador sumiu/ficou longe, limpa o alvo e volta a vagar
            alvoAtual = null;
            MudarEstado(AnimalState.Vagando);
        }

        // Se ainda estiver dentro do tempo de cooldown de 3 segundos, bloqueia trocas comuns de estado
        if (cronometroTrocaEstado > 0f) return;

        // 2. COMPETIÇÃO IGUALITÁRIA ENTRE AS 4 NECESSIDADES
        float urgenciaFome = animalLife.fome * animalLife.pesoFome;
        float urgenciaSede = animalLife.sede * animalLife.pesoSede;
        float urgenciaSono = animalLife.sono * animalLife.pesoSono;
        float urgenciaTesao = animalLife.tesao * animalLife.pesoTesao;

        float maiorUrgencia = Mathf.Max(urgenciaFome, Mathf.Max(urgenciaSede, Mathf.Max(urgenciaSono, urgenciaTesao)));

        if (maiorUrgencia >= limiarUrgenciaNecessidade)
        {
            AnimalState novoEstado = estadoAtual;

            if (maiorUrgencia == urgenciaSono) novoEstado = AnimalState.Dormindo;
            else if (maiorUrgencia == urgenciaSede) novoEstado = AnimalState.CacandoAgua;
            else if (maiorUrgencia == urgenciaFome) novoEstado = AnimalState.CacandoComida;
            else if (maiorUrgencia == urgenciaTesao) novoEstado = AnimalState.CacandoParceiro;

            if (estadoAtual != novoEstado)
            {
                MudarEstado(novoEstado);
                return;
            }
        }
    }

    bool VerificarAmeacasNaRegiao(out Transform transformPredador)
    {
        transformPredador = null;
        if (animalSenses == null || animalLife == null) return false;

        float raioBusca = Mathf.Max(animalSenses.audicaoRaioReal, animalSenses.visaoDistanciaReal);
        int quantidadeEncontrada = Physics.OverlapSphereNonAlloc(transform.position, raioBusca, collidersProximos);

        for (int i = 0; i < quantidadeEncontrada; i++)
        {
            Collider col = collidersProximos[i];
            if (col.gameObject == gameObject) continue;

            if (col.TryGetComponent<AnimalLife>(out AnimalLife outroAnimal))
            {
                if (EAmeaca(outroAnimal))
                {
                    float distanciaParaAlvo = Vector3.Distance(transform.position, col.transform.position);

                    if (estadoAtual == AnimalState.Dormindo)
                    {
                        if (distanciaParaAlvo <= animalSenses.audicaoRaioReal)
                        {
                            transformPredador = col.transform;
                            return true;
                        }
                    }
                    else
                    {
                        bool ouviu = distanciaParaAlvo <= animalSenses.audicaoRaioReal;
                        bool viu = animalSenses.EstaNaVisao(col.transform);

                        if (ouviu || viu)
                        {
                            transformPredador = col.transform;
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    bool EAmeaca(AnimalLife outro)
    {
        bool outroEhPredador = (outro.animalType == AnimalType.Carnivoro || outro.animalType == AnimalType.Onivoro);

        if (animalLife.animalType == AnimalType.Herbivoro && outroEhPredador) return true;
        if (outroEhPredador && (int)outro.cadeiaAlimentar > (int)animalLife.cadeiaAlimentar) return true;

        return false;
    }

    void ExecutarEstadoAtual()
    {
        switch (estadoAtual)
        {
            case AnimalState.Parado:
                if (agent != null && agent.hasPath) agent.ResetPath();
                temDestinoVagando = false;
                break;

            case AnimalState.Vagando:
                if (agent != null) agent.speed = animalLife.velocidade;

                if (!temDestinoVagando)
                {
                    if (GerarPontoAleatorioNavMesh(transform.position, raioVagarMaximo, out Vector3 pontoEncontrado))
                    {
                        destinoAtual = pontoEncontrado;
                        agent.SetDestination(destinoAtual);
                        temDestinoVagando = true;
                    }
                }
                else
                {
                    if (!agent.pathPending && agent.remainingDistance <= distanciaChegada)
                    {
                        temDestinoVagando = false;
                        MudarEstado(AnimalState.Parado);
                    }
                }
                break;

            case AnimalState.Perseguicao:
                if (agent != null) agent.speed = animalLife.velocidadeCorrendo;
                temDestinoVagando = false;

                if (alvoAtual != null)
                {
                    agent.SetDestination(alvoAtual.position);
                }
                else
                {
                    MudarEstado(AnimalState.Vagando);
                }
                break;

            case AnimalState.Fuga:
                if (agent != null) agent.speed = animalLife.velocidadeCorrendo;
                temDestinoVagando = false;

                if (alvoAtual != null)
                {
                    Vector3 direcaoFuga = (transform.position - alvoAtual.position).normalized;
                    Vector3 posicaoFuga = transform.position + (direcaoFuga * distanciaFuga);

                    if (NavMesh.SamplePosition(posicaoFuga, out NavMeshHit hit, distanciaFuga, NavMesh.AllAreas))
                    {
                        agent.SetDestination(hit.position);
                    }
                }
                break;

            case AnimalState.CacandoComida:
                if (agent != null) agent.speed = animalLife.velocidade;
                temDestinoVagando = false;

                // --- 1. LÓGICA PARA CARNÍVOROS ---
                if (animalLife.animalType == AnimalType.Carnivoro)
                {
                    if (ProcurarPresaParaCacar(out Transform presaEncontrada))
                    {
                        alvoAtual = presaEncontrada;
                        MudarEstado(AnimalState.Perseguicao);
                    }
                    else
                    {
                        ComportamentoVagarAtivo();
                    }
                }
                // --- 2. LÓGICA PARA HERBÍVOROS ---
                else if (animalLife.animalType == AnimalType.Herbivoro)
                {
                    if (ProcurarFrutoProximo(out Transform frutoEncontrado))
                    {
                        alvoAtual = frutoEncontrado;
                        agent.SetDestination(alvoAtual.position);

                        if (!agent.pathPending && agent.remainingDistance <= raioChegadaAlvo)
                        {
                            Destroy(alvoAtual.gameObject);
                            animalLife.fome = Mathf.Clamp(animalLife.fome + 30f, 0f, 100f);
                            alvoAtual = null;

                            if (animalLife.fome >= 90f)
                            {
                                MudarEstado(AnimalState.Vagando);
                            }
                        }
                    }
                    else
                    {
                        TerrainGrassManager grassManager = FindFirstObjectByType<TerrainGrassManager>();
                        if (grassManager != null && grassManager.CheckGrassAtPosition(transform.position) > 0)
                        {
                            if (agent != null && agent.hasPath) agent.ResetPath();
                            grassManager.EatGrassAtPosition(transform.position, 2);
                            animalLife.fome = Mathf.Clamp(animalLife.fome + (10f * Time.deltaTime), 0f, 100f);

                            if (animalLife.fome >= 90f)
                            {
                                MudarEstado(AnimalState.Vagando);
                            }
                        }
                        else
                        {
                            ComportamentoVagarAtivo();
                        }
                    }
                }
                // --- 3. LÓGICA PARA PISCÍVOROS ---
                else if (animalLife.animalType == AnimalType.Piscivoro)
                {
                    if (ProcurarAguaProxima(out Transform aguaEncontrada))
                    {
                        alvoAtual = aguaEncontrada;
                        agent.SetDestination(alvoAtual.position);

                        if (!agent.pathPending && agent.remainingDistance <= raioChegadaAlvo)
                        {
                            animalLife.fome = Mathf.Clamp(animalLife.fome + (15f * Time.deltaTime), 0f, 100f);

                            if (animalLife.fome >= 90f)
                            {
                                MudarEstado(AnimalState.Vagando);
                            }
                        }
                    }
                    else
                    {
                        ComportamentoVagarAtivo();
                    }
                }
                // --- 4. LÓGICA PARA ONÍVOROS ---
                else if (animalLife.animalType == AnimalType.Onivoro)
                {
                    if (ProcurarPresaParaCacar(out Transform presaOnivoro))
                    {
                        alvoAtual = presaOnivoro;
                        MudarEstado(AnimalState.Perseguicao);
                    }
                    else if (ProcurarFrutoProximo(out Transform frutoOnivoro))
                    {
                        alvoAtual = frutoOnivoro;
                        agent.SetDestination(frutoOnivoro.position);
                    }
                    else if (ProcurarAguaProxima(out Transform aguaOnivoro))
                    {
                        alvoAtual = aguaOnivoro;
                        agent.SetDestination(aguaOnivoro.position);
                    }
                    else
                    {
                        TerrainGrassManager grassManager = FindFirstObjectByType<TerrainGrassManager>();
                        if (grassManager != null && grassManager.CheckGrassAtPosition(transform.position) > 0)
                        {
                            if (agent != null && agent.hasPath) agent.ResetPath();
                            grassManager.EatGrassAtPosition(transform.position, 2);
                            animalLife.fome = Mathf.Clamp(animalLife.fome + (10f * Time.deltaTime), 0f, 100f);

                            if (animalLife.fome >= 90f)
                            {
                                MudarEstado(AnimalState.Vagando);
                            }
                        }
                        else
                        {
                            ComportamentoVagarAtivo();
                        }
                    }
                }
                break;

            case AnimalState.CacandoAgua:
                if (agent != null) agent.speed = animalLife.velocidade;
                temDestinoVagando = false;

                if (ProcurarAguaProxima(out Transform aguaEncontradaCacando))
                {
                    alvoAtual = aguaEncontradaCacando;
                    agent.SetDestination(alvoAtual.position);

                    // Usamos a distância real em 3D para garantir que a transição ocorra sem falhas do NavMesh
                    float distanciaReal = Vector3.Distance(transform.position, alvoAtual.position);

                    if (distanciaReal <= raioChegadaAlvo)
                    {
                        MudarEstado(AnimalState.Bebendo);
                    }
                }
                else
                {
                    ComportamentoVagarAtivo();
                }
                break;

            case AnimalState.CacandoParceiro:
                if (agent != null) agent.speed = animalLife.velocidade;
                temDestinoVagando = false;
                break;

            case AnimalState.Dormindo:
                if (agent != null && agent.hasPath) agent.ResetPath();
                temDestinoVagando = false;
                animalLife.sono = Mathf.Clamp(animalLife.sono - (5f * Time.deltaTime), 0f, 100f);
                break;

            case AnimalState.Reproduzindo:
            case AnimalState.Comendo:
                if (agent != null && agent.hasPath) agent.ResetPath();
                temDestinoVagando = false;
                break;

            case AnimalState.Bebendo:
                if (agent != null && agent.hasPath) agent.ResetPath();
                temDestinoVagando = false;

                // Diminui gradualmente a vontade de beber enquanto está na água
                animalLife.sede = Mathf.Clamp(animalLife.sede - (20f * Time.deltaTime), 0f, 100f);

                // Quando a vontade de beber zerar (ou estiver muito baixa), limpa o alvo e volta a vagar
                if (animalLife.sede <= 5f)
                {
                    alvoAtual = null;
                    MudarEstado(AnimalState.Vagando);
                }
                break;
        }
    }

    void ComportamentoVagarAtivo()
    {
        if (!temDestinoVagando)
        {
            if (GerarPontoAleatorioNavMesh(transform.position, raioVagarMaximo, out Vector3 pontoEncontrado))
            {
                destinoAtual = pontoEncontrado;
                agent.SetDestination(destinoAtual);
                temDestinoVagando = true;
            }
        }
        else
        {
            if (!agent.pathPending && agent.remainingDistance <= distanciaChegada)
            {
                temDestinoVagando = false;
            }
        }
    }

    bool ProcurarPresaParaCacar(out Transform presaEncontrada)
    {
        presaEncontrada = null;
        if (animalSenses == null) return false;

        float raioBusca = Mathf.Max(animalSenses.audicaoRaioReal, animalSenses.visaoDistanciaReal);
        int quantidadeEncontrada = Physics.OverlapSphereNonAlloc(transform.position, raioBusca, collidersProximos);

        for (int i = 0; i < quantidadeEncontrada; i++)
        {
            Collider col = collidersProximos[i];
            if (col.gameObject == gameObject) continue;

            if (col.TryGetComponent<AnimalLife>(out AnimalLife outroAnimal))
            {
                if (!EAmeaca(outroAnimal) && outroAnimal.animalType != animalLife.animalType)
                {
                    float distanciaParaAlvo = Vector3.Distance(transform.position, col.transform.position);
                    bool ouviu = distanciaParaAlvo <= animalSenses.audicaoRaioReal;
                    bool viu = animalSenses.EstaNaVisao(col.transform);

                    if (ouviu || viu)
                    {
                        presaEncontrada = col.transform;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    bool ProcurarFrutoProximo(out Transform frutoEncontrado)
    {
        frutoEncontrado = null;
        if (animalSenses == null) return false;

        int quantidadeEncontrada = Physics.OverlapSphereNonAlloc(transform.position, animalSenses.visaoDistanciaReal, collidersProximos);

        for (int i = 0; i < quantidadeEncontrada; i++)
        {
            Collider col = collidersProximos[i];
            if (col.CompareTag("Fruto"))
            {
                if (animalSenses.EstaNaVisao(col.transform))
                {
                    frutoEncontrado = col.transform;
                    return true;
                }
            }
        }
        return false;
    }

    bool ProcurarAguaProxima(out Transform aguaEncontrada)
    {
        aguaEncontrada = null;
        if (animalSenses == null) return false;

        int quantidadeEncontrada = Physics.OverlapSphereNonAlloc(transform.position, animalSenses.visaoDistanciaReal, collidersProximos);

        for (int i = 0; i < quantidadeEncontrada; i++)
        {
            Collider col = collidersProximos[i];
            if (col.CompareTag("Water"))
            {
                if (animalSenses.EstaNaVisao(col.transform))
                {
                    aguaEncontrada = col.transform;
                    return true;
                }
            }
        }
        return false;
    }

    bool GerarPontoAleatorioNavMesh(Vector3 origem, float raio, out Vector3 resultado)
    {
        Vector3 direcaoAleatoria = Random.insideUnitSphere * raio;
        direcaoAleatoria += origem;

        if (NavMesh.SamplePosition(direcaoAleatoria, out NavMeshHit hit, raio, NavMesh.AllAreas))
        {
            resultado = hit.position;
            return true;
        }

        resultado = origem;
        return false;
    }
}