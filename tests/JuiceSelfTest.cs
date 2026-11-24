using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Juice Kit 的自我測試。在 Play 模式下把每個功能實際跑一遍並檢查結果。
/// 由 tests/Editor/JuiceTestRunner.cs 啟動（Unity 批次模式），結果寫到環境變數 JUICE_TEST_OUT 指定的資料夾。
/// 這個檔案不需要放進你的遊戲專案。
/// </summary>
public class JuiceSelfTest : MonoBehaviour
{
    readonly List<string> lines = new List<string>();
    readonly List<string> errors = new List<string>();
    int failures;
    string outDir;

    // 有 text 與 maxVisibleCharacters 的假元件，用來測 TextMeshPro 的那條路徑
    public class FakeTmp : MonoBehaviour
    {
        string value = "";
        int visible = 99999;
        public string text { get { return value; } set { this.value = value; } }
        public int maxVisibleCharacters { get { return visible; } set { visible = value; } }
    }

    void Check(bool ok, string what)
    {
        lines.Add((ok ? "PASS  " : "FAIL  ") + what);
        if (!ok) failures++;
    }

    void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            if (stackTrace != null && stackTrace.Contains("UnityEditor.Search")) return; // 編輯器自己的搜尋索引，跟 Juice 無關
            errors.Add(type + ": " + condition);
        }
    }

    static IEnumerator WaitReal(float seconds)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end) yield return null;
    }

    static Texture2D Capture(Camera cam, int w = 960, int h = 540)
    {
        RenderTexture rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
        RenderTexture previous = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture = previous;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return tex;
    }

    static Color PixelAt(Texture2D tex, Camera cam, Vector3 world)
    {
        // 用截圖自己的長寬比換算，不能用 WorldToViewportPoint（那是螢幕的長寬比）
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * tex.width / tex.height;
        Vector3 local = world - cam.transform.position;
        float vx = 0.5f + local.x / (2f * halfWidth);
        float vy = 0.5f + local.y / (2f * halfHeight);
        return tex.GetPixel(Mathf.RoundToInt(vx * tex.width), Mathf.RoundToInt(vy * tex.height));
    }

    static bool IsWhite(Color c)
    {
        return c.r > 0.9f && c.g > 0.9f && c.b > 0.9f;
    }

    static float Diff(Texture2D a, Texture2D b)
    {
        Color32[] pa = a.GetPixels32();
        Color32[] pb = b.GetPixels32();
        long sum = 0;
        for (int i = 0; i < pa.Length; i += 7)
        {
            sum += Mathf.Abs(pa[i].r - pb[i].r) + Mathf.Abs(pa[i].g - pb[i].g) + Mathf.Abs(pa[i].b - pb[i].b);
        }
        return sum / (float)(pa.Length / 7);
    }

    void Save(Texture2D tex, string fileName)
    {
        if (string.IsNullOrEmpty(outDir)) return;
        File.WriteAllBytes(Path.Combine(outDir, fileName), tex.EncodeToPNG());
    }

    IEnumerator Start()
    {
        outDir = Environment.GetEnvironmentVariable("JUICE_TEST_OUT");
        Application.logMessageReceived += OnLog;
        StartCoroutine(Watchdog());

        // 讓 JuiceDemo 先把場景擺好
        yield return null;
        yield return null;
        Camera cam = Camera.main;
        JuiceDemo demo = FindAnyObjectByType<JuiceDemo>();
        Check(cam != null, "scene has a main camera");
        Check(demo != null, "JuiceMenu.AddDemoToScene created the demo");
        Check(cam != null && cam.orthographic, "demo set the camera to orthographic");
        JuiceSettings.ScreenShake = true;
        JuiceSettings.Flashes = true;
        JuiceSettings.Intensity = 1f;
        lines.Add("INFO  Unity " + Application.unityVersion + ", pipeline: " +
                  (GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.GetType().Name : "Built-in") +
                  ", Juice " + Juice.Version);

        // ---------------------------------------------------------------- easing
        bool easeOk = true;
        foreach (JuiceEaseType type in Enum.GetValues(typeof(JuiceEaseType)))
        {
            if (Mathf.Abs(JuiceEase.Evaluate(type, 0f)) > 0.001f) easeOk = false;
            if (Mathf.Abs(JuiceEase.Evaluate(type, 1f) - 1f) > 0.001f) easeOk = false;
        }
        Check(easeOk, "JuiceEase: every curve starts at 0 and ends at 1");
        Check(JuiceEase.Evaluate(JuiceEaseType.OutBack, 0.6f) > 1f, "JuiceEase: OutBack overshoots");

        // ---------------------------------------------------------------- squash
        GameObject box = new GameObject("Squash Box");
        box.transform.localScale = new Vector3(-2f, 2f, 1f); // 用負的 X 模擬「用縮放翻面」的角色
        JuiceSquash.Squash(box.transform, 0.25f);
        Vector3 squashed = box.transform.localScale;
        Check(Mathf.Abs(squashed.x - (-2.5f)) < 0.01f && Mathf.Abs(squashed.y - 1.6f) < 0.01f,
            "JuiceSquash.Squash: (1.25, 0.8) applied at once, sign kept: " + squashed);
        yield return WaitReal(0.5f);
        Check(Vector3.Distance(box.transform.localScale, new Vector3(-2f, 2f, 1f)) < 0.0001f,
            "JuiceSquash: returns exactly to the original scale: " + box.transform.localScale);
        JuiceSquash.Stretch(box.transform, 0.25f);
        Check(Mathf.Abs(box.transform.localScale.y - 2.5f) < 0.01f, "JuiceSquash.Stretch: taller");
        yield return null;
        JuiceSquash.Punch(box.transform, 0.2f); // 動畫中再觸發
        yield return WaitReal(0.5f);
        Check(Vector3.Distance(box.transform.localScale, new Vector3(-2f, 2f, 1f)) < 0.0001f,
            "JuiceSquash: re-trigger mid-animation still returns to the original scale");
        Destroy(box);

        // 圖和會掉落的剛體在同一個物件上：不能變形（碰撞框會跟著動）
        GameObject faller = new GameObject("Faller");
        faller.AddComponent<SpriteRenderer>().sprite = JuiceAssets.SquareSprite;
        faller.AddComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        faller.AddComponent<BoxCollider2D>();
        faller.transform.position = new Vector3(-7f, 3.5f, 0f);
        JuiceSquash.Squash(faller.transform, 0.25f);
        Juice.Land(faller.transform);
        Juice.Jump(faller.transform);
        Check(faller.transform.localScale == Vector3.one && faller.GetComponent<JuiceSquash>() == null &&
              JuiceSquash.SafeTarget(faller.transform) == null,
            "JuiceSquash: never deforms an object whose art shares a falling rigidbody");

        // 圖在子物件上：只變形子物件
        GameObject hero = new GameObject("Hero");
        hero.AddComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        hero.AddComponent<CapsuleCollider2D>();
        hero.transform.position = new Vector3(-7f, 2f, 0f);
        GameObject art = new GameObject("Art");
        art.transform.SetParent(hero.transform, false);
        art.AddComponent<SpriteRenderer>().sprite = JuiceAssets.SquareSprite;
        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(hero.transform, false);
        groundCheck.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        Vector3 checkBefore = groundCheck.transform.position;
        Juice.Land(hero.transform);
        Check(hero.transform.localScale == Vector3.one && Mathf.Abs(art.transform.localScale.x - 1.25f) < 0.01f &&
              groundCheck.transform.position == checkBefore,
            "JuiceSquash: squashes the art child and leaves the physics root and ground check alone: " + art.transform.localScale);
        yield return WaitReal(0.4f);
        Check(Vector3.Distance(art.transform.localScale, Vector3.one) < 0.0001f, "JuiceSquash: art child returns to scale 1");

        // 不會掉落的物件（Kinematic）：可以直接變形
        GameObject floater = new GameObject("Floater");
        floater.AddComponent<SpriteRenderer>().sprite = JuiceAssets.SquareSprite;
        floater.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        floater.AddComponent<BoxCollider2D>().isTrigger = true;
        floater.transform.position = new Vector3(-7f, 0.5f, 0f);
        JuiceSquash.Punch(floater.transform, 0.2f);
        Check(Mathf.Abs(floater.transform.localScale.x - 1.2f) < 0.01f, "JuiceSquash: kinematic objects are deformed directly");
        yield return WaitReal(0.4f);
        Destroy(faller);
        Destroy(hero);
        Destroy(floater);

        // ---------------------------------------------------------------- time
        Check(Mathf.Approximately(Time.timeScale, 1f), "time scale is 1 before the tests");
        float fixedBefore = Time.fixedDeltaTime;
        JuiceTime.HitStop(0.15f);
        Check(Time.timeScale == 0f && JuiceTime.IsStopped, "JuiceTime.HitStop: time stops immediately");
        yield return WaitReal(0.05f);
        JuiceTime.HitStop(0.2f); // 延長
        yield return WaitReal(0.15f);
        Check(Time.timeScale == 0f, "JuiceTime.HitStop: a second call extends the stop");
        yield return WaitReal(0.2f);
        Check(Mathf.Approximately(Time.timeScale, 1f) && !JuiceTime.IsStopped, "JuiceTime.HitStop: time resumes at 1");

        JuiceTime.SlowMo(0.3f, 0.2f, 0.1f);
        yield return null;
        Check(Mathf.Abs(Time.timeScale - 0.3f) < 0.001f, "JuiceTime.SlowMo: time scale 0.3");
        Check(Mathf.Abs(Time.fixedDeltaTime - fixedBefore * 0.3f) < 0.0001f, "JuiceTime.SlowMo: fixedDeltaTime scaled too");
        JuiceTime.HitStop(0.05f); // 慢動作中頓幀
        Check(Time.timeScale == 0f, "JuiceTime: hit stop during slow motion");
        yield return WaitReal(0.6f);
        Check(Mathf.Approximately(Time.timeScale, 1f), "JuiceTime: back to 1 after slow motion + hit stop: " + Time.timeScale);
        Check(Mathf.Abs(Time.fixedDeltaTime - fixedBefore) < 0.00001f, "JuiceTime: fixedDeltaTime restored");

        Time.timeScale = 0f; // 模擬暫停選單
        JuiceTime.HitStop(0.05f);
        yield return WaitReal(0.15f);
        Check(Time.timeScale == 0f, "JuiceTime.HitStop: leaves a paused game paused");
        Time.timeScale = 1f;

        JuiceTime.SlowMo(0.5f, 1f);
        JuiceTime.HitStop(0.1f);
        JuiceTime.Cancel(); // 頓幀 + 慢動作進行中，玩家按下暫停
        Check(Mathf.Approximately(Time.timeScale, 1f) && Mathf.Abs(Time.fixedDeltaTime - fixedBefore) < 0.00001f && !JuiceTime.IsStopped,
            "JuiceTime.Cancel: ends hit stop and slow motion at once");
        Time.timeScale = 0f;
        yield return WaitReal(0.4f);
        Check(Time.timeScale == 0f, "JuiceTime.Cancel: a pause that follows stays paused");
        Time.timeScale = 1f;
        yield return WaitReal(1f);
        Check(Mathf.Approximately(Time.timeScale, 1f), "JuiceTime.Cancel: nothing changes time afterwards");

        // ---------------------------------------------------------------- shake
        Vector3 camBase = cam.transform.position;
        Quaternion camRotation = cam.transform.rotation;
        float camSize = cam.orthographicSize;
        Texture2D still = Capture(cam);
        JuiceShake shakeComponent = JuiceShake.Main;
        Check(shakeComponent != null && shakeComponent.gameObject == cam.gameObject, "JuiceShake.Main: attached itself to the main camera");

        Vector3 duringRender = camBase;
        float sizeDuringRender = camSize;
        Action<ScriptableRenderContext, Camera> probe = delegate(ScriptableRenderContext context, Camera c)
        {
            if (c == cam)
            {
                duringRender = c.transform.position;
                sizeDuringRender = c.orthographicSize;
            }
        };
        RenderPipelineManager.beginCameraRendering += probe;
        Camera.CameraCallback probeBuiltIn = delegate(Camera c) { if (c == cam) { duringRender = c.transform.position; sizeDuringRender = c.orthographicSize; } };
        Camera.onPreRender += probeBuiltIn;

        JuiceShake.Add(0.9f);
        JuiceShake.Zoom(0.1f, 1f);
        yield return null;
        Texture2D shaken = Capture(cam);
        Check(Vector3.Distance(duringRender, camBase) > 0.01f, "JuiceShake: camera is offset while rendering: " + (duringRender - camBase));
        Check(sizeDuringRender < camSize - 0.01f, "JuiceShake.Zoom: lens is zoomed in while rendering: " + sizeDuringRender);
        Check(cam.transform.position == camBase && cam.transform.rotation == camRotation && Mathf.Approximately(cam.orthographicSize, camSize),
            "JuiceShake: camera transform and size are restored after rendering");
        Check(Diff(still, shaken) > 1f, "JuiceShake: the rendered image really moved (diff " + Diff(still, shaken).ToString("0.0") + ")");
        Save(shaken, "shake.png");

        JuiceSettings.ScreenShake = false;
        duringRender = camBase;
        Texture2D calm = Capture(cam);
        Check(duringRender == camBase, "JuiceSettings.ScreenShake = false: no offset at all");
        JuiceSettings.ScreenShake = true;
        JuiceShake.Kick(Vector2.left, 0.5f);
        Capture(cam);
        Check(duringRender.x < camBase.x - 0.1f, "JuiceShake.Kick: pushes the camera left");
        yield return WaitReal(1.3f);
        Check(shakeComponent.trauma <= 0.0001f, "JuiceShake: trauma decays to zero");
        duringRender = camBase;
        Capture(cam);
        Check(Vector3.Distance(duringRender, camBase) < 0.001f, "JuiceShake: camera is still again after the shake");
        RenderPipelineManager.beginCameraRendering -= probe;
        Camera.onPreRender -= probeBuiltIn;

        // ---------------------------------------------------------------- flash (sprite)
        Transform block = demo.transform.Find("Target 2");
        Check(block != null, "demo created its target blocks");
        Texture2D beforeFlash = Capture(cam);
        Color blockColor = PixelAt(beforeFlash, cam, block.position);
        Check(!IsWhite(blockColor), "sprite renders with its own colour: " + blockColor);
        SpriteRenderer blockRenderer = block.GetComponent<SpriteRenderer>();
        Material originalMaterial = blockRenderer.sharedMaterial;
        JuiceFlash.Play(block.gameObject, 0.2f);
        Check(blockRenderer.sharedMaterial != originalMaterial && blockRenderer.sharedMaterial.shader.name == "Hidden/Juice/FlashSprite",
            "JuiceFlash: sprite material swapped to " + blockRenderer.sharedMaterial.shader.name);
        Check(blockRenderer.sharedMaterial.shader.isSupported, "JuiceFlashSprite shader compiles on this pipeline");
        Texture2D flashed = Capture(cam);
        Check(IsWhite(PixelAt(flashed, cam, block.position)), "JuiceFlash: sprite is drawn pure white: " + PixelAt(flashed, cam, block.position));
        Color beside = PixelAt(flashed, cam, block.position + new Vector3(1f, 0f, 0f));
        Check(!IsWhite(beside), "JuiceFlash: only the sprite turns white, not the background");
        Save(flashed, "flash.png");
        yield return WaitReal(0.35f);
        Check(blockRenderer.sharedMaterial == originalMaterial, "JuiceFlash: original material restored");

        JuiceSettings.Flashes = false;
        JuiceFlash.Play(block.gameObject, 0.2f);
        Check(blockRenderer.sharedMaterial == originalMaterial, "JuiceSettings.Flashes = false: no flash");
        JuiceSettings.Flashes = true;

        // ---------------------------------------------------------------- flash (3D mesh)
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = new Vector3(6.5f, -0.6f, 0f);
        MeshRenderer cubeRenderer = cube.GetComponent<MeshRenderer>();
        Material cubeMaterial = cubeRenderer.sharedMaterial;
        yield return null;
        Texture2D cubeBefore = Capture(cam);
        Color cubeColor = PixelAt(cubeBefore, cam, cube.transform.position);
        JuiceFlash.Play(cube, Color.red, 0.2f);
        Check(cubeRenderer.sharedMaterial.shader.name == "Hidden/Juice/FlashMesh" && cubeRenderer.sharedMaterial.shader.isSupported,
            "JuiceFlash: mesh material swapped and shader supported");
        Texture2D cubeFlashed = Capture(cam);
        Color cubeFlashColor = PixelAt(cubeFlashed, cam, cube.transform.position);
        Check(cubeFlashColor.r > 0.9f && cubeFlashColor.g < 0.1f && cubeFlashColor.b < 0.1f,
            "JuiceFlash: 3D mesh is drawn in the flash colour: " + cubeColor + " -> " + cubeFlashColor);
        yield return WaitReal(0.35f);
        Check(cubeRenderer.sharedMaterial == cubeMaterial, "JuiceFlash: mesh material restored");
        Destroy(cube);

        // ---------------------------------------------------------------- particles
        Texture2D noParticles = Capture(cam);
        JuiceBurst.Spawn(new Vector3(6f, 2f, 0f), Color.green, 12, 3f, 0.4f, 0.6f);
        ParticleSystem burst = null;
        foreach (ParticleSystem ps in FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include)) burst = ps;
        Check(burst != null && burst.particleCount == 12, "JuiceBurst.Spawn: 12 particles alive: " + (burst != null ? burst.particleCount : -1));
        ParticleSystemRenderer burstRenderer = burst != null ? burst.GetComponent<ParticleSystemRenderer>() : null;
        Check(burstRenderer != null && burstRenderer.sharedMaterial != null && burstRenderer.sharedMaterial.shader != null &&
              burstRenderer.sharedMaterial.shader.isSupported,
            "JuiceBurst: particle material uses " + (burstRenderer != null && burstRenderer.sharedMaterial != null ? burstRenderer.sharedMaterial.shader.name : "nothing"));
        yield return WaitReal(0.1f);
        Texture2D withParticles = Capture(cam);
        int green = 0;
        foreach (Color32 p in withParticles.GetPixels32()) if (p.g > 200 && p.r < 80 && p.b < 80) green++;
        Check(green > 50, "JuiceBurst: green particles are visible in the render (" + green + " px)");
        yield return WaitReal(0.9f);
        Check(burst != null && burst.particleCount == 0, "JuiceBurst: particles are simulated and die on their own");
        JuiceBurst.Spark(Vector3.zero, Vector3.right);
        JuiceBurst.Dust(Vector3.zero);
        JuiceBurst.Explosion(Vector3.zero);
        JuiceBurst.Confetti(Vector3.zero);
        yield return null;

        // ---------------------------------------------------------------- trail
        GameObject mover = new GameObject("Trail Mover");
        TrailRenderer trail = JuiceTrail.Attach(mover, Color.cyan);
        Check(trail != null && trail.transform.parent == mover.transform && trail.sharedMaterial != null, "JuiceTrail.Attach: trail on a child object");
        JuiceTrail.SetEmitting(mover, false);
        Check(!trail.emitting, "JuiceTrail.SetEmitting(false)");
        Destroy(mover);

        // ---------------------------------------------------------------- sound
        bool blipsOk = true;
        foreach (JuiceBlip blip in Enum.GetValues(typeof(JuiceBlip)))
        {
            AudioClip clip = JuiceSfx.GetBlip(blip);
            if (clip == null || clip.samples < 1000) { blipsOk = false; continue; }
            float[] data = new float[clip.samples];
            clip.GetData(data, 0);
            float peak = 0f;
            for (int i = 0; i < data.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            if (peak < 0.1f || peak > 1f) blipsOk = false;
            JuiceSfx.Blip(blip, 0.2f);
            yield return null;
            yield return null;
        }
        Check(blipsOk, "JuiceSfx: all ten built-in blips are generated with audible, unclipped samples");
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include);
        Check(sources.Length == 10, "JuiceSfx: pooled audio sources: " + sources.Length);
        JuiceSfx.PlayCombo(JuiceSfx.GetBlip(JuiceBlip.Coin), 12, 0.2f);
        bool octave = false;
        foreach (AudioSource s in sources) if (s.clip == JuiceSfx.GetBlip(JuiceBlip.Coin) && Mathf.Abs(s.pitch - 2f) < 0.001f) octave = true;
        Check(octave, "JuiceSfx.PlayCombo: combo 12 plays one octave up");

        // ---------------------------------------------------------------- floating text
        JuiceFloatingText.Spawn(new Vector3(-6f, 3f, 0f), "36!", Color.yellow, 1.5f, 0.3f, 1f);
        TextMesh floating = FindAnyObjectByType<TextMesh>();
        Check(floating != null && floating.text == "36!" && floating.GetComponent<MeshRenderer>().sharedMaterial != null,
            "JuiceFloatingText.Spawn: text object created with a font material");
        yield return WaitReal(0.1f);
        Texture2D withText = Capture(cam);
        int yellow = 0;
        foreach (Color32 p in withText.GetPixels32()) if (p.r > 200 && p.g > 180 && p.b < 90) yellow++;
        Check(yellow > 30, "JuiceFloatingText: text is visible in the render (" + yellow + " px)");
        yield return WaitReal(0.4f);
        Check(floating != null && !floating.gameObject.activeSelf, "JuiceFloatingText: returns to the pool when done");
        JuiceFloatingText.Spawn(Vector3.zero, "12");
        Check(FindObjectsByType<TextMesh>(FindObjectsInactive.Include).Length == 1, "JuiceFloatingText: pooled object is reused");

        // ---------------------------------------------------------------- UI text helpers
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas));
        GameObject textObject = new GameObject("Score", typeof(RectTransform));
        textObject.transform.SetParent(canvasObject.transform, false);
        Text uiText = textObject.AddComponent<Text>();
        JuiceText found = JuiceText.Find(textObject);
        Check(found != null && !found.SupportsVisibleCount, "JuiceText.Find: finds UI Text");
        JuiceCounter counter = textObject.AddComponent<JuiceCounter>();
        counter.duration = 0.2f;
        counter.SetInstant(1234);
        Check(uiText.text == "1234", "JuiceCounter: the default format shows the number as it was: " + uiText.text);
        counter.format = "{0:N0}";
        counter.SetInstant(0);
        counter.Set(1234);
        yield return WaitReal(0.1f);
        int mid;
        int.TryParse(uiText.text.Replace(",", ""), out mid);
        Check(mid > 0 && mid < 1234, "JuiceCounter: number is rolling: " + uiText.text);
        yield return WaitReal(0.4f);
        Check(uiText.text == "1,234", "JuiceCounter: ends on the exact value with separators: " + uiText.text);
        Check(Vector3.Distance(textObject.transform.localScale, Vector3.one) < 0.0001f, "JuiceCounter: punch returns to scale 1");

        GameObject lineObject = new GameObject("Line", typeof(RectTransform));
        lineObject.transform.SetParent(canvasObject.transform, false);
        Text lineText = lineObject.AddComponent<Text>();
        JuiceTypewriter typewriter = lineObject.AddComponent<JuiceTypewriter>();
        typewriter.charsPerSecond = 60f;
        typewriter.punctuationPause = 0.02f;
        bool completed = false;
        typewriter.onComplete.AddListener(delegate { completed = true; });
        typewriter.Play("很久以前，<b>王子</b>來了。");
        yield return null;
        yield return null;
        Check(typewriter.IsPlaying && lineText.text.Length > 0 && lineText.text.Length < 16, "JuiceTypewriter: reveals gradually: \"" + lineText.text + "\"");
        yield return WaitReal(0.6f);
        Check(!typewriter.IsPlaying && completed && lineText.text == "很久以前，<b>王子</b>來了。", "JuiceTypewriter: finishes with the full text and fires onComplete");
        typewriter.Play("第二句話很長很長很長很長");
        typewriter.Complete();
        Check(lineText.text == "第二句話很長很長很長很長", "JuiceTypewriter.Complete: skips to the end");

        GameObject tmpObject = new GameObject("Fake TMP");
        FakeTmp fake = tmpObject.AddComponent<FakeTmp>();
        JuiceTypewriter tmpTypewriter = tmpObject.AddComponent<JuiceTypewriter>();
        tmpTypewriter.charsPerSecond = 60f;
        tmpTypewriter.blip = false;
        tmpTypewriter.Play("<color=red>ABC</color>DEF");
        yield return null;
        Check(fake.text == "<color=red>ABC</color>DEF" && fake.maxVisibleCharacters < 6, "JuiceTypewriter: TextMeshPro path uses maxVisibleCharacters: " + fake.maxVisibleCharacters);
        yield return WaitReal(0.3f);
        Check(fake.maxVisibleCharacters == 99999, "JuiceTypewriter: TextMeshPro path shows everything at the end");

        // ---------------------------------------------------------------- pop
        GameObject popObject = new GameObject("Pop");
        popObject.transform.localScale = new Vector3(2f, 2f, 2f);
        JuicePop pop = popObject.AddComponent<JuicePop>();
        pop.duration = 0.15f;
        Check(popObject.transform.localScale == Vector3.zero, "JuicePop: starts from zero when enabled");
        yield return WaitReal(0.3f);
        Check(Vector3.Distance(popObject.transform.localScale, new Vector3(2f, 2f, 2f)) < 0.0001f, "JuicePop: ends at the original scale");

        // ---------------------------------------------------------------- button
        GameObject buttonObject = new GameObject("Button", typeof(RectTransform));
        buttonObject.transform.SetParent(canvasObject.transform, false);
        buttonObject.AddComponent<Image>();
        Button uiButton = buttonObject.AddComponent<Button>();
        JuiceButton juiceButton = buttonObject.AddComponent<JuiceButton>();
        juiceButton.OnPointerEnter(null);
        yield return WaitReal(0.5f);
        Check(Mathf.Abs(buttonObject.transform.localScale.x - 1.05f) < 0.005f, "JuiceButton: hover scale 1.05: " + buttonObject.transform.localScale.x);
        juiceButton.OnPointerDown(null);
        yield return WaitReal(0.5f);
        Check(Mathf.Abs(buttonObject.transform.localScale.x - 0.95f) < 0.005f, "JuiceButton: press scale 0.95: " + buttonObject.transform.localScale.x);
        juiceButton.OnPointerUp(null);
        juiceButton.OnPointerExit(null);
        yield return WaitReal(0.5f);
        Check(Mathf.Abs(buttonObject.transform.localScale.x - 1f) < 0.005f, "JuiceButton: back to 1");
        uiButton.interactable = false;
        juiceButton.OnPointerEnter(null);
        yield return WaitReal(0.2f);
        Check(Mathf.Abs(buttonObject.transform.localScale.x - 1f) < 0.005f, "JuiceButton: ignores a disabled button");

        // ---------------------------------------------------------------- fade
        bool fadedOut = false;
        JuiceFade.Out(0.15f, Color.black, delegate { fadedOut = true; });
        yield return WaitReal(0.3f);
        Check(fadedOut && JuiceFade.Amount > 0.99f, "JuiceFade.Out: fully covered and callback fired");
        JuiceFade.In(0.15f);
        yield return WaitReal(0.3f);
        Check(JuiceFade.Amount < 0.01f, "JuiceFade.In: clear again");

        // ---------------------------------------------------------------- jump assist
        JuiceJumpAssist jump = new JuiceJumpAssist();
        jump.Tick(true);
        jump.Tick(false);
        jump.Press(); // 剛離開平台就按：土狼時間
        Check(jump.TryJump(), "JuiceJumpAssist: coyote time allows a jump just after leaving the ground");
        Check(!jump.TryJump(), "JuiceJumpAssist: one press gives only one jump");
        Check(jump.ShouldCutJump(false, 5f) && !jump.ShouldCutJump(false, 5f), "JuiceJumpAssist: jump cut fires once");
        yield return new WaitForSeconds(0.2f);
        jump.Press();
        Check(!jump.TryJump(), "JuiceJumpAssist: no jump in mid-air after coyote time");
        yield return new WaitForSeconds(0.05f);
        jump.Tick(true); // 緩衝時間內落地
        Check(jump.TryJump(), "JuiceJumpAssist: buffered press jumps on landing");
        jump.Press();
        yield return new WaitForSeconds(0.3f);
        jump.Tick(true);
        Check(!jump.TryJump(), "JuiceJumpAssist: buffer expires");
        Check(Mathf.Approximately(jump.GravityMultiplier(0.5f, true), 0.5f) && Mathf.Approximately(jump.GravityMultiplier(-5f, false), 1.5f) &&
              Mathf.Approximately(jump.GravityMultiplier(8f, true), 1f), "JuiceJumpAssist: gravity multipliers");

        JuiceJumpAssist hop = new JuiceJumpAssist();
        hop.Tick(true);
        hop.Press();
        Check(hop.TryJump(), "JuiceJumpAssist: jumps from the ground");
        hop.Tick(true); // 起跳後的下一格，地面偵測還碰得到地
        hop.Press();
        Check(!hop.TryJump(), "JuiceJumpAssist: no second jump while the ground check still overlaps after take-off");
        hop.Tick(false);
        hop.Tick(true);
        Check(hop.TryJump(), "JuiceJumpAssist: the buffered press jumps once the character has really left and landed");
        yield return new WaitForSeconds(0.25f);
        hop.Tick(true); // 起跳後一直沒有離地（頭頂被擋住）
        hop.Press();
        Check(hop.TryJump(), "JuiceJumpAssist: re-arms when the character never left the ground");

        // ---------------------------------------------------------------- follow camera
        GameObject followTarget = new GameObject("Follow Target");
        GameObject followCamera = new GameObject("Follow Camera");
        followCamera.transform.position = new Vector3(0f, 0f, -10f);
        JuiceFollowCamera follow = followCamera.AddComponent<JuiceFollowCamera>();
        follow.target = followTarget.transform;
        yield return null;
        followTarget.transform.position = new Vector3(0.2f, 0f, 0f);
        yield return WaitReal(0.3f);
        Check(Mathf.Abs(followCamera.transform.position.x) < 0.05f, "JuiceFollowCamera: stays still inside the dead zone");
        followTarget.transform.position = new Vector3(10f, 3f, 0f);
        yield return WaitReal(1.5f);
        Vector3 followed = followCamera.transform.position;
        Check(Mathf.Abs(followed.x - 9.6f) < 0.1f && Mathf.Abs(followed.y - 2.4f) < 0.1f && Mathf.Abs(followed.z + 10f) < 0.01f,
            "JuiceFollowCamera: follows with damping and keeps its offset: " + followed);
        Destroy(followCamera);
        Destroy(followTarget);

        // ---------------------------------------------------------------- feedback component + recipes
        GameObject feedbackObject = new GameObject("Feedback");
        feedbackObject.AddComponent<SpriteRenderer>().sprite = JuiceAssets.SquareSprite;
        feedbackObject.transform.position = new Vector3(-6f, -1f, 0f);
        JuiceFeedback feedback = feedbackObject.AddComponent<JuiceFeedback>();
        feedback.hitStop = 0.05f;
        feedback.slowMotion = 0.5f;
        feedback.slowMotionDuration = 0.1f;
        feedback.shake = 0.4f;
        feedback.zoomPunch = 0.05f;
        feedback.scale = JuiceFeedback.ScaleEffect.Punch;
        feedback.flash = true;
        feedback.particles = 8;
        feedback.sound = JuiceFeedback.SoundSource.BuiltInBlip;
        feedback.floatingText = "+1";
        bool eventFired = false;
        feedback.onPlay.AddListener(delegate { eventFired = true; });
        feedback.Play();
        Check(eventFired && Time.timeScale == 0f && shakeComponent.trauma > 0.3f, "JuiceFeedback.Play: everything fires together");
        yield return WaitReal(1f);
        Check(Mathf.Approximately(Time.timeScale, 1f), "JuiceFeedback: time is back to normal: " + Time.timeScale);

        Juice.Hit(feedbackObject, feedbackObject.transform.position);
        Juice.Hit(feedbackObject, 0.5f);
        Juice.Hurt(feedbackObject);
        Juice.Jump(feedbackObject.transform);
        Juice.Land(feedbackObject.transform, 2f);
        Juice.Pickup(Vector3.zero, null, "+1");
        Juice.Coin(Vector3.zero, 3);
        Juice.Explode(Vector3.zero);
        Juice.Damage(Vector3.zero, 12);
        Juice.Damage(Vector3.zero, 36, true);
        Juice.Click(feedbackObject.transform);
        Juice.Win(Vector3.zero);
        Juice.Lose();
        Juice.Hit(null, Vector3.zero); // 不能因為 null 當掉
        JuiceFlash.Play(null);
        JuiceSquash.Punch(null);
        JuiceSfx.Play(null);
        yield return WaitReal(2f);
        Check(Mathf.Approximately(Time.timeScale, 1f) && Mathf.Abs(Time.fixedDeltaTime - fixedBefore) < 0.00001f,
            "Juice recipes: all thirteen run and time returns to normal: " + Time.timeScale);
        Check(Vector3.Distance(feedbackObject.transform.localScale, Vector3.one) < 0.0001f, "Juice recipes: object scale returns to 1");
        Destroy(feedbackObject);

        // ---------------------------------------------------------------- demo
        Destroy(canvasObject); // 測試用的 UI 會蓋住畫面，截圖前先拿掉
        Destroy(tmpObject);
        Destroy(popObject);
        yield return null;
        demo.sound = demo.squash = demo.flash = demo.particles = demo.shake = demo.hitStop = demo.damageText = true;
        Texture2D best = null;
        for (int round = 0; round < 3; round++)
        {
            for (int i = 0; i < 5; i++)
            {
                demo.HitTarget(i);
                yield return WaitReal(0.07f);
                if (round == 1 && i == 2) best = Capture(cam);
            }
        }
        if (best != null) Save(best, "demo.png");
        yield return WaitReal(0.4f);
        int alive = 0;
        for (int i = 1; i <= 5; i++) if (demo.transform.Find("Target " + i).gameObject.activeSelf) alive++;
        Check(alive == 0, "JuiceDemo: three hits destroy every block");
        yield return WaitReal(1.6f);
        alive = 0;
        for (int i = 1; i <= 5; i++) if (demo.transform.Find("Target " + i).gameObject.activeSelf) alive++;
        Check(alive == 5, "JuiceDemo: blocks respawn");
        Transform jumper = demo.transform.Find("Jumper");
        float lowest = jumper.position.y;
        float highest = jumper.position.y;
        float watchUntil = Time.realtimeSinceStartup + 2f;
        while (Time.realtimeSinceStartup < watchUntil)
        {
            lowest = Mathf.Min(lowest, jumper.position.y);
            highest = Mathf.Max(highest, jumper.position.y);
            yield return null;
        }
        Check(highest - lowest > 1f, "JuiceDemo: the jumper keeps jumping (height " + (highest - lowest).ToString("0.0") + ")");
        // 宣傳用的截圖：一格裡同時看得到閃白、火花、傷害數字
        demo.hitStop = false;
        demo.shake = false;
        demo.HitTarget(0);
        yield return WaitReal(0.06f);
        demo.HitTarget(1);
        demo.HitTarget(3);
        yield return WaitReal(0.05f);
        demo.HitTarget(2);
        demo.HitTarget(4);
        yield return WaitReal(0.025f);
        Save(Capture(cam, 1280, 720), "beauty.png");
        yield return WaitReal(0.5f);

        Check(Mathf.Approximately(Time.timeScale, 1f), "time scale is 1 at the end");

        // ---------------------------------------------------------------- done
        yield return null;
        Check(errors.Count == 0, "no errors or exceptions were logged (" + errors.Count + ")");
        foreach (string e in errors) lines.Add("LOG   " + e);
        Finish(failures);
    }

    IEnumerator Watchdog()
    {
        yield return WaitReal(150f);
        lines.Add("FAIL  timed out");
        Finish(99);
    }

    void Finish(int code)
    {
        Application.logMessageReceived -= OnLog;
        lines.Add("RESULT " + (code == 0 ? "ALL PASSED" : code + " FAILED") + " (" + lines.FindAll(l => l.StartsWith("PASS")).Count + " passed)");
        string report = string.Join("\n", lines.ToArray());
        Debug.Log("[JuiceSelfTest]\n" + report);
        if (!string.IsNullOrEmpty(outDir)) File.WriteAllText(Path.Combine(outDir, "results.txt"), report + "\n");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(code);
#else
        Application.Quit(code);
#endif
    }
}
