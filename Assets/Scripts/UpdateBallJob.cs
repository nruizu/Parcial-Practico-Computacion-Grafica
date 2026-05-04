using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Mathematics.math;

/// <summary>
/// Job que actualiza cada pelota: aplica movimiento lineal, envuelve la posición
/// en el mundo toroidal y crece el radio hasta el objetivo.
/// </summary>
[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
public struct UpdateBallJob : IJob
{
    public NativeList<BallState> balls;
    public Area2D worldArea;
    public float dt, maxSpeed;

    public void Execute()
    {
        float maxSpeedSq = maxSpeed * maxSpeed;

        for (int i = 0; i < balls.Length; i++)
        {
            BallState ball = balls[i];
            if (!ball.alive) continue;

            // Limitar velocidad máxima
            if (dot(ball.velocity, ball.velocity) > maxSpeedSq)
                ball.velocity = normalize(ball.velocity) * maxSpeed;

            // Mover y envolver en el mundo
            ball.position = worldArea.Wrap(ball.position + ball.velocity * dt);

            // Crecer el radio hasta el objetivo
            ball.radius = min(ball.targetRadius, ball.radius + dt);

            balls[i] = ball;
        }
    }
}
