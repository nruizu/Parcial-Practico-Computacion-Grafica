using Unity.Jobs;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [SerializeField]
    Shader impactFlashShader;

    float dt;
    Area2D worldArea, playerArea;
    BallManager ballManager;
    Camera mainCamera;
    HitJob hitJob;
    bool isPlaying;

    ImpactFlashController[] wallFlashControllers;

    void Awake()
    {
        mainCamera = Camera.main;
        mainCamera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -20f), Quaternion.identity);
        mainCamera.nearClipPlane = 16f;
        mainCamera.farClipPlane = 24f;

        Light mainLight = FindFirstObjectByType<Light>();
        if (mainLight != null)
            mainLight.transform.rotation = Quaternion.Euler(50f, 45f, 0f);

        worldArea = Area2D.FromView(mainCamera);
        playerArea.extents = worldArea.extents -
            new Unity.Mathematics.float2(worldBoundsRadius + player.Radius);

        CreateWorldBounds();

        ballManager = GetComponent<BallManager>();
        ballManager.Initialize(worldArea);
        bulletManager.Initialize(worldArea);
        player.Initialize();

        hitJob = new HitJob
        {
            worldArea = worldArea,
            bulletRadius = bulletManager.radius,
            playerRadius = player.Radius
        };
        hitJob.health = player.health;
        hitJob.score = player.score;
        ballManager.SetupHitJob(ref hitJob);
        hitJob.bullets = bulletManager.States;

        Cursor.visible = false;
    }

    void OnDisable()
    {
        ballManager.CompleteAllJobs();
        ballManager.Dispose();
        bulletManager.Dispose();
        player.Dispose();
        Cursor.visible = true;
    }

    void StartNewGame()
    {
        isPlaying = true;
        ballManager.StartNewGame();
        bulletManager.StartNewGame();
        player.StartNewGame(GetTargetPoint());
    }

    void Update()
    {
        if (isPlaying)
        {
            player.FreeAim = !Mouse.current.leftButton.isPressed &&
                             !Keyboard.current.spaceKey.isPressed;
            player.TargetPosition = GetTargetPoint();
        }
        else if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartNewGame();
        }

        dt += Time.deltaTime;
        while (dt >= fixedDeltaTime)
        {
            UpdateGameState(fixedDeltaTime);
            dt -= fixedDeltaTime;
        }

        ballManager.UpdateVisualization(dt);
        bulletManager.UpdateVisualization(dt);

        if (isPlaying && player.UpdateVisualization(dt / fixedDeltaTime))
            isPlaying = false;
    }

    void UpdateGameState(float fixedDt)
    {
        if (isPlaying)
        {
            player.UpdateState(fixedDt);
            hitJob.playerPosition = player.Position;
        }

        JobHandle handle = JobHandle.CombineDependencies(
            ballManager.UpdateBalls(fixedDt),
            bulletManager.UpdateBullets(fixedDt));

        handle = hitJob.Schedule(handle);
        ballManager.SetLastJobHandle(handle);
        
        // ResolveBalls completa el job, LUEGO es seguro leer health
        ballManager.ResolveBalls(isPlaying ? player.Position : Vector2.zero, handle);
        
        // Ahora el job está completado y es seguro leer los valores
        if (isPlaying && hitJob.health.Value <= 0)
            TriggerWallFlash();
    }

    public void TriggerWallFlash()
    {
        if (wallFlashControllers == null) return;
        foreach (var ctrl in wallFlashControllers)
            ctrl.TriggerFlash();
    }

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
        Shader shaderToUse = impactFlashShader != null
            ? impactFlashShader
            : Shader.Find("Universal Render Pipeline/Lit");

        Material mat = new(shaderToUse);

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", new Color(16f / 255f, 0f, 0f, 1f));
        else
            mat.color = new Color(16f / 255f, 0f, 0f, 1f);

        Transform parent = new GameObject("WorldBounds").transform;
        parent.SetParent(transform);

        float ex = worldArea.extents.x;
        float ey = worldArea.extents.y;
        float r  = worldBoundsRadius;

        wallFlashControllers = new ImpactFlashController[4];
        wallFlashControllers[0] = SpawnBound("Top",    parent, mat, new Vector3(0f,  ey, 0f), new Vector3(ex, r, r) * 2f);
        wallFlashControllers[1] = SpawnBound("Bottom", parent, mat, new Vector3(0f, -ey, 0f), new Vector3(ex, r, r) * 2f);
        wallFlashControllers[2] = SpawnBound("Left",   parent, mat, new Vector3(-ex, 0f, 0f), new Vector3(r, ey, r) * 2f);
        wallFlashControllers[3] = SpawnBound("Right",  parent, mat, new Vector3( ex, 0f, 0f), new Vector3(r, ey, r) * 2f);
    }

    static ImpactFlashController SpawnBound(
        string name, Transform parent, Material mat, Vector3 pos, Vector3 scale)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go.AddComponent<ImpactFlashController>();
    }
}
