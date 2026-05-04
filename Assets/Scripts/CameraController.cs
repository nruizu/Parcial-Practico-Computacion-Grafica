using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    Player player;

    [SerializeField, Min(0f)]
    float followStrength = 0.1f;

    [SerializeField, Min(0f)]
    float smoothTime = 0.3f;

    [SerializeField, Min(0f)]
    float maxOffset = 3f;

    Vector3 basePosition;
    Vector3 velocity;

    void Awake()
    {
        basePosition = transform.position;
    }

    void LateUpdate()
    {
        if (player == null || !player.gameObject.activeSelf)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position, basePosition, ref velocity, smoothTime);
            return;
        }

        // La cámara sigue levemente al jugador dentro de un offset máximo
        Vector3 target = basePosition + new Vector3(
            Mathf.Clamp(player.Position.x * followStrength, -maxOffset, maxOffset),
            Mathf.Clamp(player.Position.y * followStrength, -maxOffset, maxOffset),
            0f);

        transform.position = Vector3.SmoothDamp(
            transform.position, target, ref velocity, smoothTime);
    }
}
