using UnityEngine;

/// <summary>
/// Visualización de una bala. Usa material property blocks para
/// cambiar la opacidad según el tiempo de vida restante de forma eficiente.
/// </summary>
public class BulletVisualization : MonoBehaviour
{
    static readonly int LifeFactorID = Shader.PropertyToID("_LifeFactor");
    static MaterialPropertyBlock materialPropertyBlock;

    PrefabInstancePool<BulletVisualization> pool;
    MeshRenderer meshRenderer;

    void Awake()
    {
        materialPropertyBlock ??= new MaterialPropertyBlock();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public BulletVisualization Spawn(Quaternion rotation)
    {
        BulletVisualization instance = pool.GetInstance(this);
        instance.pool = pool;
        instance.transform.localRotation = rotation;
        return instance;
    }

    public void Despawn() => pool.Recycle(this);

    /// <summary>
    /// Actualiza posición y factor de vida (para efecto de desvanecimiento).
    /// </summary>
    public void UpdateVisualization(Vector2 position, float lifeFactor)
    {
        transform.localPosition = new Vector3(position.x, position.y, 0f);

        materialPropertyBlock.SetFloat(LifeFactorID, lifeFactor);
        meshRenderer.SetPropertyBlock(materialPropertyBlock);
    }
}
