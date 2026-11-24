using UnityEngine;

/// <summary>
/// 擠壓與拉伸。一行就能用，會自動掛到物件上：
///     JuiceSquash.Squash(transform, 0.25f);   // 壓扁（落地、被踩）
///     JuiceSquash.Stretch(transform, 0.25f);  // 拉長（起跳、衝刺）
///     JuiceSquash.Punch(transform, 0.2f);     // 整個放大再彈回（撿到東西、被打、按鈕）
/// 體積大致不變：X 變成 1.25 倍時，Y 會變成 0.8 倍。
/// 用縮放翻面（localScale.x = -1）的角色也沒問題，正負號會保留。
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Juice/Juice Squash")]
public class JuiceSquash : MonoBehaviour
{
    [Tooltip("要變形的物件。留空 = 自己。建議指定只放圖的子物件，碰撞框才不會跟著變形。")]
    public Transform target;

    [Tooltip("彈回原狀要幾秒。0.1–0.2 最有彈性。")]
    public float duration = 0.18f;

    [Tooltip("回彈曲線。OutBack 會稍微彈過頭，OutElastic 會抖幾下。")]
    public JuiceEaseType ease = JuiceEaseType.OutBack;

    [Tooltip("打勾後不受 Time.timeScale 影響（UI 或暫停畫面用）。")]
    public bool unscaledTime;

    Vector3 baseScale = Vector3.one;
    Vector3 fromMultiplier = Vector3.one;
    float progress = 1f;
    bool playing;

    public static JuiceSquash Get(Transform t)
    {
        if (t == null) return null;
        JuiceSquash s = t.GetComponent<JuiceSquash>();
        if (s == null) s = t.gameObject.AddComponent<JuiceSquash>();
        return s;
    }

    public static void Squash(Transform t, float amount = 0.25f)
    {
        JuiceSquash s = Get(t);
        if (s != null) s.Squash(amount);
    }

    public static void Stretch(Transform t, float amount = 0.25f)
    {
        JuiceSquash s = Get(t);
        if (s != null) s.Stretch(amount);
    }

    public static void Punch(Transform t, float amount = 0.2f)
    {
        JuiceSquash s = Get(t);
        if (s != null) s.Punch(amount);
    }

    /// <summary>壓扁：變寬變矮。amount 0.25 = 寬 1.25 倍、高 0.8 倍。</summary>
    public void Squash(float amount = 0.25f)
    {
        float a = 1f + Mathf.Max(0f, amount);
        Play(new Vector3(a, 1f / a, a));
    }

    /// <summary>拉長：變窄變高。</summary>
    public void Stretch(float amount = 0.25f)
    {
        float a = 1f + Mathf.Max(0f, amount);
        Play(new Vector3(1f / a, a, 1f / a));
    }

    /// <summary>整個放大再彈回。amount 可以是負的（縮小再彈回）。</summary>
    public void Punch(float amount = 0.2f)
    {
        float a = Mathf.Max(0.01f, 1f + amount);
        Play(new Vector3(a, a, a));
    }

    /// <summary>從 multiplier 倍彈回 1 倍。</summary>
    public void Play(Vector3 multiplier)
    {
        if (!isActiveAndEnabled) return;
        Transform tr = target != null ? target : transform;
        if (!playing)
        {
            Vector3 s = tr.localScale;
            baseScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        }
        fromMultiplier = multiplier;
        progress = 0f;
        playing = true;
        ApplyScale(tr, multiplier);
    }

    void Update()
    {
        if (!playing) return;
        Transform tr = target != null ? target : transform;
        progress += (unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / Mathf.Max(0.01f, duration);
        if (progress >= 1f)
        {
            playing = false;
            ApplyScale(tr, Vector3.one);
            return;
        }
        float e = JuiceEase.Evaluate(ease, progress);
        ApplyScale(tr, Vector3.LerpUnclamped(fromMultiplier, Vector3.one, e));
    }

    void OnDisable()
    {
        if (!playing) return;
        playing = false;
        ApplyScale(target != null ? target : transform, Vector3.one);
    }

    void ApplyScale(Transform tr, Vector3 multiplier)
    {
        Vector3 current = tr.localScale;
        tr.localScale = new Vector3(
            (current.x < 0f ? -1f : 1f) * baseScale.x * multiplier.x,
            (current.y < 0f ? -1f : 1f) * baseScale.y * multiplier.y,
            (current.z < 0f ? -1f : 1f) * baseScale.z * multiplier.z);
    }
}
