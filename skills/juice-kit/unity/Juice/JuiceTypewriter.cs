using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 打字機：對話一個字一個字出現，標點會停頓，可以搭配「答答」聲。
/// 掛在有文字元件（建議 TextMeshPro）的物件上：
///     typewriter.Play("很久很久以前……");
///     if (typewriter.IsPlaying) typewriter.Complete(); else NextLine();   // 玩家點一下
/// 用 TextMeshPro 時 Rich Text 標籤（顏色、粗體）不會被拆開；UI Text 則會略過 &lt;...&gt; 標籤再逐字顯示。
/// </summary>
[AddComponentMenu("Juice/Juice Typewriter")]
public class JuiceTypewriter : MonoBehaviour
{
    [Tooltip("每秒出現幾個字。中文 20–30，英文 40–60。")]
    public float charsPerSecond = 28f;

    [Tooltip("遇到標點多停幾秒。")]
    public float punctuationPause = 0.18f;

    [Tooltip("啟用時自動把文字元件上現有的文字打一遍。")]
    public bool playOnEnable;

    [Tooltip("出字時播放內建的答答聲。")]
    public bool blip = true;

    [Tooltip("每幾個字響一次。")]
    public int blipEvery = 2;

    [Tooltip("打勾後不受 Time.timeScale 影響。")]
    public bool unscaledTime = true;

    [Tooltip("整句顯示完的時候呼叫。")]
    public UnityEvent onComplete = new UnityEvent();

    const string Punctuation = "，。、！？；：…,.!?;:";

    readonly List<int> visibleIndices = new List<int>();
    JuiceText label;
    bool initialized;
    string full = "";
    int shown;
    float timer;
    bool playing;

    public bool IsPlaying
    {
        get { return playing; }
    }

    void Init()
    {
        if (initialized) return;
        initialized = true;
        label = JuiceText.Find(gameObject);
        if (label == null) Debug.LogWarning("[Juice] JuiceTypewriter 找不到文字元件，請掛在有 TextMeshPro 或 UI Text 的物件上。", this);
    }

    void OnEnable()
    {
        if (!playOnEnable) return;
        Init();
        if (label != null) Play(label.Text);
    }

    /// <summary>從頭開始逐字顯示 content。</summary>
    public void Play(string content)
    {
        Init();
        if (label == null) return;
        full = content ?? "";
        shown = 0;
        timer = 0f;

        // 記下每個「看得到的字」在字串裡的位置，<b> 這類標籤不算字
        visibleIndices.Clear();
        for (int i = 0; i < full.Length; i++)
        {
            if (full[i] == '<')
            {
                int close = full.IndexOf('>', i);
                if (close > i && close - i <= 40 && full.IndexOf('<', i + 1, close - i - 1) < 0)
                {
                    i = close;
                    continue;
                }
            }
            visibleIndices.Add(i);
        }

        if (label.SupportsVisibleCount)
        {
            label.Text = full;
            label.SetVisibleCount(0);
        }
        else
        {
            label.Text = "";
        }
        playing = true;
        if (visibleIndices.Count == 0) Finish();
    }

    /// <summary>立刻顯示完整句子（玩家按下跳過）。</summary>
    public void Complete()
    {
        if (playing) Finish();
    }

    void Update()
    {
        if (!playing) return;
        timer -= unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        while (playing && timer <= 0f)
        {
            char c = full[visibleIndices[shown]];
            shown++;
            timer += 1f / Mathf.Max(1f, charsPerSecond);
            if (Punctuation.IndexOf(c) >= 0) timer += punctuationPause;

            if (label.SupportsVisibleCount) label.SetVisibleCount(shown);
            else label.Text = full.Substring(0, visibleIndices[shown - 1] + 1);

            if (blip && !char.IsWhiteSpace(c) && shown % Mathf.Max(1, blipEvery) == 0)
            {
                JuiceSfx.Blip(JuiceBlip.Click, 0.18f, 0.12f, 1.3f);
            }
            if (shown >= visibleIndices.Count) Finish();
        }
    }

    void Finish()
    {
        playing = false;
        if (label != null)
        {
            if (label.SupportsVisibleCount) label.SetVisibleCount(99999);
            else label.Text = full;
        }
        onComplete.Invoke();
    }
}
