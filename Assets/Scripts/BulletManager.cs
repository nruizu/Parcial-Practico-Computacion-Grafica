using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class BulletManager : MonoBehaviour
{
    [SerializeField]
    BulletVisualization bulletPrefab;

    [SerializeField]
    ParticleSystem explosionParticleSystem;

    [SerializeField, Min(0)]
    int explosionParticleCount = 50;

    [SerializeField, Min(0f)]
    float speed = 12f, startLifetime = 1f;

    public float radius = 0.15f;

    NativeList<BulletState> states;
    List<BulletVisualization> visualizations;
    List<int> pendingRemoval;
    UpdateBulletJob updateBulletJob;
    Area2D worldArea;

    public NativeList<BulletState> States => states;
    public float StartLifetime => startLifetime;

    public void Initialize(Area2D area)
    {
        worldArea = area;
        states = new NativeList<BulletState>(100, Allocator.Persistent);
        visualizations = new List<BulletVisualization>(100);
        pendingRemoval = new List<int>();
        updateBulletJob = new UpdateBulletJob
        {
            bullets = states,
            worldArea = worldArea
        };
    }

    public void Dispose()
    {
        if (!states.IsCreated) return;

        // Verificar que el objeto no está siendo destruido antes de parar corrutinas
        if (this != null && gameObject != null)
            StopAllCoroutines();

        foreach (var v in visualizations) v.Despawn();
        visualizations.Clear();
        states.Dispose();
    }

    public void StartNewGame()
    {
        StopAllCoroutines();
        foreach (var v in visualizations) v.Despawn();
        visualizations.Clear();
        states.Clear();
        pendingRemoval.Clear();
    }

    public JobHandle UpdateBullets(float dt)
    {
        updateBulletJob.dt = dt;
        return updateBulletJob.Schedule(states.Length, default);
    }

    public void Add(Vector2 position, float angle)
    {
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        int index = states.Length;
        states.Add(new BulletState
        {
            position = position,
            velocity = (Vector2)(rotation * Vector3.up) * speed,
            timeRemaining = startLifetime
        });
        BulletVisualization vis = bulletPrefab.Spawn(rotation);
        visualizations.Add(vis);

        StartCoroutine(BulletLifetimeCoroutine(index));
    }

    IEnumerator BulletLifetimeCoroutine(int index)
    {
        yield return new WaitForSeconds(startLifetime);

        if (index < states.Length)
        {
            BulletState s = states[index];
            if (s.Alive)
            {
                s.timeRemaining = 0f;
                states[index] = s;
            }
        }
    }

    public void UpdateVisualization(float dtExtrapolated)
    {
        for (int i = 0; i < visualizations.Count; i++)
        {
            BulletState state = states[i];
            if (state.Alive)
            {
                visualizations[i].UpdateVisualization(
                    state.position + state.velocity * dtExtrapolated,
                    Mathf.Max(0f, state.timeRemaining - dtExtrapolated) / startLifetime);
            }
            else
            {
                if (state.exploded && explosionParticleSystem != null)
                {
                    explosionParticleSystem.Emit(
                        new ParticleSystem.EmitParams
                        {
                            position = new Vector3(state.position.x, state.position.y, 0f),
                            applyShapeToPosition = true
                        },
                        explosionParticleCount
                    );
                }

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
