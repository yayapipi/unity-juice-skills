using UnityEngine;

/// <summary>
/// 無障礙設定：讓玩家關掉震動與閃光，或把整體強度調低。會自動存進 PlayerPrefs。
/// 在設定選單裡接上：JuiceSettings.ScreenShake = toggle.isOn;
/// </summary>
public static class JuiceSettings
{
    const string KeyShake = "Juice.ScreenShake";
    const string KeyFlash = "Juice.Flashes";
    const string KeyIntensity = "Juice.Intensity";

    static bool loaded;
    static bool screenShake = true;
    static bool flashes = true;
    static float intensity = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        loaded = false;
    }

    /// <summary>鏡頭震動、後座與縮放。關掉後 JuiceShake 完全不動。</summary>
    public static bool ScreenShake
    {
        get { Load(); return screenShake; }
        set { Load(); screenShake = value; PlayerPrefs.SetInt(KeyShake, value ? 1 : 0); }
    }

    /// <summary>閃白。關掉後 JuiceFlash 不會換材質。</summary>
    public static bool Flashes
    {
        get { Load(); return flashes; }
        set { Load(); flashes = value; PlayerPrefs.SetInt(KeyFlash, value ? 1 : 0); }
    }

    /// <summary>整體強度 0–1。影響震動幅度、頓幀長度與粒子數量。</summary>
    public static float Intensity
    {
        get { Load(); return intensity; }
        set { Load(); intensity = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KeyIntensity, intensity); }
    }

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        screenShake = PlayerPrefs.GetInt(KeyShake, 1) != 0;
        flashes = PlayerPrefs.GetInt(KeyFlash, 1) != 0;
        intensity = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyIntensity, 1f));
    }
}
