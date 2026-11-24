using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一行噴出粒子，不用做 Prefab、不用素材：
///     JuiceBurst.Spark(hitPoint, hitDirection);     // 命中火花
///     JuiceBurst.Dust(feetPosition);                // 起跳、落地的塵土
///     JuiceBurst.Explosion(position);               // 爆炸、死亡
///     JuiceBurst.Confetti(position);                // 過關、獎勵
///     JuiceBurst.Spawn(position, Color.cyan, 12);   // 自己指定
/// 所有粒子共用幾個 Particle System，會自動回收，數量有上限。
/// 正交鏡頭（2D）時粒子在畫面平面上散開，透視鏡頭（3D）時往四面八方散開。
/// </summary>
public static class JuiceBurst
{
    /// <summary>2D 的排序順序。粒子被擋住時把它調大。</summary>
    public static int SortingOrder = 100;

    static readonly Dictionary<int, ParticleSystem> systems = new Dictionary<int, ParticleSystem>();

    static readonly Color[] ConfettiColors =
    {
        new Color(1f, 0.36f, 0.42f), new Color(1f, 0.8f, 0.25f), new Color(0.35f, 0.85f, 0.55f),
        new Color(0.3f, 0.7f, 1f), new Color(0.75f, 0.5f, 1f)
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        systems.Clear();
    }

    /// <summary>
    /// 噴出 count 顆粒子。
    /// speed：初速（單位／秒）；size：大小；lifetime：存活秒數；gravity：重力倍率（0 = 不會掉）；
    /// direction 與 spread：往哪個方向、散開幾度（360 = 全方向）。
    /// </summary>
    public static void Spawn(Vector3 position, Color color, int count = 10, float speed = 5f, float size = 0.2f,
        float lifetime = 0.4f, float gravity = 0f, Vector3 direction = default(Vector3), float spread = 360f)
    {
        if (!JuiceRunner.Available) return;
        count = Mathf.RoundToInt(count * Mathf.Lerp(0.3f, 1f, JuiceSettings.Intensity));
        if (count <= 0) return;

        ParticleSystem ps = GetSystem(gravity);
        if (ps == null) return;

        Camera cam = Camera.main;
        bool planar = cam == null || cam.orthographic;
        Vector3 planeNormal = cam != null ? cam.transform.forward : Vector3.forward;
        Vector3 dir = direction.sqrMagnitude > 0.000001f ? direction.normalized : (cam != null ? cam.transform.up : Vector3.up);
        float half = Mathf.Clamp(spread, 0f, 360f) * 0.5f;

        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            Vector3 d;
            if (planar)
            {
                d = Quaternion.AngleAxis(Random.Range(-half, half), planeNormal) * dir;
            }
            else if (half >= 179f)
            {
                d = Random.onUnitSphere;
            }
            else
            {
                Vector3 axis = Vector3.Cross(dir, Random.onUnitSphere);
                if (axis.sqrMagnitude < 0.0001f) axis = Vector3.Cross(dir, Vector3.right);
                d = Quaternion.AngleAxis(Random.Range(0f, half), axis.normalized) * dir;
            }
            emit.position = position;
            emit.velocity = d * (speed * Random.Range(0.35f, 1f));
            emit.startColor = color;
            emit.startSize = size * Random.Range(0.5f, 1f);
            emit.startLifetime = lifetime * Random.Range(0.6f, 1f);
            ps.Emit(emit, 1);
        }
    }

    /// <summary>命中火花：又快又短。有給 direction 就往那個方向噴（通常是命中面的反彈方向）。</summary>
    public static void Spark(Vector3 position, Vector3 direction = default(Vector3), Color? color = null)
    {
        bool directed = direction.sqrMagnitude > 0.000001f;
        Spawn(position, color ?? new Color(1f, 0.87f, 0.35f), 10, 9f, 0.16f, 0.25f, 0f, direction, directed ? 70f : 360f);
    }

    /// <summary>塵土：少少幾顆、慢慢散開。放在角色腳邊。</summary>
    public static void Dust(Vector3 position, Color? color = null)
    {
        Spawn(position, color ?? new Color(0.86f, 0.86f, 0.86f, 0.85f), 6, 1.8f, 0.32f, 0.45f, 0f, default(Vector3), 170f);
    }

    /// <summary>爆炸：很多、很快、全方向。power 1 = 一般，2 = 大爆炸。</summary>
    public static void Explosion(Vector3 position, Color? color = null, float power = 1f)
    {
        Color c = color ?? new Color(1f, 0.55f, 0.2f);
        Spawn(position, c, Mathf.RoundToInt(22f * power), 9f * power, 0.3f, 0.5f, 0f, default(Vector3), 360f);
        Spawn(position, Color.white, Mathf.RoundToInt(8f * power), 4f * power, 0.2f, 0.3f, 0f, default(Vector3), 360f);
    }

    /// <summary>彩帶：五種顏色往上噴再掉下來。</summary>
    public static void Confetti(Vector3 position, int count = 40)
    {
        int each = Mathf.Max(1, count / ConfettiColors.Length);
        for (int i = 0; i < ConfettiColors.Length; i++)
        {
            Spawn(position, ConfettiColors[i], each, 9f, 0.18f, 1.2f, 1.6f, default(Vector3), 110f);
        }
    }

    static ParticleSystem GetSystem(float gravity)
    {
        int key = Mathf.RoundToInt(gravity * 10f);
        ParticleSystem ps;
        if (systems.TryGetValue(key, out ps) && ps != null) return ps;

        JuiceRunner runner = JuiceRunner.Instance;
        if (runner == null) return null;

        GameObject go = new GameObject("[Juice] Burst (gravity " + gravity.ToString("0.0") + ")");
        go.SetActive(false);
        go.transform.SetParent(runner.transform, false);
        ps = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 2000;
        main.startSpeed = 0f;
        main.gravityModifier = gravity;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;

        // 越來越小
        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.5f, 0.85f), new Keyframe(1f, 0f)));

        // 最後淡出
        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = JuiceAssets.ParticleMaterial;
        renderer.sortingOrder = SortingOrder;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        go.SetActive(true);
        systems[key] = ps;
        return ps;
    }
}
