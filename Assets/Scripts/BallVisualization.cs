using Unity.Mathematics;
using UnityEngine;

using Random = UnityEngine.Random;

/// <summary>
/// Componente visual de una pelota. Maneja su pool de instancias,
/// rotación visual aleatoria y sincronización con el estado físico.
/// </summary>
public class BallVisualization : MonoBehaviour
{
    [SerializeField, Min(0f)]
    float minSpinSpeed = 20f, maxSpinSpeed = 60f;

    PrefabInstancePool<BallVisualization> pool;

    Vector3 rotationAxis;
    float radius, rotationSpeed, rotationAngle;

    public BallVisualization Spawn()
    {
        BallVisualization instance = pool.GetInstance(this);
        instance.pool = pool;
        instance.radius = -1f;

        // Asignar rotación aleatoria a los hijos para variedad de forma
        for (int i = 0; i < instance.transform.childCount; i++)
            instance.transform.GetChild(i).localRotation = Random.rotation;

        // Configurar spin visual aleatorio
        instance.rotationAxis  = Random.onUnitSphere;
        instance.rotationSpeed = Random.Range(minSpinSpeed, maxSpinSpeed);
        instance.rotationAngle = Random.Range(0f, 360f);

        return instance;
    }

    public void Despawn() => pool.Recycle(this);

    /// <summary>
    /// Actualiza posición y escala de la visualización con tiempo extrapolado.
    /// </summary>
    public void UpdateVisualization(float2 position, float targetRadius)
    {
        // Girar visualmente (puramente estético, no afecta física)
        rotationAngle = (rotationAngle + rotationSpeed * Time.deltaTime) % 360f;

        transform.SetLocalPositionAndRotation(
            new Vector3(position.x, position.y, 0f),
            Quaternion.AngleAxis(rotationAngle, rotationAxis)
        );

        // Solo actualizar escala cuando cambia el radio (evita operaciones innecesarias)
        if (radius != targetRadius)
        {
            radius = targetRadius;
            transform.localScale = Vector3.one * (2f * targetRadius);
        }
    }
}
