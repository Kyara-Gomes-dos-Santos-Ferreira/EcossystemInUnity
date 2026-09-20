using UnityEngine;
using UnityEngine.UI;

public class PlayPauseButtonUI : MonoBehaviour
{
    [Header("Texturas dos Botões")]
    [SerializeField] private Texture playTexture;   // Mostra quando está pausado
    [SerializeField] private Texture pauseTexture; // Mostra quando está rodando

    private RawImage targetRawImage;

    void Start()
    {
        // Pega o componente RawImage que está no objeto filho
        targetRawImage = GetComponentInChildren<RawImage>();

        if (targetRawImage == null)
        {
            Debug.LogWarning($"[PlayPauseButtonUI] Nenhuma RawImage foi encontrada nos filhos de {gameObject.name}!");
        }

        // Atualiza a textura inicial correta logo ao iniciar
        UpdateTexture();
    }

    // Chamado automaticamente a cada frame para manter o ícone sincronizado
    void Update()
    {
        UpdateTexture();
    }

    void UpdateTexture()
    {
        if (targetRawImage == null || TimeManager.Instance == null) return;

        // Verifica se o tempo está pausado (multiplicador igual a 0)
        bool isPaused = (TimeManager.Instance.CurrentTimeMultiplier == 0f);

        if (isPaused)
        {
            targetRawImage.texture = playTexture;   // Exibe textura de Play para retomar
        }
        else
        {
            targetRawImage.texture = pauseTexture; // Exibe textura de Pause para parar
        }
    }
}