using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 鏡頭震動（創傷值模型）、後座與縮放衝擊。
/// 一行就能用，會自動掛到主鏡頭上：
///     JuiceShake.Add(0.4f);                 // 震動，0–1，可累加
///     JuiceShake.Kick(Vector2.left, 0.3f);  // 往某個方向頓一下再彈回
///     JuiceShake.Zoom(0.05f);               // 推近 5% 再彈回
/// 位移只在「畫這一格」時套用，畫完就還原，所以不會跟鏡頭跟隨或 Cinemachine 打架。
/// 用的是 unscaled time，頓幀（Time.timeScale = 0）時照樣會震。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Juice/Juice Shake (Camera)")]
public class JuiceShake : MonoBehaviour
{
    [Tooltip("目前的創傷值 0–1。實際震動量 = trauma 的平方，所以小事幾乎不震、大事才明顯。")]
    [Range(0f, 1f)] public float trauma;

    [Tooltip("每秒減少多少創傷值。越大停得越快。")]
    public float decay = 1.4f;

    [Tooltip("晃動頻率。越大越急促。")]
    public float frequency = 22f;

    [Tooltip("trauma = 1 時的最大位移（世界單位）。2D 建議 0.3–0.8；3D 透視鏡頭會自動縮小位移、改以旋轉為主。")]
    public float maxOffset = 0.5f;

    [Tooltip("trauma = 1 時的最大旋轉角度（度）。")]
    public float maxAngle = 3f;

    static JuiceShake main;

    Camera cam;
    float seed;
    Vector2 kick;
    float zoomAmount;
    float zoomTime = 1f;
    float zoomDuration = 1f;

    bool applied;
    Vector3 savedPosition;
    Quaternion savedRotation;
    float savedLens;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        main = null;
    }

    /// <summary>主鏡頭上的 JuiceShake；沒有就自動加一個。場景裡沒有鏡頭時回傳 null。</summary>
    public static JuiceShake Main
    {
        get
        {
            if (!Application.isPlaying) return null;
            if (main != null && main.isActiveAndEnabled) return main;
            Camera c = Camera.main;
            if (c == null && Camera.allCamerasCount > 0) c = Camera.allCameras[0];
            if (c == null) return null;
            main = c.GetComponent<JuiceShake>();
            if (main == null) main = c.gameObject.AddComponent<JuiceShake>();
            return main;
        }
    }

    /// <summary>加上創傷值。輕擊 0.2、一般命中 0.3–0.4、爆炸或受傷 0.5–0.7。</summary>
    public static void Add(float amount = 0.3f)
    {
        JuiceShake s = Main;
        if (s != null) s.AddTrauma(amount);
    }

    /// <summary>把鏡頭往 direction 推 strength（世界單位），再快速彈回。開槍的後座就是往射擊的反方向推。</summary>
    public static void Kick(Vector2 direction, float strength = 0.3f)
    {
        JuiceShake s = Main;
        if (s != null) s.AddKick(direction, strength);
    }

    /// <summary>瞬間推近 amount（0.05 = 5%），在 duration 秒內彈回。重擊、衝刺、過關時用。</summary>
    public static void Zoom(float amount = 0.05f, float duration = 0.25f)
    {
        JuiceShake s = Main;
        if (s != null) s.PunchZoom(amount, duration);
    }

    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    public void AddKick(Vector2 direction, float strength)
    {
        if (direction.sqrMagnitude < 0.000001f) return;
        kick += direction.normalized * strength;
    }

    public void PunchZoom(float amount, float duration)
    {
        zoomAmount = amount;
        zoomDuration = Mathf.Max(0.01f, duration);
        zoomTime = 0f;
    }

    void Awake()
    {
        cam = GetComponent<Camera>();
        seed = Random.value * 100f;
    }

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        Camera.onPreCull += OnCameraPreCull;
        Camera.onPostRender += OnCameraPostRender;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        Camera.onPreCull -= OnCameraPreCull;
        Camera.onPostRender -= OnCameraPostRender;
        Restore();
    }

    void Update()
    {
        if (applied) Restore();
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        trauma = Mathf.Max(0f, trauma - decay * dt);
        kick = Vector2.Lerp(kick, Vector2.zero, 1f - Mathf.Exp(-14f * dt));
        if (zoomTime < zoomDuration) zoomTime += dt;
    }

    // URP / HDRP
    void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == cam) Apply();
    }

    void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == cam) Restore();
    }

    // 內建管線
    void OnCameraPreCull(Camera camera)
    {
        if (camera == cam) Apply();
    }

    void OnCameraPostRender(Camera camera)
    {
        if (camera == cam) Restore();
    }

    void Apply()
    {
        if (applied) Restore();
        if (cam == null) return;

        float intensity = JuiceSettings.ScreenShake ? JuiceSettings.Intensity : 0f;
        if (intensity <= 0f) return;

        float shake = trauma * trauma * intensity;
        Vector2 k = kick * intensity;
        float zoom = 0f;
        if (zoomTime < zoomDuration)
        {
            zoom = zoomAmount * (1f - JuiceEase.Evaluate(JuiceEaseType.OutCubic, zoomTime / zoomDuration)) * intensity;
        }
        if (shake < 0.0001f && k.sqrMagnitude < 0.000001f && Mathf.Abs(zoom) < 0.0001f) return;

        Transform t = transform;
        savedPosition = t.localPosition;
        savedRotation = t.localRotation;
        savedLens = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;

        float time = Time.unscaledTime * frequency;
        float nx = Mathf.PerlinNoise(seed, time) * 2f - 1f;
        float ny = Mathf.PerlinNoise(seed + 17.3f, time) * 2f - 1f;
        float nr = Mathf.PerlinNoise(seed + 41.7f, time) * 2f - 1f;

        if (cam.orthographic)
        {
            // 2D：位移為主，加一點點旋轉
            t.position += t.right * (nx * maxOffset * shake + k.x) + t.up * (ny * maxOffset * shake + k.y);
            t.localRotation = savedRotation * Quaternion.Euler(0f, 0f, nr * maxAngle * shake);
        }
        else
        {
            // 3D：旋轉為主，位移很小
            float move = maxOffset * 0.25f * shake;
            t.position += t.right * (nx * move + k.x) + t.up * (ny * move + k.y);
            t.localRotation = savedRotation * Quaternion.Euler(ny * maxAngle * shake, nx * maxAngle * shake, nr * maxAngle * 0.5f * shake);
        }

        if (Mathf.Abs(zoom) >= 0.0001f)
        {
            if (cam.orthographic) cam.orthographicSize = savedLens * (1f - zoom);
            else cam.fieldOfView = savedLens * (1f - zoom);
        }
        applied = true;
    }

    void Restore()
    {
        if (!applied) return;
        applied = false;
        transform.localPosition = savedPosition;
        transform.localRotation = savedRotation;
        if (cam != null)
        {
            if (cam.orthographic) cam.orthographicSize = savedLens;
            else cam.fieldOfView = savedLens;
        }
    }
}
