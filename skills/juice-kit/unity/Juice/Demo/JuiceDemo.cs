using System.Collections;
using UnityEngine;

/// <summary>
/// Juice Kit 的示範：同一個小遊戲，左邊的開關一項一項打開，感受差在哪裡。
/// 用法：選單 Tools ▸ Juice ▸ Create Demo Scene，或把這個元件掛在任何場景的空物件上，按 Play。
/// 畫面全部用程式產生，不需要任何素材。用滑鼠點方塊攻擊，打三下會爆炸。
/// </summary>
[AddComponentMenu("Juice/Juice Demo")]
public class JuiceDemo : MonoBehaviour
{
    [Header("開關（執行時也可以在畫面左邊切換）")]
    public bool sound;
    public bool squash;
    public bool flash;
    public bool particles;
    public bool shake;
    public bool hitStop;
    public bool damageText;

    const int TargetCount = 5;
    const float FloorY = -2.6f;

    static readonly Color[] Palette =
    {
        new Color(1f, 0.42f, 0.48f), new Color(1f, 0.74f, 0.3f), new Color(0.4f, 0.85f, 0.6f),
        new Color(0.35f, 0.7f, 1f), new Color(0.72f, 0.55f, 1f)
    };

    Camera cam;
    Transform[] targets;
    int[] health;
    Transform jumper;
    float jumperVelocity;
    float jumperRest;
    float jumperDirection = 1f;
    bool jumperGrounded = true;
    int hits;
    GUIStyle labelStyle;
    GUIStyle toggleStyle;
    GUIStyle buttonStyle;
    Rect panel;

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
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.transform.rotation = Quaternion.identity;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.09f, 0.22f);

        targets = new Transform[TargetCount];
        health = new int[TargetCount];
        for (int i = 0; i < TargetCount; i++)
        {
            targets[i] = CreateBlock("Target " + (i + 1), new Vector3(-2f + i * 2f, 1.3f, 0f), new Vector2(1.3f, 1.3f), Palette[i]);
            health[i] = 3;
        }
        CreateBlock("Floor", new Vector3(1.5f, FloorY - 0.6f, 0f), new Vector2(14f, 0.3f), new Color(0.3f, 0.27f, 0.5f));
        jumper = CreateBlock("Jumper", new Vector3(-2f, FloorY, 0f), new Vector2(0.8f, 0.8f), Color.white);
        jumper.position = new Vector3(-2f, FloorY - 0.05f, 0f);
    }

    Transform CreateBlock(string blockName, Vector3 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(blockName);
        go.transform.SetParent(transform, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = JuiceAssets.SquareSprite;
        renderer.color = color;
        return go.transform;
    }

    void Update()
    {
        UpdateJumper();
    }

    // 一個自己一直跳的小方塊，用來看擠壓拉伸與塵土
    void UpdateJumper()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || jumper == null) return;
        Vector3 p = jumper.position;
        float ground = FloorY - 0.05f;

        if (jumperGrounded)
        {
            jumperRest -= dt;
            if (jumperRest <= 0f)
            {
                jumperGrounded = false;
                jumperVelocity = 9f;
                if (squash) JuiceSquash.Stretch(jumper, 0.25f);
                if (particles) JuiceBurst.Dust(new Vector3(p.x, ground - 0.4f, 0f));
                if (sound) JuiceSfx.Blip(JuiceBlip.Jump, 0.18f);
            }
        }
        else
        {
            jumperVelocity -= 22f * dt;
            p.y += jumperVelocity * dt;
            p.x += jumperDirection * 2.2f * dt;
            if (p.x > 5.5f) jumperDirection = -1f;
            if (p.x < -2.5f) jumperDirection = 1f;
            if (p.y <= ground)
            {
                p.y = ground;
                jumperGrounded = true;
                jumperRest = 0.45f;
                if (squash) JuiceSquash.Squash(jumper, 0.3f);
                if (particles) JuiceBurst.Dust(new Vector3(p.x, ground - 0.4f, 0f));
                if (sound) JuiceSfx.Blip(JuiceBlip.Land, 0.18f);
            }
            jumper.position = p;
        }
    }

    /// <summary>攻擊第 index 個方塊（0–4）。畫面上點方塊就是呼叫這個。</summary>
    public void HitTarget(int index)
    {
        if (targets == null || index < 0 || index >= targets.Length) return;
        Transform t = targets[index];
        if (t == null || !t.gameObject.activeSelf) return;

        hits++;
        health[index]--;
        Vector3 point = t.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0f);
        bool dead = health[index] <= 0;
        int damage = Random.Range(8, 15);

        if (damageText) Juice.Damage(point + Vector3.up * 0.4f, dead ? damage * 3 : damage, dead);

        if (!dead)
        {
            if (sound) JuiceSfx.Blip(JuiceBlip.Hit);
            if (squash) JuiceSquash.Punch(t, 0.25f);
            if (flash) JuiceFlash.Play(t.gameObject);
            if (particles) JuiceBurst.Spark(point, default(Vector3), Palette[index]);
            if (shake) JuiceShake.Add(0.3f);
            if (hitStop) JuiceTime.HitStop(0.06f);
            return;
        }

        if (sound) JuiceSfx.Blip(JuiceBlip.Explosion, 0.6f);
        if (particles) JuiceBurst.Explosion(t.position, Palette[index]);
        if (shake)
        {
            JuiceShake.Add(0.6f);
            JuiceShake.Zoom(0.04f, 0.3f);
        }
        if (hitStop) JuiceTime.HitStop(0.1f);
        t.gameObject.SetActive(false);
        StartCoroutine(Respawn(index));
    }

    IEnumerator Respawn(int index)
    {
        yield return new WaitForSeconds(1.2f);
        Transform t = targets[index];
        if (t == null) yield break;
        health[index] = 3;
        t.gameObject.SetActive(true);
        if (squash) JuiceSquash.Punch(t, -0.6f);
        if (sound) JuiceSfx.Blip(JuiceBlip.Pickup, 0.3f);
    }

    void SetAll(bool on)
    {
        sound = squash = flash = particles = shake = hitStop = damageText = on;
    }

    void OnGUI()
    {
        float s = Mathf.Max(1f, Screen.height / 540f);
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            toggleStyle = new GUIStyle(GUI.skin.toggle);
            buttonStyle = new GUIStyle(GUI.skin.button);
        }
        labelStyle.fontSize = toggleStyle.fontSize = buttonStyle.fontSize = Mathf.RoundToInt(15f * s);
        labelStyle.normal.textColor = Color.white;

        panel = new Rect(12f * s, 12f * s, 190f * s, 330f * s);
        GUILayout.BeginArea(panel, GUI.skin.box);
        GUILayout.Label("Juice Kit Demo", labelStyle);
        GUILayout.Space(4f * s);
        sound = Toggle(sound, "Sound 音效", s);
        squash = Toggle(squash, "Squash 擠壓拉伸", s);
        flash = Toggle(flash, "Flash 閃白", s);
        particles = Toggle(particles, "Particles 粒子", s);
        shake = Toggle(shake, "Shake 鏡頭震動", s);
        hitStop = Toggle(hitStop, "Hit Stop 頓幀", s);
        damageText = Toggle(damageText, "Text 傷害數字", s);
        GUILayout.Space(6f * s);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("All 全開", buttonStyle, GUILayout.Height(30f * s))) SetAll(true);
        if (GUILayout.Button("None 全關", buttonStyle, GUILayout.Height(30f * s))) SetAll(false);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        GUI.Label(new Rect(panel.xMax + 14f * s, 14f * s, 600f * s, 30f * s),
            "Click the blocks 點方塊攻擊，打三下會爆炸    Hits: " + hits, labelStyle);

        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && !panel.Contains(e.mousePosition) && cam != null && targets != null)
        {
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(e.mousePosition.x, Screen.height - e.mousePosition.y, 10f));
            for (int i = 0; i < targets.Length; i++)
            {
                Transform t = targets[i];
                if (t == null || !t.gameObject.activeSelf) continue;
                Vector3 half = new Vector3(0.75f, 0.75f, 0f);
                Vector3 d = world - t.position;
                if (Mathf.Abs(d.x) <= half.x && Mathf.Abs(d.y) <= half.y)
                {
                    HitTarget(i);
                    e.Use();
                    break;
                }
            }
        }
    }

    bool Toggle(bool value, string text, float s)
    {
        return GUILayout.Toggle(value, " " + text, toggleStyle, GUILayout.Height(32f * s));
    }
}
