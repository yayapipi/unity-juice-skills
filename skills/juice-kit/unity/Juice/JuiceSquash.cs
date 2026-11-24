using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 擠壓與拉伸。一行就能用，會自動掛到物件上：
///     JuiceSquash.Squash(transform, 0.25f);   // 壓扁（落地、被踩）
///     JuiceSquash.Stretch(transform, 0.25f);  // 拉長（起跳、衝刺）
///     JuiceSquash.Punch(transform, 0.2f);     // 整個放大再彈回（撿到東西、被打、按鈕）
/// 體積大致不變：X 變成 1.25 倍時，Y 會變成 0.8 倍。
/// 用縮放翻面（localScale.x = -1）的角色也沒問題，正負號會保留。
///
/// 會自己挑安全的物件來變形（見 SafeTarget）：角色的圖放在子物件時只變形那個子物件，碰撞框不受影響；
/// 圖和「會受重力掉落的剛體」在同一個物件上時不會變形，因為那會讓角色浮起來或卡進地板。
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

    static readonly List<Renderer> scratch = new List<Renderer>();
    static readonly Dictionary<Transform, JuiceSquash> resolved = new Dictionary<Transform, JuiceSquash>();
    static readonly HashSet<Transform> unsafeRoots = new HashSet<Transform>();
    static bool warned;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        resolved.Clear();
        unsafeRoots.Clear();
        warned = false;
    }

    /// <summary>直接在這個物件上取得（沒有就加上）JuiceSquash，不做安全檢查。UI 或你確定沒有物理的物件用。</summary>
    public static JuiceSquash Get(Transform t)
    {
        if (t == null) return null;
        JuiceSquash s = t.GetComponent<JuiceSquash>();
        if (s == null) s = t.gameObject.AddComponent<JuiceSquash>();
        return s;
    }

    /// <summary>
    /// 找出可以安全變形的物件。
    /// 1. 物件本身沒有碰撞框與剛體：就是它自己。
    /// 2. 有物理，而圖放在子物件上：回傳放圖的子物件，碰撞框不會跟著變形。
    /// 3. 圖和物理在同一個物件上，而且會受重力掉落（Dynamic 剛體或 CharacterController）：回傳 null。
    ///    這種物件一變形，碰撞框與底下的地面偵測點會跟著移動，角色會浮起來、卡進地板或重複觸發落地。
    /// </summary>
    public static Transform SafeTarget(Transform root)
    {
        if (root == null) return null;
        if (!HasPhysics(root)) return root;

        Renderer own = root.GetComponent<Renderer>();
        if (!IsVisual(own))
        {
            Transform visual = null;
            root.GetComponentsInChildren(true, scratch);
            for (int i = 0; i < scratch.Count && visual == null; i++)
            {
                Renderer r = scratch[i];
                if (r.transform == root || !IsVisual(r)) continue;
                // 優先用 root 底下第一層的那個子物件（整組圖一起變形）；裡面如果還有碰撞框，就只用放圖的那一個
                Transform top = r.transform;
                while (top.parent != root) top = top.parent;
                if (!HasColliderInChildren(top)) visual = top;
                else if (!HasPhysics(r.transform)) visual = r.transform;
            }
            scratch.Clear();
            if (visual != null) return visual;
        }
        return FallsWithGravity(root) ? null : root;
    }

    static bool IsVisual(Renderer r)
    {
        return r is SpriteRenderer || r is MeshRenderer || r is SkinnedMeshRenderer;
    }

    static bool HasPhysics(Transform t)
    {
        return t.GetComponent<Collider2D>() != null || t.GetComponent<Collider>() != null ||
               t.GetComponent<Rigidbody2D>() != null || t.GetComponent<Rigidbody>() != null;
    }

    static bool HasColliderInChildren(Transform t)
    {
        return t.GetComponentInChildren<Collider2D>(true) != null || t.GetComponentInChildren<Collider>(true) != null;
    }

    static bool FallsWithGravity(Transform t)
    {
        Rigidbody2D body2D = t.GetComponent<Rigidbody2D>();
        if (body2D != null && body2D.bodyType == RigidbodyType2D.Dynamic && body2D.gravityScale != 0f) return true;
        Rigidbody body = t.GetComponent<Rigidbody>();
        if (body != null && !body.isKinematic && body.useGravity) return true;
        return t.GetComponent<CharacterController>() != null;
    }

    // 靜態呼叫用：物件上已經有 JuiceSquash 就用它（尊重 Inspector 的 target），否則挑安全的物件
    static JuiceSquash Resolve(Transform t)
    {
        if (t == null || !Application.isPlaying) return null;
        JuiceSquash cached;
        if (resolved.TryGetValue(t, out cached) && cached != null) return cached;
        if (unsafeRoots.Contains(t)) return null;
        // 物件被刪掉之後記錄還會留著：累積太多就整個清掉重來
        if (resolved.Count > 512) resolved.Clear();
        if (unsafeRoots.Count > 512) unsafeRoots.Clear();

        JuiceSquash s = t.GetComponent<JuiceSquash>();
        if (s == null)
        {
            Transform safe = SafeTarget(t);
            if (safe == null)
            {
                unsafeRoots.Add(t);
                if (!warned)
                {
                    warned = true;
                    Debug.LogWarning("[Juice] 「" + t.name + "」的圖和會受重力影響的剛體在同一個物件上，為了不影響碰撞與落地判定，" +
                                     "沒有套用擠壓拉伸（其他回饋照常）。把圖（Sprite 或模型）移到子物件就會自動生效。", t);
                }
                return null;
            }
            s = Get(safe);
        }
        resolved[t] = s;
        return s;
    }

    public static void Squash(Transform t, float amount = 0.25f)
    {
        JuiceSquash s = Resolve(t);
        if (s != null) s.Squash(amount);
    }

    public static void Stretch(Transform t, float amount = 0.25f)
    {
        JuiceSquash s = Resolve(t);
        if (s != null) s.Stretch(amount);
    }

    public static void Punch(Transform t, float amount = 0.2f)
    {
        JuiceSquash s = Resolve(t);
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
