using UnityEngine;

/// <summary>
/// 數字滾動：分數、金幣不要瞬間變，用滾的跳上去，同時彈一下。
/// 掛在有文字元件（TextMeshPro 或 UI Text）的物件上：
///     counter.Set(score);     // 從目前顯示的數字滾到 score
///     counter.Add(10);        // 加 10
///     counter.SetInstant(0);  // 直接設定，不播動畫
/// </summary>
[AddComponentMenu("Juice/Juice Counter")]
public class JuiceCounter : MonoBehaviour
{
    [Tooltip("數字的格式。{0:N0} 會加千分位（1,234），{0} 不加，也可以寫「分數：{0}」。")]
    public string format = "{0:N0}";

    [Tooltip("滾到目標數字要幾秒。")]
    public float duration = 0.4f;

    [Tooltip("數字改變時放大多少再彈回。0 = 不彈。")]
    public float punch = 0.15f;

    [Tooltip("打勾後不受 Time.timeScale 影響（暫停或頓幀時照樣滾）。")]
    public bool unscaledTime = true;

    JuiceText label;
    bool initialized;
    float from;
    float to;
    float shown;
    float progress = 1f;

    /// <summary>目標數字（動畫結束後會顯示的值）。</summary>
    public float Value
    {
        get { return to; }
    }

    void Awake()
    {
        Init();
    }

    void Init()
    {
        if (initialized) return;
        initialized = true;
        label = JuiceText.Find(gameObject);
        if (label == null) Debug.LogWarning("[Juice] JuiceCounter 找不到文字元件，請掛在有 TextMeshPro 或 UI Text 的物件上。", this);
    }

    public void Set(float value)
    {
        Init();
        if (Mathf.Approximately(value, to) && progress >= 1f) return;
        from = shown;
        to = value;
        progress = 0f;
        if (punch > 0f && isActiveAndEnabled)
        {
            JuiceSquash squash = JuiceSquash.Get(transform);
            squash.unscaledTime = unscaledTime;
            squash.Punch(punch);
        }
    }

    public void Add(float delta)
    {
        Set(to + delta);
    }

    public void SetInstant(float value)
    {
        Init();
        from = to = shown = value;
        progress = 1f;
        Write();
    }

    void Update()
    {
        if (progress >= 1f) return;
        progress += (unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / Mathf.Max(0.01f, duration);
        shown = progress >= 1f ? to : Mathf.Lerp(from, to, JuiceEase.Evaluate(JuiceEaseType.OutCubic, progress));
        Write();
    }

    void Write()
    {
        if (label != null && label.IsValid) label.Text = string.Format(format, Mathf.RoundToInt(shown));
    }
}
