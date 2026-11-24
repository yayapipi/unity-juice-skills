using UnityEngine;

/// <summary>
/// 彈出：物件出現時從小彈到原本的大小。用在視窗、對話框、立繪、道具圖示。
/// 掛上去就會在每次啟用（SetActive(true)）時自動播放；也可以手動呼叫 Play()。
/// 多個物件設不同的 delay，就會一個接一個彈出來。
/// </summary>
[AddComponentMenu("Juice/Juice Pop")]
public class JuicePop : MonoBehaviour
{
    [Tooltip("每次啟用時自動播放。")]
    public bool playOnEnable = true;

    [Tooltip("彈出要幾秒。")]
    public float duration = 0.25f;

    [Tooltip("延遲幾秒才開始。")]
    public float delay;

    [Tooltip("曲線。OutBack 會稍微彈過頭，最常用。")]
    public JuiceEaseType ease = JuiceEaseType.OutBack;

    [Tooltip("打勾後不受 Time.timeScale 影響（暫停選單用）。")]
    public bool unscaledTime = true;

    Vector3 baseScale = Vector3.one;
    bool hasBase;
    float progress = 1f;
    float wait;

    void Awake()
    {
        Capture();
    }

    void Capture()
    {
        if (hasBase) return;
        hasBase = true;
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        if (playOnEnable) Play();
    }

    public void Play()
    {
        Capture();
        wait = delay;
        progress = 0f;
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        if (progress >= 1f) return;
        float dt = unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (wait > 0f)
        {
            wait -= dt;
            return;
        }
        progress += dt / Mathf.Max(0.01f, duration);
        transform.localScale = baseScale * (progress >= 1f ? 1f : JuiceEase.Evaluate(ease, progress));
    }

    void OnDisable()
    {
        if (!hasBase) return;
        progress = 1f;
        transform.localScale = baseScale;
    }
}
