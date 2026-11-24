using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Juice Kit 的幕後物件，負責跑協程與補間。第一次用到時會自動建立，不用手動放進場景。
/// </summary>
[AddComponentMenu("")]
public class JuiceRunner : MonoBehaviour
{
    static JuiceRunner instance;
    static bool quitting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        quitting = false;
    }

    /// <summary>只有在遊戲執行中才能用（編輯模式或正在關閉時為 false）。</summary>
    public static bool Available
    {
        get { return Application.isPlaying && !quitting; }
    }

    public static JuiceRunner Instance
    {
        get
        {
            if (!Available) return null;
            if (instance == null)
            {
                GameObject go = new GameObject("[Juice]");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<JuiceRunner>();
            }
            return instance;
        }
    }

    void OnApplicationQuit()
    {
        quitting = true;
    }

    public static Coroutine Run(IEnumerator routine)
    {
        JuiceRunner runner = Instance;
        return runner != null ? runner.StartCoroutine(routine) : null;
    }

    public static void Stop(Coroutine routine)
    {
        if (routine != null && instance != null) instance.StopCoroutine(routine);
    }

    /// <summary>
    /// 最小的補間：在 duration 秒內，每一格用緩動後的 0–1 呼叫 step。
    /// 例：JuiceRunner.Tween(0.3f, e => panel.alpha = e);
    /// </summary>
    public static Coroutine Tween(float duration, Action<float> step,
        JuiceEaseType ease = JuiceEaseType.OutCubic, bool unscaled = false, Action done = null)
    {
        return Run(TweenRoutine(duration, step, ease, unscaled, done));
    }

    static IEnumerator TweenRoutine(float duration, Action<float> step, JuiceEaseType ease, bool unscaled, Action done)
    {
        if (duration > 0f)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += (unscaled ? Time.unscaledDeltaTime : Time.deltaTime) / duration;
                if (step != null) step(t >= 1f ? 1f : JuiceEase.Evaluate(ease, t));
                if (t < 1f) yield return null;
            }
        }
        else if (step != null)
        {
            step(1f);
        }
        if (done != null) done();
    }
}
