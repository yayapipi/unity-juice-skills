using UnityEngine;

/// <summary>
/// 眼睛：替任何物件加上一對會看東西、會眨眼的眼睛，方塊馬上變成一個角色。
///     JuiceEyes eyes = JuiceEyes.Attach(transform);   // 加上眼睛（已經有就直接拿回來）
///     eyes.LookAt(ball);                              // 一直看著某個物件，也可以給座標
///     eyes.Squint();                                  // 瞇眼：被打到、用力
///     eyes.Widen();                                   // 睜大：驚訝、害怕
///     eyes.Blink();                                   // 眨眼。平常會自己眨
///     eyes.Visible = false;                           // 先藏起來
/// 眼睛用程式畫，不需要任何圖。物件被壓扁、拉長時，眼睛會保持圓形。給 2D 或正對鏡頭的物件用。
/// </summary>
[AddComponentMenu("Juice/Juice Eyes")]
[DisallowMultipleComponent]
public class JuiceEyes : MonoBehaviour
{
    [Tooltip("一顆眼睛的直徑（世界單位）。")]
    public float size = 0.28f;

    [Tooltip("兩顆眼睛中心的距離（世界單位）。")]
    public float spacing = 0.42f;

    [Tooltip("眼睛相對於物件中心的位置（世界單位）。")]
    public Vector2 offset = new Vector2(0f, 0.04f);

    public Color eyeColor = Color.white;
    public Color pupilColor = new Color(0.13f, 0.11f, 0.22f);

    [Tooltip("加上一張嘴：平常微笑，睜大眼睛的時候張開。")]
    public bool mouth;

    [Tooltip("嘴巴在眼睛下方幾個眼睛直徑。0 = 跟眼睛同高，夾在兩眼中間（扁的物件用）。")]
    public float mouthDrop = 0.92f;

    [Tooltip("每隔幾秒自己眨眼。")]
    public bool autoBlink = true;

    [Tooltip("眼睛一直看著的物件。留空 = 看正前方，或用 LookAt 指定座標。")]
    public Transform lookTarget;

    Transform root;
    Transform leftEye, rightEye, leftPupil, rightPupil, smile, round;
    Vector3 lookPoint;
    bool hasLookPoint;
    Vector2 look, lookVelocity;
    float open = 1f, openVelocity;
    float wide = 1f, wideVelocity;
    float blinkTimer = 1f;
    float blinkPhase = -1f;
    float squintUntil = -1f;
    float widenUntil = -1f;
    bool visible = true;

    static Sprite smileSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        smileSprite = null;
    }

    /// <summary>替 owner 加上眼睛。size：眼睛直徑；spacing：兩眼距離；mouth：要不要嘴巴。</summary>
    public static JuiceEyes Attach(Transform owner, float size = 0.28f, float spacing = 0.42f, bool mouth = false)
    {
        if (owner == null || !Application.isPlaying) return null;
        JuiceEyes eyes = owner.GetComponent<JuiceEyes>();
        if (eyes == null) eyes = owner.gameObject.AddComponent<JuiceEyes>();
        eyes.size = size;
        eyes.spacing = spacing;
        eyes.mouth = mouth;
        eyes.Build();
        return eyes;
    }

    /// <summary>顯示或隱藏眼睛。</summary>
    public bool Visible
    {
        get { return visible; }
        set
        {
            visible = value;
            if (root != null) root.gameObject.SetActive(value && isActiveAndEnabled);
        }
    }

    public void LookAt(Transform target)
    {
        lookTarget = target;
        hasLookPoint = false;
    }

    public void LookAt(Vector3 worldPoint)
    {
        lookTarget = null;
        lookPoint = worldPoint;
        hasLookPoint = true;
    }

    /// <summary>看回正前方。</summary>
    public void LookForward()
    {
        lookTarget = null;
        hasLookPoint = false;
    }

    public void Blink()
    {
        blinkPhase = 0f;
    }

    /// <summary>瞇眼 seconds 秒。</summary>
    public void Squint(float seconds = 0.3f)
    {
        squintUntil = Time.time + seconds;
    }

    /// <summary>睜大眼睛 seconds 秒。</summary>
    public void Widen(float seconds = 0.4f)
    {
        widenUntil = Time.time + seconds;
    }

    void Start()
    {
        if (root == null) Build();
    }

    void OnEnable()
    {
        if (root != null) root.gameObject.SetActive(visible);
    }

    void OnDisable()
    {
        if (root != null) root.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (root != null) Destroy(root.gameObject);
    }

    void Build()
    {
        if (root != null) Destroy(root.gameObject);

        Renderer body = GetComponent<Renderer>();
        int layer = body != null ? body.sortingLayerID : 0;
        int order = body != null ? body.sortingOrder : 0;

        root = new GameObject("Juice Eyes").transform;
        root.SetParent(transform, false);
        leftEye = Part("Eye L", root, JuiceAssets.CircleSprite, eyeColor, layer, order + 1);
        rightEye = Part("Eye R", root, JuiceAssets.CircleSprite, eyeColor, layer, order + 1);
        leftPupil = Part("Pupil", leftEye, JuiceAssets.CircleSprite, pupilColor, layer, order + 2);
        rightPupil = Part("Pupil", rightEye, JuiceAssets.CircleSprite, pupilColor, layer, order + 2);
        smile = null;
        round = null;
        if (mouth)
        {
            smile = Part("Smile", root, SmileSprite, pupilColor, layer, order + 1);
            round = Part("Mouth", root, JuiceAssets.CircleSprite, pupilColor, layer, order + 1);
        }
        blinkTimer = Random.Range(0.5f, 3.5f);
        root.gameObject.SetActive(visible && isActiveAndEnabled);
        Layout(0f);
    }

    static Transform Part(string partName, Transform parent, Sprite sprite, Color color, int layer, int order)
    {
        GameObject go = new GameObject(partName);
        go.transform.SetParent(parent, false);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerID = layer;
        renderer.sortingOrder = order;
        return go.transform;
    }

    void LateUpdate()
    {
        if (root == null) return;
        Layout(Mathf.Min(Time.deltaTime, 0.05f));
    }

    void Layout(float dt)
    {
        // 抵銷父物件的縮放：身體被壓扁、拉長的時候，眼睛還是圓的
        Vector3 scale = transform.lossyScale;
        float sx = Mathf.Max(0.0001f, Mathf.Abs(scale.x));
        float sy = Mathf.Max(0.0001f, Mathf.Abs(scale.y));
        root.localScale = new Vector3(1f / sx, 1f / sy, 1f);
        root.localPosition = new Vector3(offset.x / sx, offset.y / sy, 0f);

        // 視線
        Vector2 want = Vector2.zero;
        if (lookTarget != null || hasLookPoint)
        {
            Vector3 point = lookTarget != null ? lookTarget.position : lookPoint;
            Vector3 local = root.InverseTransformPoint(point);
            Vector2 flat = new Vector2(local.x, local.y);
            float distance = flat.magnitude;
            if (distance > 0.001f) want = flat / distance * Mathf.Clamp01(distance / 1.5f);
        }
        look.x = JuiceSpring.Step(look.x, ref lookVelocity.x, want.x, 5f, 0.55f, dt);
        look.y = JuiceSpring.Step(look.y, ref lookVelocity.y, want.y, 5f, 0.55f, dt);

        // 眨眼
        if (autoBlink && dt > 0f)
        {
            blinkTimer -= dt;
            if (blinkTimer <= 0f)
            {
                blinkPhase = 0f;
                blinkTimer = Random.Range(1.6f, 4.5f);
            }
        }
        float lid = 1f;
        if (blinkPhase >= 0f)
        {
            blinkPhase += dt / 0.13f;
            if (blinkPhase >= 1f) blinkPhase = -1f;
            else lid = 1f - Mathf.Sin(blinkPhase * Mathf.PI) * 0.92f;
        }

        // 瞇眼與睜大，用彈簧過渡
        float now = Time.time;
        open = JuiceSpring.Step(open, ref openVelocity, now < squintUntil ? 0.3f : 1f, 7f, 0.5f, dt);
        wide = JuiceSpring.Step(wide, ref wideVelocity, now < widenUntil ? 1.35f : 1f, 7f, 0.45f, dt);

        float width = size * Mathf.Max(0.2f, wide);
        float height = width * Mathf.Clamp(open * lid, 0.06f, 1.5f);
        float pupil = wide > 1.12f ? 0.4f : 0.52f;
        Place(leftEye, leftPupil, -spacing * 0.5f, width, height, pupil);
        Place(rightEye, rightPupil, spacing * 0.5f, width, height, pupil);

        if (smile != null && round != null)
        {
            bool surprised = wide > 1.12f;
            smile.gameObject.SetActive(!surprised);
            round.gameObject.SetActive(surprised);
            smile.localPosition = new Vector3(0f, -size * mouthDrop, 0f);
            smile.localScale = new Vector3(size * 1.25f, size * 1.25f, 1f);
            round.localPosition = new Vector3(0f, -size * (mouthDrop + 0.03f), 0f);
            float o = size * 0.5f * Mathf.Max(0.2f, wide);
            round.localScale = new Vector3(o * 0.85f, o, 1f);
        }
    }

    void Place(Transform eye, Transform pupil, float x, float width, float height, float pupilSize)
    {
        eye.localPosition = new Vector3(x, 0f, 0f);
        eye.localScale = new Vector3(width, height, 1f);
        pupil.localPosition = new Vector3(look.x * 0.22f, look.y * 0.22f, 0f);
        pupil.localScale = new Vector3(pupilSize, pupilSize, 1f);
    }

    // 一條往上彎的弧線，1 單位寬
    static Sprite SmileSprite
    {
        get
        {
            if (smileSprite != null) return smileSprite;
            const int w = 64, h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = "Juice Smile";
            tex.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[w * h];
            const float cx = 31.5f, cy = 34f, radius = 26f, thickness = 5.5f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float d = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - radius);
                    float a = Mathf.Clamp01(thickness * 0.5f - d + 0.5f);
                    // 只留下方的弧，兩端淡出
                    a *= Mathf.Clamp01((cy - 12f - y) / 4f);
                    pixels[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            smileSprite = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), w);
            smileSprite.name = "Juice Smile";
            return smileSprite;
        }
    }
}
