using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// Job que actualiza cada bala: reduce su tiempo de vida y aplica movimiento lineal
/// con wrapping en el mundo toroidal.
/// </summary>
[BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
public struct UpdateBulletJob : IJobFor
{
    public NativeList<BulletState> bullets;
    public Area2D worldArea;
    public float dt;

    public void Execute(int i)
    {
        BulletState bullet = bullets[i];
        bullet.timeRemaining -= dt;
        bullet.position = worldArea.Wrap(bullet.position + bullet.velocity * dt);
        bullets[i] = bullet;
    }
}
