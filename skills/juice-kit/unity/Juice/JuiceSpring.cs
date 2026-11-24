using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 彈簧動畫：位置與角度會彈過頭再晃回來，物件看起來有重量、有彈性。
///     JuiceSpring.Nudge(transform, Vector3.down * 0.3f);     // 推一下再彈回原位（被撞、後座力、被踩）
///     JuiceSpring.Wobble(transform, 15f);                    // 歪一下，左右晃幾下再回正
///     JuiceSpring.DropIn(transform, Vector3.up * 6f, 0.1f);  // 出場：從上方掉進來，落在現在的位置
///     JuiceSpring.MoveTo(transform, targetPosition);         // 用彈簧移動到新位置，會稍微衝過頭
///     JuiceSpring.Follow(transform, target);                 // 一直跟著目標，有延遲與回彈
/// 縮放的彈性請用 JuiceSquash。
/// 請用在不會被其他程式每一格直接指定位置的物件上，最保險的做法是用在只放圖的子物件。
/// </summary>
[AddComponentMenu("Juice/Juice Spring")]
[DisallowMultipleComponent]
public class JuiceSpring : MonoBehaviour
{
    [Tooltip("每秒來回幾次。3–5 像果凍，8 以上很硬。")]
    public float frequency = 4f;

    [Tooltip("阻尼。0.2 會晃很多下，0.5 晃一兩下，1 完全不會彈過頭。")]
    [Range(0.05f, 1f)]
    public float damping = 0.4f;

    [Tooltip("打勾後不受 Time.timeScale 影響（UI 或暫停畫面用）。")]
    public bool unscaledTime;

    [Tooltip("要跟隨的目標。留空 = 不跟隨。")]
    public Transform followTarget;

    [Tooltip("跟隨時與目標保持的距離。")]
    public Vector3 followOffset;

    Vector3 offset;                 // 世界座標：目前離原位多遠
    Vector3 velocity;
    float angle;                    // Z 軸角度（度）
    float angleVelocity;
    float delay;

    Vector3 restPosition;           // 不含彈簧位移的 localPosition
    Vector3 lastPosition;
    Vector3 lastOffsetLocal;
    bool positionApplied;
    Quaternion restRotation = Quaternion.identity;
    Quaternion lastRotation = Quaternion.identity;
    Quaternion lastDelta = Quaternion.identity;
    bool rotationApplied;

    static readonly Dictionary<Transform, JuiceSpring> cache = new Dictionary<Transform, JuiceSpring>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        cache.Clear();
    }

    /// <summary>目前的速度（世界座標，單位／秒）。可以拿來做傾斜或拉伸。</summary>
    public Vector3 Velocity { get { return velocity; } }

    /// <summary>目前離原位多遠（世界座標）。</summary>
    public Vector3 Offset { get { return offset; } }

    /// <summary>已經停下來了。</summary>
    public bool IsSettled
    {
        get
        {
            return delay <= 0f && offset.sqrMagnitude < 0.000001f && velocity.sqrMagnitude < 0.0001f
                   && Mathf.Abs(angle) < 0.02f && Mathf.Abs(angleVelocity) < 0.2f;
        }
    }

    // ------------------------------------------------------------------ 一行呼叫

    /// <summary>取得物件上的 JuiceSpring，沒有就自動加一個。</summary>
    public static JuiceSpring Get(Transform t)
    {
        if (t == null || !Application.isPlaying) return null;
        JuiceSpring spring;
        if (cache.TryGetValue(t, out spring) && spring != null) return spring;
        spring = t.GetComponent<JuiceSpring>();
        if (spring == null) spring = t.gameObject.AddComponent<JuiceSpring>();
        cache[t] = spring;
        return spring;
    }

    /// <summary>把物件推離原位 worldOffset，再彈回去。</summary>
    public static JuiceSpring Nudge(Transform t, Vector3 worldOffset)
    {
        JuiceSpring spring = Get(t);
        if (spring != null) spring.Nudge(worldOffset);
        return spring;
    }

    /// <summary>歪 degrees 度，左右晃幾下再回正。</summary>
    public static JuiceSpring Wobble(Transform t, float degrees = 12f)
    {
        JuiceSpring spring = Get(t);
        if (spring != null) spring.Wobble(degrees);
        return spring;
    }

    /// <summary>出場：先放在 fromOffset 的位置，等 delay 秒後彈進現在的位置。</summary>
    public static JuiceSpring DropIn(Transform t, Vector3 fromOffset, float delay = 0f)
    {
        JuiceSpring spring = Get(t);
        if (spring != null) spring.DropIn(fromOffset, delay);
        return spring;
    }

    /// <summary>用彈簧移動到 worldPosition。</summary>
    public static JuiceSpring MoveTo(Transform t, Vector3 worldPosition)
    {
        JuiceSpring spring = Get(t);
        if (spring != null) spring.MoveTo(worldPosition);
        return spring;
    }

    /// <summary>一直跟著 target。停止跟隨：JuiceSpring.Get(transform).StopFollow();</summary>
    public static JuiceSpring Follow(Transform t, Transform target, Vector3 offset = default(Vector3))
    {
        JuiceSpring spring = Get(t);
        if (spring != null) spring.Follow(target, offset);
        return spring;
    }

    // ------------------------------------------------------------------ 元件用法

    public void Nudge(Vector3 worldOffset)
    {
        offset += worldOffset * JuiceSettings.Intensity;
        enabled = true;
    }

    /// <summary>給一個速度（單位／秒），物件會被甩出去再拉回來。比 Nudge 柔和。</summary>
    public void Kick(Vector3 worldVelocity)
    {
        velocity += worldVelocity * JuiceSettings.Intensity;
        enabled = true;
    }

    public void Wobble(float degrees = 12f)
    {
        angle += degrees * JuiceSettings.Intensity;
        enabled = true;
    }

    public void DropIn(Vector3 fromOffset, float startDelay = 0f)
    {
        SyncRest();
        offset = fromOffset;
        velocity = Vector3.zero;
        delay = Mathf.Max(0f, startDelay);
        enabled = true;
        Apply();
    }

    public void MoveTo(Vector3 worldPosition)
    {
        SyncRest();
        Vector3 shown = transform.position;
        transform.position = worldPosition;
        restPosition = transform.localPosition;
        offset = shown - worldPosition;
        enabled = true;
        Apply();
    }

    public void Follow(Transform target, Vector3 targetOffset = default(Vector3))
    {
        followTarget = target;
        followOffset = targetOffset;
        enabled = true;
    }

    public void StopFollow()
    {
        followTarget = null;
    }

    /// <summary>馬上停下來，回到原位。</summary>
    public void Settle()
    {
        SyncRest();
        offset = Vector3.zero;
        velocity = Vector3.zero;
        angle = 0f;
        angleVelocity = 0f;
        delay = 0f;
        Rest();
    }

    // ------------------------------------------------------------------ 內部

    void LateUpdate()
    {
        float dt = unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        SyncRest();

        if (delay > 0f)
        {
            delay -= dt;
        }
        else if (dt > 0f)
        {
            dt = Mathf.Min(dt, 0.05f);
            if (followTarget != null)
            {
                // 把原位搬到目標上，畫面上的位置先不變，再讓彈簧把差距拉回來
                Transform t = transform;
                t.localPosition = restPosition;
                Vector3 targetWorld = followTarget.position + followOffset;
                offset += t.position - targetWorld;
                t.position = targetWorld;
                restPosition = t.localPosition;
            }
            offset.x = Step(offset.x, ref velocity.x, 0f, frequency, damping, dt);
            offset.y = Step(offset.y, ref velocity.y, 0f, frequency, damping, dt);
            offset.z = Step(offset.z, ref velocity.z, 0f, frequency, damping, dt);
            angle = Step(angle, ref angleVelocity, 0f, frequency, damping, dt);
        }

        if (followTarget == null && IsSettled)
        {
            offset = Vector3.zero;
            velocity = Vector3.zero;
            angle = 0f;
            angleVelocity = 0f;
            Rest();
            enabled = false;
            return;
        }
        Apply();
    }

    // 讀出「不含彈簧位移」的原位。物件如果被別的程式移動過，就把移動量算進原位。
    void SyncRest()
    {
        Transform t = transform;
        if (!positionApplied) restPosition = t.localPosition;
        else if (t.localPosition != lastPosition) restPosition = t.localPosition - lastOffsetLocal;

        if (!rotationApplied) restRotation = t.localRotation;
        else if (t.localRotation != lastRotation) restRotation = t.localRotation * Quaternion.Inverse(lastDelta);
    }

    void Apply()
    {
        Transform t = transform;
        Vector3 local = t.parent != null ? t.parent.InverseTransformVector(offset) : offset;
        t.localPosition = restPosition + local;
        lastPosition = t.localPosition;
        lastOffsetLocal = local;
        positionApplied = true;

        Quaternion delta = Quaternion.Euler(0f, 0f, angle);
        t.localRotation = restRotation * delta;
        lastRotation = t.localRotation;
        lastDelta = delta;
        rotationApplied = true;
    }

    void Rest()
    {
        Transform t = transform;
        if (positionApplied) t.localPosition = restPosition;
        if (rotationApplied) t.localRotation = restRotation;
        positionApplied = false;
        rotationApplied = false;
    }

    void OnDisable()
    {
        if (!positionApplied && !rotationApplied) return;
        SyncRest();
        Rest();
    }

    void OnDestroy()
    {
        cache.Remove(transform);
    }

    /// <summary>
    /// 彈簧的數學本體：讓 current 往 target 靠近，回傳新的值，速度存在 velocity。
    /// 想自己控制某個數字（位置、角度、大小、顏色）的時候，每一格呼叫一次：
    ///     x = JuiceSpring.Step(x, ref xVelocity, targetX, 5f, 0.5f, Time.deltaTime);
    /// </summary>
    public static float Step(float current, ref float velocity, float target, float frequency, float damping, float dt)
    {
        if (dt <= 0f) return current;
        float w = 2f * Mathf.PI * Mathf.Max(0.01f, frequency);
        float f = 1f + 2f * dt * damping * w;
        float ww = w * w;
        float hww = dt * ww;
        float hhww = dt * hww;
        float inv = 1f / (f + hhww);
        float x = (f * current + dt * velocity + hhww * target) * inv;
        velocity = (velocity + hww * (target - current)) * inv;
        return x;
    }
}
