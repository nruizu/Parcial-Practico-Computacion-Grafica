using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using Random = UnityEngine.Random;

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

    [SerializeField, Range(0.01f, 1f)]
    float fragmentSeparation = 0.6f;

    [SerializeField, Min(0f)]
    float explosionStrength = 2f;

    NativeList<BallState> states;
    List<BallVisualization> visualizations;

    UpdateBallJob updateBallJob;
    BounceBallsJob bounceBallsJob;
    VerifySpawnJob verifySpawnJob;
    HitJob hitJob;

    float cooldown, cooldownDuration;
    Area2D worldArea;
    JobHandle lastHandle;

    public NativeList<BallState> States => states;

    public void Initialize(Area2D area)
    {
        worldArea = area;
        states = new NativeList<BallState>(100, Allocator.Persistent);
        visualizations = new List<BallVisualization>(100);
        cooldown = cooldownDuration = startingCooldown;

        updateBallJob = new UpdateBallJob
        {
            balls = states,
            worldArea = worldArea,
            maxSpeed = maxSpeed
        };

        bounceBallsJob = new BounceBallsJob
        {
            balls = states,
            worldArea = worldArea,
            bounceStrength = bounceStrength
        };

        verifySpawnJob = new VerifySpawnJob
        {
            balls = states,
            success = new NativeReference<bool>(Allocator.Persistent),
            worldArea = worldArea,
            radius = BallState.radii[BallState.initialStage],
            avoidRadius = avoidSpawnRadius
        };
    }

    public void SetupHitJob(ref HitJob job)
    {
        job.balls = states;
        job.fragmentSeparation = fragmentSeparation;
        job.explosionStrength = explosionStrength;
    }

    /// <summary>
    /// Completa todos los jobs pendientes. DEBE llamarse antes de Dispose().
    /// </summary>
    public void CompleteAllJobs()
    {
        lastHandle.Complete();
    }

    /// <summary>
    /// Guarda el handle del job más reciente (HitJob) para completarlo en OnDisable.
    /// </summary>
    public void SetLastJobHandle(JobHandle handle)
    {
        lastHandle = handle;
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

    public JobHandle UpdateBalls(float dt)
    {
        cooldown -= dt;
        bounceBallsJob.dt = updateBallJob.dt = dt;
        lastHandle = updateBallJob.Schedule(default);
        return lastHandle;
    }

    public JobHandle ScheduleHitJob(HitJob job, JobHandle dependency)
    {
        hitJob = job;
        return hitJob.Schedule(dependency);
    }

    public void ResolveBalls(Vector2 avoidPosition, JobHandle dependency)
    {
        lastHandle = dependency;
        dependency = bounceBallsJob.Schedule(dependency);

        if (cooldown <= 0f)
        {
            verifySpawnJob.position = worldArea.RandomVector2;
            verifySpawnJob.avoidPosition = avoidPosition;
            dependency = verifySpawnJob.Schedule(dependency);
        }

        dependency.Complete();
        lastHandle = dependency;

        if (cooldown <= 0f && verifySpawnJob.success.Value)
        {
            cooldown += cooldownDuration;
            cooldownDuration *= cooldownPersistence;
            states.Add(new BallState
            {
                position = verifySpawnJob.position,
                velocity = UnityEngine.Random.insideUnitCircle * maxStartSpeed,
                mass = BallState.masses[BallState.initialStage],
                radius = 0f,
                targetRadius = BallState.radii[BallState.initialStage],
                stage = BallState.initialStage,
                type = UnityEngine.Random.Range(0, ballPrefabs.Length),
                alive = true
            });
        }
    }

    public void UpdateVisualization(float dtExtrapolated)
    {
        for (int i = visualizations.Count; i < states.Length; i++)
            visualizations.Add(ballPrefabs[states[i].type].Spawn());

        for (int i = 0; i < visualizations.Count; i++)
        {
            BallState s = states[i];
            if (s.alive)
            {
                visualizations[i].UpdateVisualization(
                    s.position + s.velocity * dtExtrapolated,
                    Mathf.Min(s.radius + dtExtrapolated, s.targetRadius));
            }
            else
            {
                int last = states.Length - 1;
                states[i] = states[last];
                states.Length -= 1;
                visualizations[i].Despawn();
                visualizations[i] = visualizations[last];
                visualizations.RemoveAt(last);
                i--;
            }
        }
    }
}
