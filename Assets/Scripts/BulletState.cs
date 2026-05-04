using Unity.Mathematics;

/// <summary>
/// Estado físico de una bala. Almacenado en NativeList para acceso desde jobs.
/// </summary>
public struct BulletState
{
    public float2 position, velocity;

    public float timeRemaining;

    public bool exploded;

    // Una bala está viva mientras le quede tiempo
    public bool Alive => timeRemaining > 0f;

    public void Explode()
    {
        exploded = true;
        timeRemaining = 0f;
    }
}
