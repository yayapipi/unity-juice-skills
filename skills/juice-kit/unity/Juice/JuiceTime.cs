using System.Collections;
using UnityEngine;

/// <summary>
/// 時間類的回饋：頓幀與慢動作。
///     JuiceTime.HitStop(0.06f);        // 命中瞬間停 0.06 秒
///     JuiceTime.SlowMo(0.3f, 0.5f);    // 放慢到 0.3 倍，維持 0.5 秒（真實時間）再回復
///     JuiceTime.Cancel();              // 立刻結束頓幀與慢動作（打開暫停選單前呼叫）
/// 會自己處理重疊呼叫與 fixedDeltaTime。遊戲已經暫停（timeScale = 0）時不會動它。
/// </summary>
public static class JuiceTime
{
    static bool stopping;
    static int stopVersion;
    static float stopUntil;
    static float resumeScale = 1f;

    static bool slowing;
    static int slowVersion;
    static float slowBaseScale = 1f;
    static float baseFixedDelta = 0.02f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        stopping = false;
        stopVersion = 0;
        slowing = false;
        slowVersion = 0;
    }

    /// <summary>現在是否正在頓幀。</summary>
    public static bool IsStopped
    {
        get { return stopping; }
    }

    /// <summary>
    /// 頓幀：把時間停 seconds 秒（真實時間）。一般命中 0.05–0.08，重擊 0.1–0.15，大招最多 0.2。
    /// 頓幀期間再呼叫會延長，不會疊加。
    /// </summary>
    public static void HitStop(float seconds = 0.06f)
    {
        if (!JuiceRunner.Available) return;
        seconds *= JuiceSettings.Intensity;
        if (seconds <= 0f) return;

        float until = Time.realtimeSinceStartup + seconds;
        if (stopping)
        {
            if (until > stopUntil) stopUntil = until;
            return;
        }
        if (Time.timeScale <= 0f) return; // 遊戲被別人暫停了，不要插手

        stopUntil = until;
        resumeScale = Time.timeScale;
        stopping = true;
        stopVersion++;
        Time.timeScale = 0f;
        JuiceRunner.Run(StopRoutine(stopVersion));
    }

    static IEnumerator StopRoutine(int version)
    {
        while (Time.realtimeSinceStartup < stopUntil)
        {
            if (version != stopVersion) yield break;
            yield return null;
        }
        if (version != stopVersion) yield break;
        stopping = false;
        // 頓幀期間如果有人改過 timeScale（例如打開暫停選單又關掉），就尊重他的值
        if (Time.timeScale == 0f) Time.timeScale = resumeScale;
    }

    /// <summary>
    /// 立刻結束進行中的頓幀與慢動作，把時間還原。
    /// 專案有暫停選單時，在暫停前呼叫：JuiceTime.Cancel(); Time.timeScale = 0f;
    /// 這樣剛好在頓幀中按下暫停，也不會被頓幀結束時「解除暫停」。
    /// </summary>
    public static void Cancel()
    {
        if (stopping)
        {
            stopping = false;
            stopVersion++;
            Time.timeScale = resumeScale;
        }
        if (slowing)
        {
            slowing = false;
            slowVersion++;
            Time.timeScale = slowBaseScale;
            Time.fixedDeltaTime = baseFixedDelta;
        }
    }

    /// <summary>
    /// 慢動作：放慢到 scale 倍，維持 duration 秒（真實時間），再用 easeBack 秒回到原速。
    /// 建議 scale 0.2–0.5、duration 0.3–1。
    /// </summary>
    public static void SlowMo(float scale = 0.3f, float duration = 0.5f, float easeBack = 0.25f)
    {
        if (!JuiceRunner.Available) return;
        scale = Mathf.Clamp(scale, 0.01f, 1f);
        if (!slowing)
        {
            float current = stopping ? resumeScale : Time.timeScale;
            if (current <= 0f) return; // 暫停中
            slowBaseScale = current;
            baseFixedDelta = Time.fixedDeltaTime;
        }
        slowing = true;
        slowVersion++;
        JuiceRunner.Run(SlowRoutine(slowVersion, scale, Mathf.Max(0f, duration), Mathf.Max(0f, easeBack)));
    }

    static IEnumerator SlowRoutine(int version, float scale, float duration, float easeBack)
    {
        SetScale(scale);
        float end = Time.realtimeSinceStartup + duration;
        while (Time.realtimeSinceStartup < end)
        {
            if (version != slowVersion) yield break;
            yield return null;
        }
        float t = 0f;
        while (t < 1f && easeBack > 0f)
        {
            if (version != slowVersion) yield break;
            t += Time.unscaledDeltaTime / easeBack;
            SetScale(Mathf.Lerp(scale, 1f, Mathf.Clamp01(t)));
            yield return null;
        }
        if (version != slowVersion) yield break;
        SetScale(1f);
        slowing = false;
    }

    // relative：相對於慢動作開始前的速度（1 = 原速）
    static void SetScale(float relative)
    {
        float scale = slowBaseScale * relative;
        if (stopping) resumeScale = scale;
        else Time.timeScale = scale;
        // 物理步長跟著縮，慢動作才不會一格一格跳
        Time.fixedDeltaTime = baseFixedDelta * relative;
    }
}
