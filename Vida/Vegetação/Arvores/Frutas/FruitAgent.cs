using UnityEngine;

public class FruitAgent : MonoBehaviour
{
    [Header("Identificação e Genética")]
    public int fruitTypeIndex = 0;
    public float maxNutritionalValue = 20f;

    [Header("Tempos (em segundos)")]
    public float maturationDuration = 10f;
    public float matureDuration = 15f;
    public float rotDuration = 30f;

    [Header("Materiais Padrão Universais")]
    public Material immatureMaterial;
    public Material rottenMaterial;

    private float currentMaturationTime = 0f;
    private float currentMatureTime = 0f;
    private float currentRotTime = 0f;

    private bool isMature = false;
    private bool isRotting = false;
    private bool hasDropped = false;
    private Rigidbody rb;
    private Collider col;
    private Renderer rend;

    private Material matureMaterial;
    private Material instantiatedMaterial;

    public float NutritionalValue
    {
        get
        {
            if (!isMature)
            {
                float progress = Mathf.Clamp01(currentMaturationTime / maturationDuration);
                return maxNutritionalValue * progress;
            }
            else if (!isRotting)
            {
                return maxNutritionalValue;
            }
            else
            {
                float rotProgress = Mathf.Clamp01(currentRotTime / rotDuration);
                return Mathf.Lerp(maxNutritionalValue, 0f, rotProgress);
            }
        }
    }

    void Start()
    {
        rb = GetComponentInChildren<Rigidbody>();
        col = GetComponentInChildren<Collider>();

        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
        foreach (var r in childRenderers)
        {
            string objName = r.gameObject.name.ToLower();
            if (!objName.Contains("caule") && !objName.Contains("stem"))
            {
                rend = r;
                break;
            }
        }

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (col != null)
        {
            col.enabled = false;
        }

        if (rend != null)
        {
            matureMaterial = rend.sharedMaterial;
            instantiatedMaterial = new Material(matureMaterial);
            rend.material = instantiatedMaterial;
        }

        UpdateVisuals(0f, 0f);
    }

    void Update()
    {
        if (!hasDropped && transform.parent == null)
        {
            DropFruit();
        }

        float speedMultiplier = hasDropped ? 0.2f : 1.0f;

        if (!isMature)
        {
            currentMaturationTime += Time.deltaTime * speedMultiplier;
            float progress = Mathf.Clamp01(currentMaturationTime / maturationDuration);
            UpdateVisuals(progress, 0f);

            if (progress >= 1.0f)
            {
                isMature = true;
                currentMatureTime = 0f;
            }
        }
        else if (!isRotting)
        {
            currentMatureTime += Time.deltaTime * speedMultiplier;
            UpdateVisuals(1.0f, 0f);

            if (currentMatureTime >= matureDuration)
            {
                DropFruit();
                isRotting = true;
                currentRotTime = 0f;
            }
        }
        else
        {
            currentRotTime += Time.deltaTime * speedMultiplier;
            float rotProgress = Mathf.Clamp01(currentRotTime / rotDuration);
            UpdateVisuals(1.0f, rotProgress);

            if (currentRotTime >= rotDuration)
            {
                Destroy(gameObject);
            }
        }
    }

    void DropFruit()
    {
        if (hasDropped) return;

        // Mantém a redução pela metade que você ajustou na queda
        Vector3 currentWorldScale = transform.lossyScale * 0.25f;

        hasDropped = true;
        //transform.SetParent(null);

        // Procura pelo GameObject chamado "frutos" na cena
        GameObject frutosContainer = GameObject.Find("Frutos");

        // Se o objeto "frutos" não existir na cena, criamos um automaticamente para evitar erros
        if (frutosContainer == null)
        {
            frutosContainer = new GameObject("Frutos");
        }

        // Define o objeto "frutos" como o novo pai, mantendo a escala mundial correta
        transform.SetParent(frutosContainer.transform);

        transform.localScale = currentWorldScale;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (col != null)
        {
            col.enabled = true;
        }
    }

    void UpdateVisuals(float maturationProgress, float rotProgress)
    {
        // Define a escala base do crescimento
        float scale = maturationProgress < 1.0f ? Mathf.Lerp(0.05f, 0.1f, maturationProgress) : Mathf.Lerp(0.1f, 0.09f, rotProgress);

        if (hasDropped)
        {
            // Escala quando já caiu no chão (mantendo o fator que você definiu)
            transform.localScale = Vector3.one * (scale * 2.5f);
        }
        else
        {
            // CORREÇÃO: Enquanto está na árvore, dividimos pelo tamanho da árvore pai 
            // para que a escala local compense o tamanho gigante dela e fique no tamanho correto!
            if (transform.parent != null)
            {
                float parentScale = transform.parent.localScale.x;
                // Reduz proporcionalmente à escala da árvore e aplica pela metade (0.5f) conforme você quis
                transform.localScale = Vector3.one * ((scale / parentScale) * 2f);
            }
            else
            {
                transform.localScale = Vector3.one * scale;
            }
        }

        if (rend != null && instantiatedMaterial != null)
        {
            if (maturationProgress < 1.0f && immatureMaterial != null)
            {
                Color cImmature = immatureMaterial.HasProperty("_Color") ? immatureMaterial.color : Color.green;
                Color cMature = matureMaterial.HasProperty("_Color") ? matureMaterial.color : Color.red;
                instantiatedMaterial.color = Color.Lerp(cImmature, cMature, maturationProgress);
            }
            else if (maturationProgress >= 1.0f && rottenMaterial != null)
            {
                Color cMature = matureMaterial.HasProperty("_Color") ? matureMaterial.color : Color.red;
                Color cRotten = rottenMaterial.HasProperty("_Color") ? rottenMaterial.color : new Color(0.35f, 0.2f, 0.1f);
                InstantiatedManagerColor(cMature, cRotten, rotProgress);
            }
        }
    }

    void InstantiatedManagerColor(Color cMature, Color cRotten, float rotProgress)
    {
        instantiatedMaterial.color = Color.Lerp(cMature, cRotten, rotProgress);
    }
}