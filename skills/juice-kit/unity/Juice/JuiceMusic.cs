using System.Collections;
using UnityEngine;

/// <summary>
/// 背景音樂：淡入、淡出、換歌時交叉淡化。還沒有音樂檔的時候，有一段內建的合成循環可以先用。
///     JuiceMusic.Play();               // 內建的合成循環（16 秒，120 BPM）
///     JuiceMusic.Play(myClip);         // 自己的音樂。正在播別首的話會交叉淡化
///     JuiceMusic.Stop();               // 淡出
///     JuiceMusic.Volume = 0.4f;        // 音樂音量，接到設定選單的滑桿
///     JuiceMusic.Duck(0.3f, 0.6f);     // 暫時壓低音量（大爆炸、對話、過關音效）
///     float pulse = JuiceMusic.BeatPulse;   // 每一拍從 1 掉到 0，拿來讓畫面跟著節拍動
/// 不用在場景裡放 AudioSource。
/// </summary>
public static class JuiceMusic
{
    /// <summary>內建循環的速度。</summary>
    public const float BuiltInBpm = 120f;

    /// <summary>音樂音量 0–1。</summary>
    public static float Volume = 0.5f;

    /// <summary>靜音。</summary>
    public static bool Muted;

    /// <summary>正在播的音樂每分鐘幾拍。BeatPulse 用它來算節拍；播自己的音樂時請在 Play 傳入。</summary>
    public static float Bpm = BuiltInBpm;

    static AudioSource current, previous;
    static float currentLevel, previousLevel, targetLevel;
    static float fadeSeconds = 0.5f;
    static float duck = 1f, duckTarget = 1f, duckUntil;
    static AudioClip builtIn;
    static bool driving;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        current = null;
        previous = null;
        currentLevel = previousLevel = targetLevel = 0f;
        duck = duckTarget = 1f;
        duckUntil = 0f;
        builtIn = null;
        driving = false;
        Bpm = BuiltInBpm;
    }

    /// <summary>正在播放（包含淡入中）。</summary>
    public static bool IsPlaying
    {
        get { return current != null && current.isPlaying && targetLevel > 0f; }
    }

    /// <summary>每一拍從 1 掉到 0。沒有在播的時候是 0。</summary>
    public static float BeatPulse
    {
        get
        {
            if (!IsPlaying || current.clip == null || Bpm <= 0f) return 0f;
            float beats = (float)current.timeSamples / current.clip.frequency * Bpm / 60f;
            float rest = 1f - (beats - Mathf.Floor(beats));
            return rest * rest;
        }
    }

    /// <summary>內建的合成循環。第一次使用時才產生。</summary>
    public static AudioClip BuiltInLoop
    {
        get
        {
            if (builtIn == null) builtIn = BuildLoop();
            return builtIn;
        }
    }

    /// <summary>播放音樂。clip 留空 = 內建循環；fade = 淡入秒數；bpm = 這首歌的速度（不知道就不用填）。</summary>
    public static void Play(AudioClip clip = null, float fade = 0.5f, float bpm = 0f)
    {
        if (!Application.isPlaying) return;
        if (clip == null)
        {
            clip = BuiltInLoop;
            bpm = BuiltInBpm;
        }
        if (!EnsureSources()) return;

        fadeSeconds = Mathf.Max(0.0001f, fade);
        targetLevel = 1f;
        if (bpm > 0f) Bpm = bpm;
        if (current.clip != clip || !current.isPlaying)
        {
            // 把正在播的換到另一軌淡出，新的從 0 淡入
            AudioSource swap = previous;
            previous = current;
            current = swap;
            previousLevel = currentLevel;
            currentLevel = fade <= 0f ? 1f : 0f;
            current.clip = clip;
            current.loop = true;
            current.volume = 0f;
            current.Play();
        }
        Drive();
    }

    /// <summary>淡出並停止。</summary>
    public static void Stop(float fade = 0.5f)
    {
        if (current == null) return;
        fadeSeconds = Mathf.Max(0.0001f, fade);
        targetLevel = 0f;
        Drive();
    }

    /// <summary>把音量暫時壓到 level 倍，seconds 秒後自己回來。</summary>
    public static void Duck(float level = 0.35f, float seconds = 0.5f)
    {
        duckTarget = Mathf.Clamp01(level);
        duckUntil = Time.unscaledTime + seconds;
        Drive();
    }

    static bool EnsureSources()
    {
        if (current != null && previous != null) return true;
        JuiceRunner runner = JuiceRunner.Instance;
        if (runner == null) return false;
        GameObject go = new GameObject("[Juice] Music");
        go.transform.SetParent(runner.transform, false);
        current = NewSource(go);
        previous = NewSource(go);
        return true;
    }

    static AudioSource NewSource(GameObject go)
    {
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = true;
        source.priority = 0;
        source.volume = 0f;
        return source;
    }

    static void Drive()
    {
        if (driving) return;
        if (JuiceRunner.Run(Driver()) != null) driving = true;
    }

    static IEnumerator Driver()
    {
        while (current != null)
        {
            float dt = Time.unscaledDeltaTime;
            float step = dt / fadeSeconds;
            currentLevel = Mathf.MoveTowards(currentLevel, targetLevel, step);
            previousLevel = Mathf.MoveTowards(previousLevel, 0f, step);
            if (Time.unscaledTime >= duckUntil) duckTarget = 1f;
            duck = Mathf.MoveTowards(duck, duckTarget, dt * 6f);

            float master = Muted ? 0f : Mathf.Clamp01(Volume) * duck;
            current.volume = currentLevel * master;
            if (previous != null)
            {
                previous.volume = previousLevel * master;
                if (previousLevel <= 0f && previous.isPlaying) previous.Stop();
            }
            if (targetLevel <= 0f && currentLevel <= 0f)
            {
                current.Stop();
                break;
            }
            yield return null;
        }
        driving = false;
    }

    // ------------------------------------------------------------------ 內建循環

    // 八小節：Am F C G 走兩次。大鼓、小鼓、腳踏鈸、貝斯、琶音，後四小節多一條旋律。
    static AudioClip BuildLoop()
    {
        const int rate = 22050;
        const int bars = 8;
        float stepSeconds = 60f / BuiltInBpm / 4f;      // 一個十六分音符
        int steps = bars * 16;
        int total = Mathf.RoundToInt(steps * stepSeconds * rate);
        float[] d = new float[total];

        float[] roots = { 110.00f, 87.31f, 130.81f, 98.00f };
        float[][] chords =
        {
            new[] { 440.00f, 523.25f, 659.25f },
            new[] { 349.23f, 440.00f, 523.25f },
            new[] { 392.00f, 523.25f, 659.25f },
            new[] { 392.00f, 493.88f, 587.33f }
        };
        float[][] lead =
        {
            new[] { 659.25f, 0f, 659.25f, 587.33f, 523.25f, 0f, 440.00f, 0f },
            new[] { 523.25f, 0f, 440.00f, 0f, 523.25f, 587.33f, 523.25f, 0f },
            new[] { 659.25f, 0f, 783.99f, 659.25f, 587.33f, 0f, 523.25f, 0f },
            new[] { 587.33f, 0f, 493.88f, 0f, 587.33f, 659.25f, 587.33f, 0f }
        };
        int[] pattern = { 0, 1, 2, 1, 0, 2, 1, 2 };
        System.Random random = new System.Random(7);

        for (int step = 0; step < steps; step++)
        {
            int bar = step / 16;
            int inBar = step % 16;
            int chord = bar % 4;
            int start = Mathf.RoundToInt(step * stepSeconds * rate);

            if (inBar % 4 == 0) Kick(d, start, rate);
            if (inBar == 4 || inBar == 12) Snare(d, start, rate, random);
            if (inBar % 4 == 2) Hat(d, start, rate, random, 0.075f);
            else if (bar >= 4 && inBar % 2 == 1) Hat(d, start, rate, random, 0.03f);

            if (inBar % 2 == 0)
                Tone(d, start, rate, roots[chord] * (inBar % 4 == 2 ? 2f : 1f), stepSeconds * 1.8f, 0.2f, 5, 0.3f);

            float arp = chords[chord][pattern[inBar % 8]] * (inBar >= 8 ? 2f : 1f);
            Tone(d, start, rate, arp, stepSeconds * 0.95f, 0.06f, 5, 0.07f);

            if (bar >= 4 && inBar % 2 == 0)
            {
                float note = lead[chord][inBar / 2];
                if (note > 0f) Tone(d, start, rate, note, stepSeconds * 1.9f, 0.1f, 3, 0.22f);
            }
        }

        // 柔性限幅，再把最大音量拉到 0.85
        float peak = 0.0001f;
        for (int i = 0; i < total; i++)
        {
            d[i] = (float)System.Math.Tanh(d[i] * 1.15f);
            float a = Mathf.Abs(d[i]);
            if (a > peak) peak = a;
        }
        float gain = 0.85f / peak;
        for (int i = 0; i < total; i++) d[i] *= gain;

        AudioClip clip = AudioClip.Create("Juice Music Loop", total, 1, rate, false);
        clip.SetData(d, 0);
        return clip;
    }

    // 方波感的音色：只加奇數泛音，高到會失真的泛音不加。尾巴會繞回開頭，循環才接得起來。
    static void Tone(float[] d, int start, int rate, float frequency, float seconds, float volume, int maxHarmonic, float decay)
    {
        int length = Mathf.RoundToInt(seconds * rate);
        int total = d.Length;
        float limit = rate * 0.42f;
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / rate;
            float envelope = Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-t / decay) * Mathf.Min(1f, (length - i) / (0.008f * rate));
            float phase = 2f * Mathf.PI * frequency * t;
            float sample = 0f;
            for (int k = 1; k <= maxHarmonic && frequency * k < limit; k += 2) sample += Mathf.Sin(phase * k) / k;
            d[(start + i) % total] += sample * envelope * volume;
        }
    }

    static void Kick(float[] d, int start, int rate)
    {
        int length = Mathf.RoundToInt(0.24f * rate);
        int total = d.Length;
        float phase = 0f;
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / rate;
            phase += 2f * Mathf.PI * (48f + 120f * Mathf.Exp(-t / 0.025f)) / rate;
            d[(start + i) % total] += Mathf.Sin(phase) * Mathf.Exp(-t / 0.11f) * 0.55f;
        }
    }

    static void Snare(float[] d, int start, int rate, System.Random random)
    {
        int length = Mathf.RoundToInt(0.16f * rate);
        int total = d.Length;
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / rate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            float body = Mathf.Sin(2f * Mathf.PI * 190f * t) * Mathf.Exp(-t / 0.04f) * 0.12f;
            d[(start + i) % total] += noise * Mathf.Exp(-t / 0.05f) * 0.15f + body;
        }
    }

    static void Hat(float[] d, int start, int rate, System.Random random, float volume)
    {
        int length = Mathf.RoundToInt(0.05f * rate);
        int total = d.Length;
        float last = 0f;
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / rate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            d[(start + i) % total] += (noise - last) * Mathf.Exp(-t / 0.012f) * volume;
            last = noise;
        }
    }
}
