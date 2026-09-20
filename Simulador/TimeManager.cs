using UnityEngine;

public class TimeManager : MonoBehaviour
{
    // Instância estática para facilitar o acesso de qualquer outro script (Singleton simples)
    public static TimeManager Instance { get; private set; }

    [Header("Configurações de Tempo")]
    [SerializeField] private float currentTimeMultiplier = 1.0f;
    private float previousTimeMultiplier = 1.0f; // Usado para lembrar a velocidade antes de pausar
    private bool isPaused = false;

    // Propriedade pública para os outros scripts consultarem se precisarem
    public float CurrentTimeMultiplier => isPaused ? 0f : currentTimeMultiplier;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Método para alterar a velocidade (ex: 1 para normal, 2 para 2x)
    public void SetTimeMultiplier(float multiplier)
    {
        if (isPaused) return; // Se estiver pausado, muda apenas o alvo, mas mantém o tempo parado

        currentTimeMultiplier = Mathf.Max(0f, multiplier);
        Time.timeScale = currentTimeMultiplier; // Afeta a física global do Unity
    }

    // Pausa o jogo
    public void PauseGame()
    {
        isPaused = true;
        previousTimeMultiplier = currentTimeMultiplier;
        Time.timeScale = 0f; // Congela o tempo global e a física
    }

    // Despausa o jogo voltando à velocidade anterior
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = previousTimeMultiplier;
    }

    // Alterna entre Pausado e Despausado (ideal para botão de Toggle)
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    // Atalhos específicos para botões de UI
    public void SetNormalSpeed() => SetTimeMultiplier(1.0f);
    public void SetDoubleSpeed() => SetTimeMultiplier(2.0f);
    public void SetQuadSpeed() => SetTimeMultiplier(4.0f); // Bônus: opcional para 4x
}