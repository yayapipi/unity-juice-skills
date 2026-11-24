using UnityEngine;

/// <summary>
/// Juice Kit 的完整展示：一個打磚塊小遊戲。左邊的開關可以一項一項關掉，看看少了什麼。
/// 用法：選單 Tools ▸ Juice ▸ Create Showcase Scene，或把這個元件掛在空物件上，按 Play。
/// 移動滑鼠控制板子（滑鼠不動的時候它會自己玩），點一下多一顆球。
/// 數字鍵 1–9、0、- 切換開關，空白鍵全開或全關。
/// 畫面與聲音全部用程式產生，不需要任何素材。每一種回饋都只是一行 Juice Kit 的呼叫，
/// 可以直接搜尋 OnBrick、OnPaddle、OnWall 這幾個函式來看。
/// </summary>
[AddComponentMenu("Juice/Juice Showcase")]
public class JuiceShowcase : MonoBehaviour
{
    [Header("開關（執行時也可以在畫面左邊切換，或按數字鍵）")]
    public bool spring = true;
    public bool squash = true;
    public bool eyes = true;
    public bool trail = true;
    public bool particles = true;
    public bool flash = true;
    public bool shake = true;
    public bool hitStop = true;
    public bool text = true;
    public bool sound = true;
    public bool music = true;

    [Tooltip("打勾後板子永遠自己玩，不理會滑鼠。")]
    public bool autoPlay;

    // 場地
    const float Left = -4.4f, Right = 8f, Top = 4.5f, LoseY = -5.7f;
    const float CenterX = (Left + Right) * 0.5f;
    const int Cols = 10, Rows = 5;
    const float PitchX = 1.2f, PitchY = 0.6f, BrickW = 1.08f, BrickH = 0.46f, TopRowY = 3.45f;
    const float GridLeft = CenterX - Cols * PitchX * 0.5f, GridTop = TopRowY + PitchY * 0.5f;
    const float PaddleY = -3.9f, PaddleW = 2.3f, PaddleH = 0.46f;
    const float BallRadius = 0.17f, BaseSpeed = 8f, MaxSpeed = 11.5f;
    const int MaxBalls = 4;

    static readonly Color[] RowColors =
    {
        new Color(1f, 0.42f, 0.56f), new Color(1f, 0.62f, 0.32f), new Color(1f, 0.85f, 0.36f),
        new Color(0.45f, 0.88f, 0.6f), new Color(0.38f, 0.72f, 1f)
    };
    static readonly Color WallColor = new Color(0.36f, 0.32f, 0.62f);
    static readonly Color WallSpark = new Color(0.62f, 0.56f, 1f);
    static readonly Color PaddleColor = new Color(0.94f, 0.92f, 1f);
    static readonly Color Accent = new Color(1f, 0.74f, 0.3f);
    static readonly Color SwitchOn = new Color(0.96f, 0.45f, 0.06f);
    static readonly int[] ComboScale = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21, 24 };
    static readonly string[] Names =
    {
        "Spring 彈簧", "Squash 擠壓拉伸", "Eyes 眼睛", "Trail 拖尾", "Particles 粒子", "Flash 閃白",
        "Shake 鏡頭震動", "Hit Stop 頓幀", "Text 飄字", "Sound 音效", "Music 音樂"
    };
    static readonly string[] Keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-" };

    class Brick
    {
        public GameObject go;
        public Transform t;
        public SpriteRenderer sr;
        public JuiceEyes eyes;
        public Vector3 rest;
        public Color color;
        public int row, col, hp;
        public bool alive;
    }

    class Ball
    {
        public Transform root, stretch, art;
        public SpriteRenderer sr, glow;
        public TrailRenderer trail;
        public Vector2 pos, dir;
        public float speed, launchAt;
        public bool alive, stuck;
    }

    class Piece
    {
        public Transform t;
        public SpriteRenderer sr;
        public Vector2 v;
        public float spin, life, maxLife;
        public Color color;
    }

    Camera cam;
    Sprite brickSprite, pillSprite, ringSprite, glowSprite;
    Vector3 brickScale;
    Brick[] bricks;
    Ball[] balls;
    Piece[] pieces;
    Piece[] rings;
    Transform[] stars;
    float[] starSize;
    SpriteRenderer pulseRenderer;
    Transform wallLeft, wallRight, wallTop;
    Transform paddleRoot, paddleStretch, paddleArt, focus;
    JuiceEyes paddleEyes;
    float paddleX = CenterX, paddleVelocity, paddleTilt, paddleTiltVelocity;

    int alive, combo, score, destroyed, wave;
    float waveReadyAt, nextWaveAt = -1f, respawnAt = -1f;
    float pulse, comboPop = 1f, comboPopVelocity;
    Color pulseColor = Color.white;
    bool lastEyes, lastTrail, lastMusic, applied;

    Vector2 lastMouseGui;
    Vector3 mouseScreen;
    bool seenMouse;
    float lastMouseMoveTime = -100f;
    Rect panel;
    GUIStyle titleStyle, rowStyle, hudStyle, comboStyle, hintStyle, buttonStyle;

    /// <summary>目前的分數。</summary>
    public int Score { get { return score; } }
    /// <summary>場上還剩幾塊磚。</summary>
    public int BricksAlive { get { return alive; } }
    /// <summary>開始到現在一共打掉幾塊磚。</summary>
    public int BricksDestroyed { get { return destroyed; } }
    /// <summary>現在是第幾波磚塊。</summary>
    public int Wave { get { return wave; } }
    /// <summary>場上有幾顆球。</summary>
    public int BallsAlive
    {
        get
        {
            int n = 0;
            if (balls != null) for (int i = 0; i < balls.Length; i++) if (balls[i].alive) n++;
            return n;
        }
    }
    /// <summary>板子現在是不是自己在玩。</summary>
    public bool IsAuto
    {
        get { return autoPlay || !seenMouse || Time.unscaledTime - lastMouseMoveTime > 3f; }
    }

    // ------------------------------------------------------------------ 建立場景

    void Start()
    {
        cam = Camera.main;
        if (cam == null)
        {
            GameObject camObject = new GameObject("Main Camera");
            camObject.tag = "MainCamera";
            cam = camObject.AddComponent<Camera>();
            camObject.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.transform.rotation = Quaternion.identity;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.06f, 0.17f);
        FitCamera();

        brickSprite = Rounded(96, 41, 11f);
        pillSprite = Rounded(128, 26, 13f);
        ringSprite = Ring(64);
        glowSprite = Glow(64);
        brickScale = new Vector3(BrickW, BrickH / (41f / 96f), 1f);

        BuildBackground();
        Block("Field", new Vector3(CenterX, -0.4f, 0f), new Vector2(Right - Left, 10.2f), new Color(0.03f, 0.02f, 0.1f, 0.45f), -22);
        wallLeft = Block("Wall L", new Vector3(Left - 0.1f, -0.4f, 0f), new Vector2(0.2f, 10.2f), WallColor, -5);
        wallRight = Block("Wall R", new Vector3(Right + 0.1f, -0.4f, 0f), new Vector2(0.2f, 10.2f), WallColor, -5);
        wallTop = Block("Wall T", new Vector3(CenterX, Top + 0.1f, 0f), new Vector2(Right - Left + 0.4f, 0.2f), WallColor, -5);
        focus = new GameObject("Focus").transform;
        focus.SetParent(transform, false);
        BuildPaddle();
        BuildBricks();
        BuildBalls();
        BuildPools();
        JuiceMusic.Volume = 0.3f;      // 音樂要比音效小聲，打擊的聲音才聽得清楚
        StartWave();
        ApplyToggles();
    }

    void FitCamera()
    {
        cam.orthographicSize = Mathf.Max(5f, 8.9f / Mathf.Max(0.1f, cam.aspect));
    }

    Transform Block(string blockName, Vector3 position, Vector2 size, Color color, int order)
    {
        GameObject go = new GameObject(blockName);
        go.transform.SetParent(transform, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = JuiceAssets.SquareSprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return go.transform;
    }

    void BuildBackground()
    {
        Texture2D tex = new Texture2D(4, 64, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color top = new Color(0.17f, 0.12f, 0.38f), bottom = new Color(0.06f, 0.05f, 0.16f);
        for (int y = 0; y < 64; y++)
        {
            Color c = Color.Lerp(bottom, top, y / 63f);
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply(false, true);
        GameObject sky = new GameObject("Sky");
        sky.transform.SetParent(transform, false);
        sky.transform.localScale = new Vector3(40f, 1.4f, 1f);
        SpriteRenderer skyRenderer = sky.AddComponent<SpriteRenderer>();
        skyRenderer.sprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 64f), new Vector2(0.5f, 0.5f), 4f);
        skyRenderer.sortingOrder = -30;

        stars = new Transform[40];
        starSize = new float[stars.Length];
        for (int i = 0; i < stars.Length; i++)
        {
            GameObject star = new GameObject("Star");
            star.transform.SetParent(transform, false);
            star.transform.position = new Vector3(Random.Range(-9f, 9f), Random.Range(-5.5f, 5.5f), 0f);
            starSize[i] = Random.Range(0.04f, 0.13f);
            SpriteRenderer r = star.AddComponent<SpriteRenderer>();
            r.sprite = JuiceAssets.CircleSprite;
            r.color = new Color(0.8f, 0.8f, 1f, Random.Range(0.15f, 0.55f));
            r.sortingOrder = -25;
            stars[i] = star.transform;
        }

        pulseRenderer = Block("Pulse", new Vector3(CenterX, -0.4f, 0f), new Vector2(Right - Left, 10.2f), Color.clear, -20)
            .GetComponent<SpriteRenderer>();
    }

    void BuildPaddle()
    {
        paddleRoot = new GameObject("Paddle").transform;
        paddleRoot.SetParent(transform, false);
        paddleRoot.position = new Vector3(paddleX, PaddleY, 0f);
        paddleStretch = new GameObject("Stretch").transform;
        paddleStretch.SetParent(paddleRoot, false);
        GameObject art = new GameObject("Art");
        paddleArt = art.transform;
        paddleArt.SetParent(paddleStretch, false);
        paddleArt.localScale = new Vector3(PaddleW, PaddleH / (26f / 128f), 1f);
        SpriteRenderer renderer = art.AddComponent<SpriteRenderer>();
        renderer.sprite = pillSprite;
        renderer.color = PaddleColor;
        renderer.sortingOrder = 50;

        // 一行就有眼睛：兩眼分開一點，嘴巴夾在中間
        paddleEyes = JuiceEyes.Attach(paddleArt, 0.27f, 0.92f, true);
        paddleEyes.mouthDrop = 0.12f;
        paddleEyes.LookAt(focus);
    }

    void BuildBricks()
    {
        bricks = new Brick[Rows * Cols];
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                Brick b = new Brick();
                b.row = row;
                b.col = col;
                b.color = RowColors[row % RowColors.Length];
                b.rest = new Vector3(GridLeft + (col + 0.5f) * PitchX, TopRowY - row * PitchY, 0f);
                b.go = new GameObject("Brick " + row + "-" + col);
                b.t = b.go.transform;
                b.t.SetParent(transform, false);
                b.t.position = b.rest;
                b.t.localScale = brickScale;
                b.sr = b.go.AddComponent<SpriteRenderer>();
                b.sr.sprite = brickSprite;
                b.sr.color = b.color;
                b.sr.sortingOrder = 0;
                b.eyes = JuiceEyes.Attach(b.t, 0.2f, 0.38f);
                b.eyes.LookAt(focus);
                bricks[row * Cols + col] = b;
            }
        }
    }

    void BuildBalls()
    {
        balls = new Ball[MaxBalls];
        for (int i = 0; i < MaxBalls; i++)
        {
            Ball b = new Ball();
            b.root = new GameObject("Ball " + (i + 1)).transform;
            b.root.SetParent(transform, false);
            b.stretch = new GameObject("Stretch").transform;
            b.stretch.SetParent(b.root, false);
            GameObject art = new GameObject("Art");
            b.art = art.transform;
            b.art.SetParent(b.stretch, false);
            b.art.localScale = new Vector3(BallRadius * 2f, BallRadius * 2f, 1f);
            b.sr = art.AddComponent<SpriteRenderer>();
            b.sr.sprite = JuiceAssets.CircleSprite;
            b.sr.sortingOrder = 105;
            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(b.root, false);
            glow.transform.localScale = new Vector3(BallRadius * 7f, BallRadius * 7f, 1f);
            b.glow = glow.AddComponent<SpriteRenderer>();
            b.glow.sprite = glowSprite;
            b.glow.sortingOrder = 98;
            b.trail = JuiceTrail.Attach(b.root.gameObject, Color.white, 0.26f, 0.22f);
            if (b.trail != null) b.trail.emitting = false;
            b.root.gameObject.SetActive(false);
            balls[i] = b;
        }
    }

    void BuildPools()
    {
        pieces = new Piece[60];
        for (int i = 0; i < pieces.Length; i++) pieces[i] = NewPiece("Piece", brickSprite, 40);
        rings = new Piece[6];
        for (int i = 0; i < rings.Length; i++) rings[i] = NewPiece("Ring", ringSprite, 45);
    }

    Piece NewPiece(string pieceName, Sprite sprite, int order)
    {
        Piece p = new Piece();
        GameObject go = new GameObject(pieceName);
        p.t = go.transform;
        p.t.SetParent(transform, false);
        p.sr = go.AddComponent<SpriteRenderer>();
        p.sr.sprite = sprite;
        p.sr.sortingOrder = order;
        go.SetActive(false);
        return p;
    }

    // ------------------------------------------------------------------ 每一格

    void Update()
    {
        if (cam == null || bricks == null) return;
        FitCamera();
        ApplyToggles();

        float dt = Time.deltaTime;
        UpdatePaddle(dt);
        if (dt > 0f)
        {
            UpdateBalls(dt);
            if (nextWaveAt > 0f && Time.time >= nextWaveAt)
            {
                nextWaveAt = -1f;
                StartWave();
            }
        }
        UpdateEyes();
        UpdatePieces(dt);
        UpdateBackground(dt);
        comboPop = JuiceSpring.Step(comboPop, ref comboPopVelocity, 1f, 5f, 0.35f, Time.unscaledDeltaTime);
    }

    void OnDisable()
    {
        JuiceMusic.Stop(0.2f);
    }

    void ApplyToggles()
    {
        if (!applied || eyes != lastEyes)
        {
            lastEyes = eyes;
            paddleEyes.Visible = eyes;
            for (int i = 0; i < bricks.Length; i++) bricks[i].eyes.Visible = eyes;
        }
        if (!applied || trail != lastTrail)
        {
            lastTrail = trail;
            for (int i = 0; i < balls.Length; i++) SetTrail(balls[i]);
        }
        if (!applied || music != lastMusic)
        {
            lastMusic = music;
            if (music) JuiceMusic.Play();
            else JuiceMusic.Stop(0.3f);
        }
        applied = true;
    }

    void SetTrail(Ball b)
    {
        b.glow.enabled = trail;
        if (b.trail == null) return;
        bool on = trail && b.alive && !b.stuck;
        if (!on) b.trail.Clear();
        b.trail.emitting = on;
    }

    // ------------------------------------------------------------------ 板子

    void UpdatePaddle(float dt)
    {
        float min = Left + PaddleW * 0.5f, max = Right - PaddleW * 0.5f;
        float target;
        if (IsAuto)
        {
            target = AutoTarget();
        }
        else
        {
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 10f));
            target = world.x;
        }
        target = Mathf.Clamp(target, min, max);

        float before = paddleX;
        if (spring)
        {
            // 彈簧跟隨：有一點延遲，停下來的時候會稍微衝過頭
            paddleX = JuiceSpring.Step(paddleX, ref paddleVelocity, target, 5.5f, 0.55f, dt);
        }
        else
        {
            paddleX = Mathf.MoveTowards(paddleX, target, 26f * dt);
            paddleVelocity = dt > 0f ? (paddleX - before) / dt : 0f;
        }
        paddleX = Mathf.Clamp(paddleX, min, max);
        paddleRoot.position = new Vector3(paddleX, PaddleY, 0f);

        float tilt = spring ? Mathf.Clamp(-paddleVelocity * 1.4f, -14f, 14f) : 0f;
        paddleTilt = JuiceSpring.Step(paddleTilt, ref paddleTiltVelocity, tilt, 6f, 0.5f, dt);
        paddleRoot.rotation = Quaternion.Euler(0f, 0f, paddleTilt);

        float k = squash ? Mathf.Clamp(Mathf.Abs(paddleVelocity) * 0.012f, 0f, 0.22f) : 0f;
        paddleStretch.localScale = new Vector3(1f + k, 1f - k * 0.8f, 1f);
    }

    // 自己玩的時候：算出球會掉在哪裡，再故意偏一點，讓球每次往不同方向彈
    float AutoTarget()
    {
        Ball best = null;
        for (int i = 0; i < balls.Length; i++)
        {
            Ball b = balls[i];
            if (!b.alive || b.stuck || b.dir.y >= 0f) continue;
            if (best == null || b.pos.y < best.pos.y) best = b;
        }
        float wander = Mathf.Sin(Time.time * 1.3f) * 0.75f;
        if (best == null) return CenterX + Mathf.Sin(Time.time * 0.7f) * 2.5f;

        float time = (best.pos.y - (PaddleY + PaddleH * 0.5f)) / Mathf.Max(0.05f, -best.dir.y * best.speed);
        float x = best.pos.x + best.dir.x * best.speed * time;
        float lo = Left + BallRadius, hi = Right - BallRadius;
        for (int guard = 0; guard < 8 && (x < lo || x > hi); guard++)
        {
            if (x < lo) x = lo + (lo - x);
            if (x > hi) x = hi - (x - hi);
        }
        return x + wander;
    }

    // ------------------------------------------------------------------ 球

    void UpdateBalls(float dt)
    {
        int count = 0;
        Ball lowest = null;
        for (int i = 0; i < balls.Length; i++)
        {
            Ball b = balls[i];
            if (!b.alive) continue;
            if (b.stuck)
            {
                b.pos = new Vector2(paddleX, PaddleY + PaddleH * 0.5f + BallRadius + 0.02f);
                if (Time.time >= b.launchAt && Time.time >= waveReadyAt) Launch(b);
            }
            else
            {
                float remaining = b.speed * dt;
                for (int guard = 0; guard < 60 && remaining > 0f && b.alive; guard++)
                {
                    float step = Mathf.Min(remaining, 0.1f);
                    remaining -= step;
                    b.pos += b.dir * step;
                    Collide(b);
                }
            }
            if (!b.alive) continue;
            count++;
            if (lowest == null || b.pos.y < lowest.pos.y) lowest = b;

            b.root.position = new Vector3(b.pos.x, b.pos.y, 0f);
            bool moving = !b.stuck;
            b.stretch.rotation = moving ? Quaternion.Euler(0f, 0f, Mathf.Atan2(b.dir.y, b.dir.x) * Mathf.Rad2Deg) : Quaternion.identity;
            b.stretch.localScale = squash && moving ? new Vector3(1.24f, 0.84f, 1f) : Vector3.one;
        }

        // 所有的眼睛都看著最低的那顆球
        focus.position = lowest != null ? new Vector3(lowest.pos.x, lowest.pos.y, 0f) : new Vector3(paddleX, 2f, 0f);

        if (count == 0 && nextWaveAt < 0f)
        {
            if (respawnAt < 0f)
            {
                OnMiss();
                respawnAt = Time.time + 0.7f;
            }
            else if (Time.time >= respawnAt)
            {
                respawnAt = -1f;
                Spawn(true);
            }
        }
    }

    Ball Spawn(bool stuck)
    {
        for (int i = 0; i < balls.Length; i++)
        {
            Ball b = balls[i];
            if (b.alive) continue;
            b.alive = true;
            b.stuck = stuck;
            b.speed = BaseSpeed;
            b.launchAt = Time.time + 0.45f;
            b.pos = new Vector2(paddleX, PaddleY + PaddleH * 0.5f + BallRadius + 0.02f);
            b.dir = Vector2.up;
            b.sr.color = Color.white;
            b.glow.color = new Color(1f, 1f, 1f, 0.4f);
            b.root.position = new Vector3(b.pos.x, b.pos.y, 0f);
            b.root.gameObject.SetActive(true);
            if (b.trail != null)
            {
                b.trail.Clear();
                b.trail.startColor = Color.white;
                b.trail.endColor = new Color(1f, 1f, 1f, 0f);
            }
            SetTrail(b);
            if (squash) JuiceSquash.Punch(b.art, -0.7f);
            return b;
        }
        return null;
    }

    void Launch(Ball b)
    {
        float angle = Random.Range(-0.45f, 0.45f);
        b.dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
        b.stuck = false;
        SetTrail(b);
        if (sound) JuiceSfx.Blip(JuiceBlip.Jump, 0.4f);
        if (squash) JuiceSquash.Stretch(paddleArt, 0.25f);
        if (spring) JuiceSpring.Nudge(paddleStretch, Vector3.up * 0.18f);
    }

    void Kill(Ball b)
    {
        b.alive = false;
        b.stuck = false;
        if (b.trail != null)
        {
            b.trail.emitting = false;
            b.trail.Clear();
        }
        b.root.gameObject.SetActive(false);
    }

    /// <summary>從板子多發一顆球（最多四顆）。畫面上點一下就是呼叫這個。</summary>
    public void ExtraBall()
    {
        if (nextWaveAt > 0f || Time.time < waveReadyAt) return;
        Ball b = Spawn(false);
        if (b == null) return;
        Launch(b);
        if (shake) JuiceShake.Kick(Vector2.down, 0.15f);
    }

    void Collide(Ball b)
    {
        float r = BallRadius;
        if (b.pos.x < Left + r)
        {
            b.pos.x = Left + r;
            b.dir.x = Mathf.Abs(b.dir.x);
            OnWall(b, new Vector3(Left, b.pos.y, 0f), Vector3.right, wallLeft);
        }
        else if (b.pos.x > Right - r)
        {
            b.pos.x = Right - r;
            b.dir.x = -Mathf.Abs(b.dir.x);
            OnWall(b, new Vector3(Right, b.pos.y, 0f), Vector3.left, wallRight);
        }
        if (b.pos.y > Top - r)
        {
            b.pos.y = Top - r;
            b.dir.y = -Mathf.Abs(b.dir.y);
            OnWall(b, new Vector3(b.pos.x, Top, 0f), Vector3.down, wallTop);
        }
        if (b.pos.y < LoseY)
        {
            Kill(b);
            return;
        }

        float paddleTop = PaddleY + PaddleH * 0.5f;
        if (b.dir.y < 0f && b.pos.y - r < paddleTop && b.pos.y > PaddleY - PaddleH * 0.5f
            && Mathf.Abs(b.pos.x - paddleX) < PaddleW * 0.5f + r)
        {
            b.pos.y = paddleTop + r;
            float t = Mathf.Clamp((b.pos.x - paddleX) / (PaddleW * 0.5f), -1f, 1f);
            float angle = t * 58f * Mathf.Deg2Rad + Mathf.Clamp(paddleVelocity * 0.012f, -0.18f, 0.18f);
            b.dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            OnPaddle(b);
            return;
        }

        if (Time.time < waveReadyAt) return;
        int col = Mathf.FloorToInt((b.pos.x - GridLeft) / PitchX);
        int row = Mathf.FloorToInt((GridTop - b.pos.y) / PitchY);
        for (int rr = row - 1; rr <= row + 1; rr++)
        {
            for (int cc = col - 1; cc <= col + 1; cc++)
            {
                if (rr < 0 || rr >= Rows || cc < 0 || cc >= Cols) continue;
                Brick k = bricks[rr * Cols + cc];
                if (!k.alive) continue;
                float nx = Mathf.Clamp(b.pos.x, k.rest.x - BrickW * 0.5f, k.rest.x + BrickW * 0.5f);
                float ny = Mathf.Clamp(b.pos.y, k.rest.y - BrickH * 0.5f, k.rest.y + BrickH * 0.5f);
                float dx = b.pos.x - nx, dy = b.pos.y - ny;
                if (dx * dx + dy * dy >= r * r) continue;

                Vector2 incoming = b.dir;
                if (Mathf.Abs(dx) > Mathf.Abs(dy))
                {
                    b.dir.x = Mathf.Sign(dx) * Mathf.Abs(b.dir.x);
                    b.pos.x = nx + Mathf.Sign(dx) * r;
                }
                else if (dy != 0f)
                {
                    b.dir.y = Mathf.Sign(dy) * Mathf.Abs(b.dir.y);
                    b.pos.y = ny + Mathf.Sign(dy) * r;
                }
                else
                {
                    b.dir.y = -b.dir.y;
                }
                // 不要讓球幾乎水平地飛
                if (Mathf.Abs(b.dir.y) < 0.28f)
                {
                    b.dir.y = b.dir.y < 0f ? -0.28f : 0.28f;
                    b.dir.Normalize();
                }
                OnBrick(k, b, new Vector3(nx, ny, 0f), incoming);
                return;
            }
        }
    }

    // ------------------------------------------------------------------ 事件：Juice 都在這裡

    void OnWall(Ball b, Vector3 point, Vector3 normal, Transform wall)
    {
        if (sound) JuiceSfx.Blip(JuiceBlip.Click, 0.22f, 0.12f, 0.8f);
        if (squash) JuiceSquash.Punch(b.art, 0.3f);
        if (spring) JuiceSpring.Nudge(wall, -normal * 0.12f);
        if (particles) JuiceBurst.Spark(point, normal, WallSpark);
    }

    void OnPaddle(Ball b)
    {
        combo = 0;
        Vector3 point = new Vector3(b.pos.x, PaddleY + PaddleH * 0.5f, 0f);
        if (sound) JuiceSfx.Blip(JuiceBlip.Jump, 0.45f, 0.06f);
        if (squash)
        {
            JuiceSquash.Squash(paddleArt, 0.3f);
            JuiceSquash.Punch(b.art, 0.35f);
        }
        if (spring) JuiceSpring.Nudge(paddleStretch, Vector3.down * 0.22f);
        if (particles) JuiceBurst.Dust(point, new Color(1f, 1f, 1f, 0.8f));
        if (shake) JuiceShake.Kick(Vector2.down, 0.08f);
        if (eyes) paddleEyes.Blink();
    }

    void OnBrick(Brick k, Ball b, Vector3 point, Vector2 incoming)
    {
        k.hp--;
        b.speed = Mathf.Min(MaxSpeed, b.speed + 0.12f);

        if (k.hp > 0)
        {
            // 硬磚的第一下：沒有壞，但是要有反應
            k.sr.color = Color.Lerp(k.color, Color.white, 0.3f);
            if (sound) JuiceSfx.Blip(JuiceBlip.Hit, 0.5f);
            if (squash) JuiceSquash.Punch(k.t, 0.25f);
            if (spring) JuiceSpring.Wobble(k.t, incoming.x < 0f ? 16f : -16f);
            if (flash) JuiceFlash.Play(k.go);
            if (particles) JuiceBurst.Spark(point, -incoming, k.color);
            if (shake) JuiceShake.Add(0.12f);
            if (hitStop) JuiceTime.HitStop(0.03f);
            if (eyes) k.eyes.Squint(0.6f);
            return;
        }

        combo++;
        destroyed++;
        alive--;
        score += 10 * combo;
        comboPop = 1.7f;
        k.alive = false;
        k.go.SetActive(false);
        Color tint = Color.Lerp(Color.white, k.color, 0.55f);
        b.sr.color = tint;
        b.glow.color = new Color(tint.r, tint.g, tint.b, 0.45f);
        if (b.trail != null)
        {
            b.trail.startColor = tint;
            b.trail.endColor = new Color(tint.r, tint.g, tint.b, 0f);
        }

        // 連續打掉越多塊，音越高（五聲音階，怎麼按都好聽）
        if (sound)
        {
            int semitones = ComboScale[Mathf.Min(combo - 1, ComboScale.Length - 1)];
            JuiceSfx.Blip(JuiceBlip.Coin, 0.6f, 0f, Mathf.Pow(2f, semitones / 12f));
        }
        if (particles)
        {
            JuiceBurst.Explosion(k.rest, k.color, 0.6f);
            Shatter(k.rest, k.color, incoming);
            Shockwave(k.rest, k.color);
        }
        if (flash) Pulse(k.color, 0.1f + Mathf.Min(combo, 6) * 0.01f);
        if (shake)
        {
            JuiceShake.Add(0.16f + Mathf.Min(combo, 8) * 0.02f);
            JuiceShake.Kick(incoming, 0.1f);
        }
        if (hitStop) JuiceTime.HitStop(combo >= 5 ? 0.05f : 0.03f);
        if (text)
        {
            float size = 0.75f + Mathf.Min(combo, 10) * 0.05f;
            JuiceFloatingText.Spawn(k.rest + Vector3.up * 0.2f, "+" + (10 * combo), Color.Lerp(Color.white, k.color, 0.4f), size, 0.7f, 1f);
        }

        // 旁邊的磚塊被震開、嚇一跳
        for (int i = 0; i < bricks.Length; i++)
        {
            Brick o = bricks[i];
            if (!o.alive) continue;
            Vector3 d = o.rest - k.rest;
            float distance = d.magnitude;
            if (distance > 2.5f || distance < 0.01f) continue;
            if (spring) JuiceSpring.Nudge(o.t, d / distance * 0.3f * (1f - distance / 2.5f));
            if (flash && distance < 1.3f) JuiceFlash.Play(o.go, 0.05f);
            if (eyes && distance < 1.6f) o.eyes.Widen(0.5f);
        }

        if (alive <= 0) OnCleared();
    }

    void OnMiss()
    {
        combo = 0;
        if (sound) JuiceSfx.Blip(JuiceBlip.Lose, 0.6f);
        if (shake) JuiceShake.Add(0.45f);
        if (flash) Pulse(new Color(1f, 0.3f, 0.36f), 0.35f);
        if (spring) JuiceSpring.Wobble(paddleStretch, 18f);
        if (eyes) paddleEyes.Squint(1.2f);
        if (music) JuiceMusic.Duck(0.3f, 0.8f);
    }

    void OnCleared()
    {
        nextWaveAt = Time.time + 1.3f;
        for (int i = 0; i < balls.Length; i++) if (balls[i].alive) Kill(balls[i]);
        Vector3 center = new Vector3(CenterX, 1.6f, 0f);
        if (particles) JuiceBurst.Confetti(center, 60);
        if (sound) JuiceSfx.Blip(JuiceBlip.PowerUp, 0.7f);
        if (hitStop) JuiceTime.SlowMo(0.35f, 0.5f);
        if (shake) JuiceShake.Zoom(0.05f, 0.4f);
        if (text) JuiceFloatingText.Spawn(center, "CLEAR!", Accent, 1.8f, 1.2f, 0.8f);
        if (eyes) paddleEyes.Widen(1f);
    }

    void StartWave()
    {
        wave++;
        alive = 0;
        combo = 0;
        respawnAt = -1f;
        for (int i = 0; i < bricks.Length; i++)
        {
            Brick b = bricks[i];
            JuiceSpring existing = b.t.GetComponent<JuiceSpring>();
            if (existing != null) existing.Settle();
            b.alive = true;
            b.hp = b.row == 0 ? 2 : 1;
            b.sr.color = b.color;
            b.go.SetActive(true);
            b.t.position = b.rest;
            b.t.rotation = Quaternion.identity;
            b.t.localScale = brickScale;
            alive++;
            if (spring)
            {
                // 出場：一塊一塊從上面掉下來，落定的時候會晃
                JuiceSpring.DropIn(b.t, Vector3.up * 7.5f, b.col * 0.035f + (Rows - 1 - b.row) * 0.07f);
                JuiceSpring.Wobble(b.t, Random.Range(-25f, 25f));
            }
        }
        waveReadyAt = Time.time + (spring ? 1.2f : 0.3f);
        for (int i = 0; i < balls.Length; i++) if (balls[i].alive) Kill(balls[i]);
        Spawn(true);
    }

    // ------------------------------------------------------------------ 碎片、衝擊波、背景

    void Shatter(Vector3 position, Color color, Vector2 incoming)
    {
        int made = 0;
        for (int i = 0; i < pieces.Length && made < 5; i++)
        {
            Piece p = pieces[i];
            if (p.life > 0f) continue;
            made++;
            p.maxLife = p.life = Random.Range(0.6f, 0.95f);
            p.color = color;
            p.v = Random.insideUnitCircle * 4f + incoming * 2.5f + Vector2.up * 2.5f;
            p.spin = Random.Range(-720f, 720f);
            p.t.position = position + (Vector3)(Random.insideUnitCircle * 0.3f);
            p.t.localScale = new Vector3(Random.Range(0.28f, 0.42f), Random.Range(0.3f, 0.5f), 1f);
            p.t.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            p.sr.color = color;
            p.t.gameObject.SetActive(true);
        }
    }

    void Shockwave(Vector3 position, Color color)
    {
        for (int i = 0; i < rings.Length; i++)
        {
            Piece p = rings[i];
            if (p.life > 0f) continue;
            p.maxLife = p.life = 0.35f;
            p.color = Color.Lerp(Color.white, color, 0.5f);
            p.t.position = position;
            p.t.gameObject.SetActive(true);
            return;
        }
    }

    void UpdatePieces(float dt)
    {
        for (int i = 0; i < pieces.Length; i++)
        {
            Piece p = pieces[i];
            if (p.life <= 0f) continue;
            p.life -= dt;
            if (p.life <= 0f)
            {
                p.t.gameObject.SetActive(false);
                continue;
            }
            p.v.y -= 20f * dt;
            p.t.position += (Vector3)(p.v * dt);
            p.t.Rotate(0f, 0f, p.spin * dt);
            float k = p.life / p.maxLife;
            p.sr.color = new Color(p.color.r, p.color.g, p.color.b, Mathf.Clamp01(k * 2f));
        }
        for (int i = 0; i < rings.Length; i++)
        {
            Piece p = rings[i];
            if (p.life <= 0f) continue;
            p.life -= dt;
            if (p.life <= 0f)
            {
                p.t.gameObject.SetActive(false);
                continue;
            }
            float k = 1f - p.life / p.maxLife;
            float size = Mathf.Lerp(0.5f, 2.8f, JuiceEase.Evaluate(JuiceEaseType.OutCubic, k));
            p.t.localScale = new Vector3(size, size, 1f);
            p.sr.color = new Color(p.color.r, p.color.g, p.color.b, (1f - k) * 0.7f);
        }
    }

    void Pulse(Color color, float strength)
    {
        if (!JuiceSettings.Flashes) return;
        pulseColor = color;
        pulse = Mathf.Max(pulse, strength);
    }

    void UpdateBackground(float dt)
    {
        pulse = Mathf.MoveTowards(pulse, 0f, dt * 1.2f);
        pulseRenderer.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, flash ? pulse : 0f);

        // 星星慢慢往下飄，音樂開著的時候跟著節拍脹大
        float beat = music ? JuiceMusic.BeatPulse : 0f;
        for (int i = 0; i < stars.Length; i++)
        {
            Transform s = stars[i];
            Vector3 p = s.position;
            p.y -= (0.15f + starSize[i] * 3f) * dt;
            if (p.y < -5.8f) p = new Vector3(Random.Range(-9f, 9f), 5.8f, 0f);
            s.position = p;
            float size = starSize[i] * (1f + beat * 0.9f);
            s.localScale = new Vector3(size, size, 1f);
        }
    }

    void UpdateEyes()
    {
        if (!eyes) return;
        // 球掉下來快到板子的時候，板子會睜大眼睛
        for (int i = 0; i < balls.Length; i++)
        {
            Ball b = balls[i];
            if (!b.alive || b.stuck) continue;
            if (b.dir.y < 0f && b.pos.y < PaddleY + 2.6f && b.pos.y > PaddleY) paddleEyes.Widen(0.15f);
            // 球靠近的磚塊也會嚇到
            int col = Mathf.FloorToInt((b.pos.x - GridLeft) / PitchX);
            int row = Mathf.FloorToInt((GridTop - b.pos.y) / PitchY);
            for (int rr = row - 1; rr <= row + 1; rr++)
            {
                for (int cc = col - 1; cc <= col + 1; cc++)
                {
                    if (rr < 0 || rr >= Rows || cc < 0 || cc >= Cols) continue;
                    Brick k = bricks[rr * Cols + cc];
                    if (k.alive) k.eyes.Widen(0.12f);
                }
            }
        }
    }

    // ------------------------------------------------------------------ 開關與畫面上的字

    bool Get(int index)
    {
        switch (index)
        {
            case 0: return spring;
            case 1: return squash;
            case 2: return eyes;
            case 3: return trail;
            case 4: return particles;
            case 5: return flash;
            case 6: return shake;
            case 7: return hitStop;
            case 8: return text;
            case 9: return sound;
            default: return music;
        }
    }

    void Set(int index, bool on)
    {
        switch (index)
        {
            case 0: spring = on; break;
            case 1: squash = on; break;
            case 2: eyes = on; break;
            case 3: trail = on; break;
            case 4: particles = on; break;
            case 5: flash = on; break;
            case 6: shake = on; break;
            case 7: hitStop = on; break;
            case 8: text = on; break;
            case 9: sound = on; break;
            default: music = on; break;
        }
    }

    /// <summary>全部打開或全部關掉。</summary>
    public void SetAll(bool on)
    {
        for (int i = 0; i < Names.Length; i++) Set(i, on);
    }

    bool AnyOn()
    {
        for (int i = 0; i < Names.Length; i++) if (Get(i)) return true;
        return false;
    }

    static void Fill(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    void OnGUI()
    {
        Event e = Event.current;
        float s = Mathf.Max(0.5f, Screen.height / 540f);
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = false };
            rowStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
            hudStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontStyle = FontStyle.Bold, wordWrap = false };
            comboStyle = new GUIStyle(hudStyle);
            hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter, wordWrap = false };
            buttonStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false };
        }
        titleStyle.fontSize = Mathf.RoundToInt(17f * s);
        rowStyle.fontSize = Mathf.RoundToInt(13f * s);
        hudStyle.fontSize = Mathf.RoundToInt(18f * s);
        hintStyle.fontSize = Mathf.RoundToInt(12f * s);
        buttonStyle.fontSize = Mathf.RoundToInt(13f * s);
        titleStyle.normal.textColor = Color.white;
        hudStyle.normal.textColor = Color.white;
        buttonStyle.normal.textColor = Color.white;
        hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.55f);

        // 記下滑鼠位置。用 IMGUI 的事件讀，新舊輸入系統都能用
        Vector2 mouse = e.mousePosition;
        if ((mouse - lastMouseGui).sqrMagnitude > 4f)
        {
            if (seenMouse) lastMouseMoveTime = Time.unscaledTime;
            seenMouse = true;
            lastMouseGui = mouse;
        }
        mouseScreen = new Vector3(mouse.x, Screen.height - mouse.y, 0f);

        const float rowHeight = 25f;
        panel = new Rect(10f * s, 10f * s, 182f * s, (56f + Names.Length * rowHeight + 46f) * s);
        Fill(panel, new Color(0.04f, 0.03f, 0.12f, 0.78f));
        GUI.Label(new Rect(panel.x + 12f * s, panel.y + 8f * s, panel.width, 24f * s), "JUICE KIT", titleStyle);
        rowStyle.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
        GUI.Label(new Rect(panel.x + 12f * s, panel.y + 29f * s, panel.width, 18f * s), "Showcase  v" + Juice.Version, rowStyle);

        for (int i = 0; i < Names.Length; i++)
        {
            Rect row = new Rect(panel.x + 6f * s, panel.y + (54f + i * rowHeight) * s, panel.width - 12f * s, (rowHeight - 2f) * s);
            bool on = Get(i);
            if (row.Contains(mouse)) Fill(row, new Color(1f, 1f, 1f, 0.08f));
            if (GUI.Button(row, GUIContent.none, GUIStyle.none))
            {
                Set(i, !on);
                on = !on;
                if (sound) JuiceSfx.Blip(JuiceBlip.Click, 0.3f);
            }
            Rect pill = new Rect(row.x + 5f * s, row.y + 5f * s, 26f * s, 13f * s);
            Fill(pill, on ? SwitchOn : new Color(1f, 1f, 1f, 0.16f));
            Fill(new Rect(on ? pill.xMax - 11f * s : pill.x + 2f * s, pill.y + 2f * s, 9f * s, 9f * s), on ? Color.white : new Color(1f, 1f, 1f, 0.55f));
            rowStyle.normal.textColor = on ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            GUI.Label(new Rect(pill.xMax + 8f * s, row.y, row.width - 40f * s, row.height), Keys[i] + "   " + Names[i], rowStyle);
        }

        float buttonY = panel.y + (58f + Names.Length * rowHeight) * s;
        Rect allRect = new Rect(panel.x + 10f * s, buttonY, 78f * s, 28f * s);
        Rect noneRect = new Rect(allRect.xMax + 6f * s, buttonY, 78f * s, 28f * s);
        Fill(allRect, SwitchOn);
        Fill(noneRect, new Color(1f, 1f, 1f, 0.18f));
        GUI.Label(allRect, "All", buttonStyle);
        GUI.Label(noneRect, "None", buttonStyle);
        if (GUI.Button(allRect, GUIContent.none, GUIStyle.none)) SetAll(true);
        if (GUI.Button(noneRect, GUIContent.none, GUIStyle.none)) SetAll(false);

        // 分數與連擊：放在場地裡面，上面那道牆的下方
        Vector3 topLeft = cam.WorldToScreenPoint(new Vector3(Left, Top, 0f));
        Vector3 topRight = cam.WorldToScreenPoint(new Vector3(Right, Top, 0f));
        float hudY = Screen.height - topLeft.y + 5f * s;
        GUI.Label(new Rect(topRight.x - 322f * s, hudY, 310f * s, 28f * s), "SCORE  " + score.ToString("000000"), hudStyle);
        if (combo >= 2)
        {
            comboStyle.alignment = TextAnchor.UpperLeft;
            comboStyle.fontSize = Mathf.RoundToInt(18f * s * Mathf.Clamp(comboPop, 0.6f, 2f));
            comboStyle.normal.textColor = Accent;
            GUI.Label(new Rect(topLeft.x + 12f * s, hudY, 320f * s, 60f * s), "COMBO x" + combo, comboStyle);
        }
        string hint = IsAuto ? "AUTO PLAY  -  move the mouse to take over" : "Click: extra ball";
        GUI.Label(new Rect(panel.xMax, Screen.height - 30f * s, Screen.width - panel.xMax, 24f * s),
            hint + "      Keys 1-9 0 -: toggles      Space: all on / off", hintStyle);

        if (e.type == EventType.KeyDown)
        {
            int index = -1;
            if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha9) index = e.keyCode - KeyCode.Alpha1;
            else if (e.keyCode == KeyCode.Alpha0) index = 9;
            else if (e.keyCode == KeyCode.Minus) index = 10;
            if (index >= 0)
            {
                Set(index, !Get(index));
                e.Use();
            }
            else if (e.keyCode == KeyCode.Space)
            {
                SetAll(!AnyOn());
                e.Use();
            }
        }
        else if (e.type == EventType.MouseDown && e.button == 0 && !panel.Contains(mouse))
        {
            lastMouseMoveTime = Time.unscaledTime;
            seenMouse = true;
            ExtraBall();
            e.Use();
        }
    }

    // ------------------------------------------------------------------ 用程式畫圖

    // 圓角矩形，上亮下暗，邊緣稍微深一點，看起來有厚度。寬 1 單位。
    static Sprite Rounded(int w, int h, float radius)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[w * h];
        float hx = w * 0.5f, hy = h * 0.5f;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - hx) - (hx - radius);
                float qy = Mathf.Abs(y + 0.5f - hy) - (hy - radius);
                float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                float d = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;      // 小於 0 = 在形狀裡面
                float alpha = Mathf.Clamp01(0.5f - d);
                float shade = Mathf.Lerp(0.8f, 1f, (float)y / (h - 1)) * Mathf.Lerp(0.84f, 1f, Mathf.Clamp01(-d / 3f));
                byte c = (byte)(Mathf.Clamp01(shade) * 255f);
                pixels[y * w + x] = new Color32(c, c, c, (byte)(alpha * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), w);
    }

    // 中間亮、往外淡出的光暈，直徑 1 單位
    static Sprite Glow(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[size * size];
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Clamp01(Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)((1f - d) * (1f - d) * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // 空心圓，直徑 1 單位
    static Sprite Ring(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[size * size];
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Abs(Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) - (half - 4f));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(3f - d) * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
