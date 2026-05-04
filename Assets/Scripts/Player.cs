using Unity.Collections;
using UnityEngine;
using TMPro;

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

    [SerializeField, Min(1)]
    int maxHealth = 10;

    [SerializeField]
    TextMeshProUGUI healthText;

    [SerializeField]
    TextMeshProUGUI scoreText;

    Vector2 previousPosition, velocity;
    float directionAngle, previousDirectionAngle;
    Vector2 fireOffset;
    float cooldown;
    int lastCheckedHealth, lastCheckedScore;

    public float Radius => radius;
    public Vector2 Position { get; private set; }
    public Vector2 TargetPosition { private get; set; }
    public bool FreeAim { get; set; }

    public NativeReference<int> health;
    public NativeReference<int> score;

    public void Initialize()
    {
        health = new NativeReference<int>(Allocator.Persistent);
        score = new NativeReference<int>(Allocator.Persistent);
        gameObject.SetActive(false);
    }

    public void Dispose()
    {
        if (health.IsCreated) health.Dispose();
        if (score.IsCreated) score.Dispose();
    }

    public void StartNewGame(Vector2 position)
    {
        Position = TargetPosition = previousPosition = position;
        directionAngle = previousDirectionAngle = 0f;
        fireOffset = new Vector2(0f, radius);
        velocity = Vector2.zero;
        cooldown = 0f;
        health.Value = lastCheckedHealth = maxHealth;
        score.Value = lastCheckedScore = 0;
        UpdateHealthUI();
        UpdateScoreUI();
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
                cursorSnapDuration, cursorFollowSpeed, dt);

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

    public bool UpdateVisualization(float dtInterpolator)
    {
        transform.SetLocalPositionAndRotation(
            Vector2.LerpUnclamped(previousPosition, Position, dtInterpolator),
            Quaternion.Euler(0f, 0f,
                Mathf.LerpAngle(previousDirectionAngle, directionAngle, dtInterpolator)));

        if (lastCheckedScore != score.Value)
        {
            lastCheckedScore = score.Value;
            UpdateScoreUI();
        }

        if (lastCheckedHealth == health.Value) return false;

        lastCheckedHealth = health.Value;
        UpdateHealthUI();

        bool isDestroyed = lastCheckedHealth <= 0;
        if (isDestroyed) gameObject.SetActive(false);
        return isDestroyed;
    }

    void UpdateHealthUI()
    {
        if (healthText != null)
            healthText.text = "HP: " + health.Value;
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score.Value;
    }
}
