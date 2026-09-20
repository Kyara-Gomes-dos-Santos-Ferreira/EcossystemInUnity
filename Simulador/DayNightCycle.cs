using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Configuração de Tempo")]
    [Tooltip("Quantos segundos reais dura um dia completo (24h) na velocidade 1x.")]
    public float fullDayDurationInSeconds = 120f;

    [Header("Iluminação")]
    public Light directionalLight;
    public Gradient lightColorGradient;
    public AnimationCurve lightIntensityCurve;

    [Header("Leitura (Apenas Leitura)")]
    [field: SerializeField] public float CurrentTimeOfDay { get; private set; } = 0f; // Vai de 0 a 1

    void Update()
    {
        // Calcula o progresso do dia respeitando a escala do TimeManager
        float timeIncrement = (Time.deltaTime * Time.timeScale) / fullDayDurationInSeconds;
        CurrentTimeOfDay += timeIncrement;

        if (CurrentTimeOfDay >= 1f)
        {
            CurrentTimeOfDay = 0f;
        }

        // Rotação completa de 360 graus ao longo do dia
        float sunRotationX = CurrentTimeOfDay * 360f - 90f;
        transform.localRotation = Quaternion.Euler(sunRotationX, 170f, 0f);

        // Atualiza a cor e a intensidade da luz com base no ciclo total
        if (directionalLight != null)
        {
            if (lightColorGradient != null)
            {
                directionalLight.color = lightColorGradient.Evaluate(CurrentTimeOfDay);
            }
            if (lightIntensityCurve != null)
            {
                directionalLight.intensity = lightIntensityCurve.Evaluate(CurrentTimeOfDay);
            }
        }
    }
}