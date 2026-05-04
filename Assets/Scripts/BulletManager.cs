using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// Gestiona el estado físico y visualización de todas las balas activas.
/// Las balas son creadas por el Player y destruidas aquí al expirar.
/// </summary>
public class BulletManager : MonoBehaviour
{
    [SerializeField]
    BulletVisualization bulletPrefab;

    [SerializeField, Min(0f)]
    float speed = 12f, startLifetime = 1f;

    [HideInInspector]
    public float radius = 0.15f;

    NativeList<BulletState> states;
    List<BulletVisualization> visualizations;
    UpdateBulletJob updateBulletJob;

    public NativeList<BulletState> States => states;
    public float StartLifetime => startLifetime;

    public void Initialize(Area2D worldArea)
    {
        states = new NativeList<BulletState>(100, Allocator.Persistent);
        visualizations = new List<BulletVisualization>(100);
        updateBulletJob = new UpdateBulletJob
        {
            bullets   = states,
            worldArea = worldArea
        };
    }

    public void Dispose()
    {
        if (!states.IsCreated) return;
        foreach (var v in visualizations) v.Despawn();
        visualizations.Clear();
        states.Dispose();
    }

    public void StartNewGame()
    {
        foreach (var v in visualizations) v.Despawn();
        visualizations.Clear();
        states.Clear();
    }

    public JobHandle UpdateBullets(float dt)
    {
        updateBulletJob.dt = dt;
        return updateBulletJob.Schedule(states.Length, default);
    }

    /// <summary>
    /// Agrega una nueva bala con posición y ángulo de disparo.
    /// </summary>
    public void Add(Vector2 position, float angle)
    {
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        states.Add(new BulletState
        {
            position      = position,
            velocity      = (Vector2)(rotation * Vector3.up) * speed,
            timeRemaining = startLifetime
        });
        visualizations.Add(bulletPrefab.Spawn(rotation));
    }

    public void UpdateVisualization(float dtExtrapolated)
    {
        for (int i = 0; i < visualizations.Count; i++)
        {
            BulletState state = states[i];
            if (state.Alive)
            {
                // Extrapolación para movimiento suave entre pasos de física
                visualizations[i].UpdateVisualization(
                    state.position + state.velocity * dtExtrapolated,
                    Mathf.Max(0f, state.timeRemaining - dtExtrapolated) / startLifetime
                );
            }
            else
            {
                // Eliminar balas muertas intercambiando con la última
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
