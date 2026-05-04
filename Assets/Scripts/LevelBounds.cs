using UnityEngine;

public class LevelBounds : MonoBehaviour
{
    [SerializeField]
    Camera mainCamera;

    [SerializeField, Min(0f)]
    float boundsRadius = 1.24f;

    [SerializeField]
    Color boundsColor = new Color(16f / 255f, 0f, 0f, 1f);

    [SerializeField]
    bool showDebugGizmos = true;

    // Indica visualmente los límites del nivel en la vista de escena
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || mainCamera == null) return;

        Gizmos.color = Color.red;
        Area2D area = Area2D.FromView(mainCamera);

        Vector3 topLeft     = new(-area.extents.x, area.extents.y, 0f);
        Vector3 topRight    = new( area.extents.x, area.extents.y, 0f);
        Vector3 bottomLeft  = new(-area.extents.x,-area.extents.y, 0f);
        Vector3 bottomRight = new( area.extents.x,-area.extents.y, 0f);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
}
