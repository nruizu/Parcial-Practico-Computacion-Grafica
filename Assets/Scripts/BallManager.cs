using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using Random = UnityEngine.Random;

/// <summary>
/// Gestiona el estado físico y las visualizaciones de todas las pelotas.
/// Coordina los jobs de movimiento, rebote y verificación de spawn.
/// </summary>
public class BallManager : MonoBehaviour
{
    [SerializeField]
    BallVisualization[] ballPrefabs;

    [SerializeField, Min(0f)]
    float startingCooldown = 4f;

    [SerializeField, Range(0.1f, 1f)]
    float cooldownPersistence = 0.96f;

    [SerializeField, Min(0f)]
    float maxSpeed = 12.5f, maxStartSpeed = 4f;

    [SerializeField, Min(0f)]
    float bounceStrength = 100f;

    [SerializeField, Min(0f)]
    float avoidSpawnRadius = 2f;

    // Estado nativo de las pelotas (accesible por jobs de Burst)
    NativeList<BallState> states;

    // Visualizaciones correspondientes a cada estado
    List<BallVisualization> visualizations;

    UpdateBallJob   updateBallJob;
    BounceBallsJob  bounceBallsJob;
    VerifySpawnJob  verifySpawnJob;

    float cooldown, cooldownDuration;
    Area2D worldArea;

    public void Initialize(Area2D area)
    {
        worldArea = area;
        states        = new NativeList<BallState>(100, Allocator.Persistent);
        visualizations = new List<BallVisualization>(100);

        cooldown = cooldownDuration = startingCooldown;

        // Configurar jobs con referencias a los datos compartidos
        updateBallJob = new UpdateBallJob
        {
            balls     = states,
            worldArea = worldArea,
            maxSpeed  = maxSpeed
        };

        bounceBallsJob = new BounceBallsJob
        {
            balls          = states,
            worldArea      = worldArea,
            bounceStrength = bounceStrength
        };

        verifySpawnJob = new VerifySpawnJob
        {
            balls       = states,
            success     = new NativeReference<bool>(Allocator.Persistent),
            worldArea   = worldArea,
            radius      = BallState.radii[BallState.initialStage],
            avoidRadius = avoidSpawnRadius
        };
    }

    public void Dispose()
    {
        if (!states.IsCreated) return;

        foreach (var v in visualizations) v.Despawn();
        visualizations.Clear();
        states.Dispose();
        verifySpawnJob.success.Dispose();
    }

    public void StartNewGame()
    {
        foreach (var v in visualizations) v.Despawn();
        visualizations.Clear();
        states.Clear();
        cooldown = cooldownDuration = startingCooldown;
    }

    /// <summary>
    /// Schedula el job de movimiento lineal. Retorna el handle para encadenar dependencias.
    /// </summary>
    public JobHandle UpdateBalls(float dt)
    {
        cooldown -= dt;
        bounceBallsJob.dt = updateBallJob.dt = dt;
        return updateBallJob.Schedule(default);
    }

    /// <summary>
    /// Completa los jobs pendientes, verifica colisiones de rebote y spawna pelotas si es necesario.
    /// </summary>
    public void ResolveBalls(JobHandle dependency)
    {
        // Primero resolver rebotes entre pelotas
        dependency = bounceBallsJob.Schedule(dependency);

        // Si toca spawnear, verificar posición válida en paralelo
        if (cooldown <= 0f)
        {
            verifySpawnJob.position      = worldArea.RandomVector2;
            verifySpawnJob.avoidPosition = float2.zero; // Sin jugador aún
            dependency = verifySpawnJob.Schedule(dependency);
        }

        dependency.Complete();

        // Spawnear si hay posición válida
        if (cooldown <= 0f && verifySpawnJob.success.Value)
        {
            cooldown += cooldownDuration;
            cooldownDuration *= cooldownPersistence;

            states.Add(new BallState
            {
                position     = verifySpawnJob.position,
                velocity     = Random.insideUnitCircle * maxStartSpeed,
                mass         = BallState.masses[BallState.initialStage],
                radius       = 0f,
                targetRadius = BallState.radii[BallState.initialStage],
                stage        = BallState.initialStage,
                type         = Random.Range(0, ballPrefabs.Length),
                alive        = true
            });
        }
    }

    /// <summary>
    /// Sincroniza las visualizaciones con el estado físico, usando extrapolación para suavizar.
    /// </summary>
    public void UpdateVisualization(float dtExtrapolated)
    {
        // Agregar visualizaciones para pelotas nuevas
        for (int i = visualizations.Count; i < states.Length; i++)
        {
            visualizations.Add(ballPrefabs[states[i].type].Spawn());
        }

        // Actualizar posición y escala de cada visualización
        for (int i = 0; i < visualizations.Count; i++)
        {
            BallState s = states[i];
            visualizations[i].UpdateVisualization(
                s.position + s.velocity * dtExtrapolated,
                Mathf.Min(s.radius + dtExtrapolated, s.targetRadius)
            );
        }
    }

    // Propiedad pública para que otros sistemas accedan al estado de las pelotas
    public NativeList<BallState> States => states;
}