using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField, Min(0f)]
    float radius = 0.5f;

    [SerializeField, Min(0.01f)]
    float cursorFollowSpeed = 40f, cursorSnapDuration = 0.05f;

    [SerializeField, Min(0f)]
    float fireCooldown = 0.1f, fireSpreadAngle = 5f;

    [SerializeField]
    BulletManager bulletManager;

    Vector2 previousPosition, velocity;
    float directionAngle, previousDirectionAngle;
    Vector2 fireOffset;
    float cooldown;

    public float Radius => radius;
    public Vector2 Position { get; private set; }
    public Vector2 TargetPosition { private get; set; }
    public bool FreeAim { get; set; }

    public void Initialize()
    {
        gameObject.SetActive(false);
    }

    public void Dispose() { }

    public void StartNewGame(Vector2 position)
    {
        Position = TargetPosition = previousPosition = position;
        directionAngle = previousDirectionAngle = 0f;
        fireOffset = new Vector2(0f, radius);
        velocity = Vector2.zero;
        cooldown = 0f;
        gameObject.SetActive(true);
    }

    public void UpdateState(float dt)
    {
        previousPosition = Position;
        previousDirectionAngle = directionAngle;

        Vector2 targetVector = TargetPosition - Position;
        float squareTargetDistance = targetVector.sqrMagnitude;

        if (squareTargetDistance > 0.0001f)
        {
            Position = Vector2.SmoothDamp(
                Position, TargetPosition, ref velocity,
                cursorSnapDuration, cursorFollowSpeed, dt
            );

            if (FreeAim)
            {
                fireOffset = targetVector * (radius / Mathf.Sqrt(squareTargetDistance));
                directionAngle = Mathf.Atan2(targetVector.x, targetVector.y) * -Mathf.Rad2Deg;
            }
        }

        if (bulletManager == null) return;

        cooldown -= dt;
        if (cooldown <= 0f)
        {
            cooldown += fireCooldown;
            float fireAngle = directionAngle + 180f +
                              UnityEngine.Random.Range(-fireSpreadAngle, fireSpreadAngle);
            bulletManager.Add(Position - fireOffset, fireAngle);
        }
    }

    public void UpdateVisualization(float dtInterpolator)
    {
        transform.SetLocalPositionAndRotation(
            Vector2.LerpUnclamped(previousPosition, Position, dtInterpolator),
            Quaternion.Euler(0f, 0f,
                Mathf.LerpAngle(previousDirectionAngle, directionAngle, dtInterpolator))
        );
    }
}