using Unity.Jobs;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador principal del juego. Maneja el loop de física fija,
/// inicializa todos los sistemas y coordina su actualización.
/// Usa el New Input System de Unity para leer mouse y teclado.
/// </summary>
public class Game : MonoBehaviour
{
    [SerializeField, Min(0.001f)]
    float fixedDeltaTime = 0.01f;

    [SerializeField, Min(0f)]
    float worldBoundsRadius = 1.24f;

    [SerializeField]
    Player player;

    [SerializeField]
    BulletManager bulletManager;

    float dt;
    Area2D worldArea, playerArea;
    BallManager ballManager;
    Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;

        // Configurar cámara para vista 2D en plano XY
        mainCamera.transform.SetPositionAndRotation(
            new Vector3(0f, 0f, -20f), Quaternion.identity);
        mainCamera.nearClipPlane = 16f;
        mainCamera.farClipPlane  = 24f;

        // Configurar luz
        Light mainLight = FindFirstObjectByType<Light>();
        if (mainLight != null)
            mainLight.transform.rotation = Quaternion.Euler(50f, 45f, 0f);

        worldArea = Area2D.FromView(mainCamera);

        // Área donde el jugador puede moverse (sin los bordes)
        playerArea.extents = worldArea.extents -
            new Unity.Mathematics.float2(worldBoundsRadius + player.Radius);

        CreateWorldBounds();

        // Inicializar managers
        ballManager = GetComponent<BallManager>();
        ballManager.Initialize(worldArea);
        bulletManager.Initialize(worldArea);
        player.Initialize();

        // Ocultar cursor del sistema
        Cursor.visible = false;
        player.StartNewGame(Vector2.zero);
    }

    void OnDisable()
    {
        ballManager.Dispose();
        bulletManager.Dispose();
        player.Dispose();
        Cursor.visible = true;
    }

    void Update()
    {
        // Leer posición del cursor con New Input System y convertir a mundo
        Vector2 targetPoint = GetTargetPoint();

        // FreeAim: apuntar libremente cuando NO se mantiene click izquierdo o espacio
        player.FreeAim        = !Mouse.current.leftButton.isPressed &&
                                 !Keyboard.current.spaceKey.isPressed;
        player.TargetPosition = targetPoint;

        // Loop de física fija
        dt += Time.deltaTime;
        while (dt >= fixedDeltaTime)
        {
            UpdateGameState(fixedDeltaTime);
            dt -= fixedDeltaTime;
        }

        // Actualizar visualizaciones con tiempo extrapolado para suavizar
        ballManager.UpdateVisualization(dt);
        bulletManager.UpdateVisualization(dt);
        player.UpdateVisualization(dt / fixedDeltaTime);
    }

    void UpdateGameState(float fixedDt)
    {
        player.UpdateState(fixedDt);

        // Actualizar balas y pelotas en paralelo (no dependen entre sí al moverse)
        JobHandle handle = JobHandle.CombineDependencies(
            ballManager.UpdateBalls(fixedDt),
            bulletManager.UpdateBullets(fixedDt)
        );

        ballManager.ResolveBalls(handle);
    }

    /// <summary>
    /// Convierte la posición del mouse a coordenadas del mundo 2D,
    /// limitada al área jugable (excluyendo los bordes).
    /// Usa el New Input System para leer Mouse.current.position.
    /// </summary>
    Vector2 GetTargetPoint()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        float t = -ray.origin.z / ray.direction.z;
        Vector2 p = ray.origin + ray.direction * t;

        p.x = Mathf.Clamp(p.x, -playerArea.extents.x, playerArea.extents.x);
        p.y = Mathf.Clamp(p.y, -playerArea.extents.y, playerArea.extents.y);
        return p;
    }

    void CreateWorldBounds()
    {
        Material mat = new(Shader.Find("Universal Render Pipeline/Lit"));
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

    static void SpawnBound(string name, Transform parent, Material mat,
                           Vector3 pos, Vector3 scale)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }
}