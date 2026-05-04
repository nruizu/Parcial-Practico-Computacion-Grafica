using UnityEngine;

/// <summary>
/// Controla el destello visual del shader ImpactFlash en las paredes del nivel.
/// Cuando se llama a TriggerFlash(), el parámetro _FlashProgress sube a 1
/// y luego decae automáticamente hacia 0 para crear el efecto de destello.
/// </summary>
public class ImpactFlashController : MonoBehaviour
{
    [SerializeField, Min(0f)]
    float flashDecaySpeed = 3f; // Velocidad a la que desaparece el destello

    static readonly int FlashProgressID = Shader.PropertyToID("_FlashProgress");

    MaterialPropertyBlock propertyBlock;
    MeshRenderer meshRenderer;
    float flashProgress;

    void Awake()
    {
        meshRenderer   = GetComponent<MeshRenderer>();
        propertyBlock  = new MaterialPropertyBlock();
        flashProgress  = 0f;
    }

    void Update()
    {
        if (flashProgress <= 0f) return;

        // Hacer decaer el flash linealmente con el tiempo
        flashProgress = Mathf.Max(0f, flashProgress - flashDecaySpeed * Time.deltaTime);

        // Aplicar al shader usando MaterialPropertyBlock (eficiente, no crea instancias)
        propertyBlock.SetFloat(FlashProgressID, flashProgress);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    /// <summary>
    /// Llamar desde Game.cs o HitJob al detectar una colisión con esta pared.
    /// </summary>
    public void TriggerFlash()
    {
        flashProgress = 1f;
    }
}
