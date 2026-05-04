using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Mathematics.math;

/// <summary>
/// Job que verifica si una posición de spawn está libre de colisiones
/// con pelotas existentes y con una posición a evitar (ej: el jugador).
/// </summary>
[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
public struct VerifySpawnJob : IJob
{
    [ReadOnly]
    public NativeList<BallState> balls;

    [WriteOnly]
    public NativeReference<bool> success;

    public Area2D worldArea;

    public float2 position;      // Posición candidata de spawn
    public float2 avoidPosition; // Posición a evitar (jugador)
    public float radius;         // Radio de la pelota a spawnear
    public float avoidRadius;    // Radio de evitación alrededor de avoidPosition

    public void Execute()
    {
        // Verificar distancia mínima respecto a la posición a evitar (jugador)
        float2 pAvoid = worldArea.Wrap(position - avoidPosition);
        float rAvoid  = avoidRadius + radius;
        if (dot(pAvoid, pAvoid) <= rAvoid * rAvoid)
        {
            success.Value = false;
            return;
        }

        // Verificar que no solape ninguna pelota existente
        for (int i = 0; i < balls.Length; i++)
        {
            BallState ball = balls[i];
            if (!ball.alive) continue;

            float2 p = worldArea.Wrap(position - ball.position);
            float r  = radius + ball.radius;
            if (dot(p, p) <= r * r)
            {
                success.Value = false;
                return;
            }
        }

        success.Value = true;
    }
}
