using Unity.Jobs;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField, Min(0.001f)]
    float fixedDeltaTime = 0.01f;

    [SerializeField, Min(0f)]
    float worldBoundsRadius = 1.24f;

    [SerializeField]
    BallManager ballManager;

    float dt;
    Area2D worldArea;

    void Awake()
    {
        // Configurar cámara para vista 2D en plano XY
        Camera cam = Camera.main;
        cam.transform.SetPositionAndRotation(new Vector3(0f, 0f, -20f), Quaternion.identity);
        cam.nearClipPlane = 16f;
        cam.farClipPlane = 24f;

        // Configurar luz direccional
        Light mainLight = FindFirstObjectByType<Light>();
        if (mainLight != null)
            mainLight.transform.rotation = Quaternion.Euler(50f, 45f, 0f);

        // Calcular área del mundo según la cámara
        worldArea = Area2D.FromView(cam);

        // Crear bordes visuales del mundo
        CreateWorldBounds();

        // Inicializar el manager de pelotas
        ballManager.Initialize(worldArea);
    }

    void OnDisable()
    {
        ballManager.Dispose();
    }

    void Update()
    {
        // Loop de física fija: acumular tiempo y ejecutar pasos fijos
        dt += Time.deltaTime;
        while (dt >= fixedDeltaTime)
        {
            UpdateGameState(fixedDeltaTime);
            dt -= fixedDeltaTime;
        }

        // Actualizar visualización con tiempo extrapolado para suavizar movimiento
        ballManager.UpdateVisualization(dt);
    }

    void UpdateGameState(float fixedDt)
    {
        JobHandle handle = ballManager.UpdateBalls(fixedDt);
        ballManager.ResolveBalls(handle);
    }

    void CreateWorldBounds()
    {
        Material mat = new(Shader.Find("Universal Render Pipeline/Lit"));
        // Rojo muy oscuro como advertencia visual de borde
        mat.color = new Color(16f / 255f, 0f, 0f, 1f);

        Transform parent = new GameObject("WorldBounds").transform;
        parent.SetParent(transform);

        float ex = worldArea.extents.x;
        float ey = worldArea.extents.y;
        float r  = worldBoundsRadius;

        SpawnBound("Top",    parent, mat, new Vector3(0f,  ey, 0f), new Vector3(ex, r, r) * 2f);
        SpawnBound("Bottom", parent, mat, new Vector3(0f, -ey, 0f), new Vector3(ex, r, r) * 2f);
        SpawnBound("Left",   parent, mat, new Vector3(-ex, 0f, 0f), new Vector3(r, ey, r) * 2f);
        SpawnBound("Right",  parent, mat, new Vector3( ex, 0f, 0f), new Vector3(r, ey, r) * 2f);
    }

    static void SpawnBound(string name, Transform parent, Material mat, Vector3 pos, Vector3 scale)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }
}