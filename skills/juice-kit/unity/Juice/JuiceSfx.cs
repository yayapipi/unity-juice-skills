using System.Collections.Generic;
using UnityEngine;

/// <summary>內建的合成音效，沒有任何音檔也能先有聲音。之後再換成自己的 AudioClip。</summary>
public enum JuiceBlip
{
    Click,
    Jump,
    Land,
    Hit,
    Hurt,
    Pickup,
    Coin,
    Explosion,
    PowerUp,
    Lose
}

/// <summary>
/// 音效：音高隨機、連擊音階、內建佔位音效。不用在場景裡放 AudioSource。
///     JuiceSfx.Play(clip);                    // 每次播放音高隨機 ±10%，連打不膩
///     JuiceSfx.PlayCombo(clip, comboCount);   // 連擊越多音越高（每次升半音，最多一個八度）
///     JuiceSfx.Blip(JuiceBlip.Jump);          // 還沒有音檔時先用合成音
/// </summary>
public static class JuiceSfx
{
    /// <summary>總音量 0–1。接到設定選單的音效滑桿。</summary>
    public static float Volume = 1f;

    /// <summary>靜音。</summary>
    public static bool Muted;

    const int PoolSize = 10;
    const int SampleRate = 44100;

    static AudioSource[] pool;
    static int nextIndex;
    static AudioClip lastClip;
    static float lastTime = -1f;
    static readonly Dictionary<JuiceBlip, AudioClip> blips = new Dictionary<JuiceBlip, AudioClip>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        pool = null;
        nextIndex = 0;
        lastClip = null;
        lastTime = -1f;
        blips.Clear();
    }

    /// <summary>播放音效。pitchJitter 0.1 = 音高在 0.9–1.1 倍之間隨機。</summary>
    public static void Play(AudioClip clip, float volume = 1f, float pitchJitter = 0.1f, float pitch = 1f)
    {
        if (clip == null || Muted || !JuiceRunner.Available) return;

        // 同一個音效在同一瞬間重複播放會爆音，擋掉
        float now = Time.unscaledTime;
        if (clip == lastClip && now - lastTime < 0.02f) return;
        lastClip = clip;
        lastTime = now;

        AudioSource source = NextSource();
        if (source == null) return;
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume * Volume);
        source.pitch = Mathf.Max(0.05f, pitch * Random.Range(1f - pitchJitter, 1f + pitchJitter));
        source.Play();
    }

    /// <summary>連擊音階：combo 0 是原音，每加 1 升半音，12 以上不再升高。</summary>
    public static void PlayCombo(AudioClip clip, int combo, float volume = 1f)
    {
        Play(clip, volume, 0f, Mathf.Pow(2f, Mathf.Clamp(combo, 0, 12) / 12f));
    }

    /// <summary>播放內建的合成音效。</summary>
    public static void Blip(JuiceBlip type, float volume = 0.5f, float pitchJitter = 0.08f, float pitch = 1f)
    {
        Play(GetBlip(type), volume, pitchJitter, pitch);
    }

    /// <summary>取得內建合成音效的 AudioClip（第一次呼叫時產生，之後重複使用）。</summary>
    public static AudioClip GetBlip(JuiceBlip type)
    {
        AudioClip clip;
        if (blips.TryGetValue(type, out clip) && clip != null) return clip;
        clip = Build(type);
        blips[type] = clip;
        return clip;
    }

    static AudioSource NextSource()
    {
        if (pool == null || pool.Length == 0 || pool[0] == null)
        {
            JuiceRunner runner = JuiceRunner.Instance;
            if (runner == null) return null;
            GameObject go = new GameObject("[Juice] Audio");
            go.transform.SetParent(runner.transform, false);
            pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                AudioSource s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                pool[i] = s;
            }
        }
        for (int i = 0; i < PoolSize; i++)
        {
            int index = (nextIndex + i) % PoolSize;
            if (!pool[index].isPlaying)
            {
                nextIndex = (index + 1) % PoolSize;
                return pool[index];
            }
        }
        AudioSource oldest = pool[nextIndex];
        nextIndex = (nextIndex + 1) % PoolSize;
        return oldest;
    }

    // ------------------------------------------------------------------ 合成器

    enum Wave { Square, Sine, Saw, Triangle }

    static AudioClip Build(JuiceBlip type)
    {
        float[] d;
        switch (type)
        {
            case JuiceBlip.Click:
                d = Buffer(0.05f);
                Tone(d, 0f, 0.05f, 1250f, 800f, 0.3f, Wave.Square);
                break;
            case JuiceBlip.Jump:
                d = Buffer(0.18f);
                Tone(d, 0f, 0.18f, 260f, 720f, 0.3f, Wave.Square);
                break;
            case JuiceBlip.Land:
                d = Buffer(0.14f);
                Noise(d, 0f, 0.12f, 0.45f, 0.25f, 0.04f);
                Tone(d, 0f, 0.14f, 150f, 55f, 0.55f, Wave.Sine);
                break;
            case JuiceBlip.Hit:
                d = Buffer(0.12f);
                Noise(d, 0f, 0.09f, 0.55f, 0.9f, 0.2f);
                Tone(d, 0f, 0.12f, 340f, 90f, 0.35f, Wave.Square);
                break;
            case JuiceBlip.Hurt:
                d = Buffer(0.26f);
                Tone(d, 0f, 0.26f, 520f, 110f, 0.32f, Wave.Saw);
                Noise(d, 0f, 0.08f, 0.3f, 0.8f, 0.3f);
                break;
            case JuiceBlip.Pickup:
                d = Buffer(0.2f);
                Tone(d, 0f, 0.07f, 880f, 880f, 0.28f, Wave.Square);
                Tone(d, 0.07f, 0.13f, 1320f, 1320f, 0.28f, Wave.Square);
                break;
            case JuiceBlip.Coin:
                d = Buffer(0.36f);
                Tone(d, 0f, 0.06f, 1047f, 1047f, 0.26f, Wave.Square);
                Tone(d, 0.06f, 0.3f, 1568f, 1568f, 0.26f, Wave.Square);
                break;
            case JuiceBlip.Explosion:
                d = Buffer(0.7f);
                Noise(d, 0f, 0.7f, 0.8f, 0.5f, 0.02f);
                Tone(d, 0f, 0.5f, 110f, 35f, 0.6f, Wave.Sine);
                break;
            case JuiceBlip.PowerUp:
                d = Buffer(0.5f);
                Tone(d, 0f, 0.08f, 523f, 523f, 0.25f, Wave.Triangle);
                Tone(d, 0.08f, 0.08f, 659f, 659f, 0.25f, Wave.Triangle);
                Tone(d, 0.16f, 0.08f, 784f, 784f, 0.25f, Wave.Triangle);
                Tone(d, 0.24f, 0.26f, 1047f, 1047f, 0.28f, Wave.Square);
                break;
            default: // Lose
                d = Buffer(0.6f);
                Tone(d, 0f, 0.16f, 440f, 440f, 0.26f, Wave.Saw);
                Tone(d, 0.16f, 0.16f, 349f, 349f, 0.26f, Wave.Saw);
                Tone(d, 0.32f, 0.28f, 262f, 196f, 0.28f, Wave.Saw);
                break;
        }
        for (int i = 0; i < d.Length; i++) d[i] = Mathf.Clamp(d[i], -1f, 1f);
        AudioClip clip = AudioClip.Create("Juice " + type, d.Length, 1, SampleRate, false);
        clip.SetData(d, 0);
        return clip;
    }

    static float[] Buffer(float seconds)
    {
        return new float[Mathf.CeilToInt(seconds * SampleRate)];
    }

    // 一個音：頻率從 f0 滑到 f1，音量很快衝上去再衰減
    static void Tone(float[] data, float start, float length, float f0, float f1, float volume, Wave wave)
    {
        int begin = Mathf.RoundToInt(start * SampleRate);
        int count = Mathf.Min(Mathf.RoundToInt(length * SampleRate), data.Length - begin);
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float p = i / (float)count;
            float freq = f0 * Mathf.Pow(f1 / f0, p);
            phase += freq / SampleRate;
            phase -= Mathf.Floor(phase);
            float s;
            switch (wave)
            {
                case Wave.Sine: s = Mathf.Sin(phase * 2f * Mathf.PI); break;
                case Wave.Saw: s = phase * 2f - 1f; break;
                case Wave.Triangle: s = 1f - 4f * Mathf.Abs(phase - 0.5f); break;
                default: s = phase < 0.5f ? 1f : -1f; break;
            }
            float attack = Mathf.Clamp01(i / (0.002f * SampleRate));
            float envelope = attack * (1f - p) * (1f - p);
            data[begin + i] += s * volume * envelope;
        }
    }

    // 雜訊：通過一個會越關越小的低通濾波器
    static void Noise(float[] data, float start, float length, float volume, float cutoffStart, float cutoffEnd)
    {
        int begin = Mathf.RoundToInt(start * SampleRate);
        int count = Mathf.Min(Mathf.RoundToInt(length * SampleRate), data.Length - begin);
        // 固定種子的簡單亂數，不去動 UnityEngine.Random 的狀態
        uint state = 2463534242u;
        float low = 0f;
        for (int i = 0; i < count; i++)
        {
            float p = i / (float)count;
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            float white = (state & 0xFFFF) / 32767.5f - 1f;
            float cutoff = Mathf.Lerp(cutoffStart, cutoffEnd, p);
            low += (white - low) * cutoff;
            data[begin + i] += low * volume * (1f - p) * (1f - p);
        }
    }
}
