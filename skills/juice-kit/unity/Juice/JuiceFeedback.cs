using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 一個事件的所有回饋，集中在一個元件上、全部在 Inspector 調整。
/// 做法：一個事件放一個 JuiceFeedback（例如 HitFeedback、JumpFeedback），在程式裡呼叫 Play()。
///
///     public JuiceFeedback hitFeedback;
///     void OnHit(Vector3 point) { hitFeedback.PlayAt(point); }
///
/// 執行中在元件右上角的「⋮」選單選「Play（測試）」可以直接試。
/// 也可以不寫程式：把 Play 拖進按鈕的 OnClick 或任何 UnityEvent。
/// </summary>
[AddComponentMenu("Juice/Juice Feedback")]
public class JuiceFeedback : MonoBehaviour
{
    public enum ScaleEffect { None, Squash, Stretch, Punch }
    public enum SoundSource { None, Clip, BuiltInBlip }

    [Header("目標")]
    [Tooltip("要變形、閃白的物件。留空 = 這個物件。")]
    public Transform target;

    [Header("時間")]
    [Tooltip("頓幀秒數。0 = 不用。一般命中 0.05–0.08。")]
    public float hitStop;
    [Tooltip("慢動作倍率。1 = 不用。0.3 = 放慢到三成。")]
    [Range(0.05f, 1f)] public float slowMotion = 1f;
    [Tooltip("慢動作維持幾秒（真實時間）。")]
    public float slowMotionDuration = 0.4f;

    [Header("鏡頭")]
    [Tooltip("鏡頭震動 0–1。0 = 不用。輕 0.2、中 0.4、重 0.6。")]
    [Range(0f, 1f)] public float shake;
    [Tooltip("鏡頭推近的比例。0 = 不用。0.05 = 推近 5% 再彈回。")]
    [Range(0f, 0.3f)] public float zoomPunch;

    [Header("形變")]
    public ScaleEffect scale = ScaleEffect.None;
    [Tooltip("變形的幅度。0.25 = 25%。")]
    public float scaleAmount = 0.25f;

    [Header("閃白")]
    public bool flash;
    public Color flashColor = Color.white;
    public float flashDuration = 0.06f;

    [Header("粒子")]
    [Tooltip("噴幾顆粒子。0 = 不用。")]
    public int particles;
    public Color particleColor = new Color(1f, 0.87f, 0.35f);
    public float particleSpeed = 6f;
    public float particleSize = 0.18f;

    [Header("聲音")]
    public SoundSource sound = SoundSource.None;
    public AudioClip clip;
    public JuiceBlip blip = JuiceBlip.Hit;
    [Range(0f, 1f)] public float volume = 0.6f;
    [Tooltip("音高隨機的範圍。0.1 = 0.9–1.1 倍。")]
    [Range(0f, 0.5f)] public float pitchJitter = 0.1f;

    [Header("飄字")]
    [Tooltip("要飄出來的文字。留空 = 不用。")]
    public string floatingText = "";
    public Color floatingTextColor = Color.white;

    [Header("其他")]
    [Tooltip("播放時一起觸發的事件，可以接動畫、開關物件等等。")]
    public UnityEvent onPlay = new UnityEvent();

    /// <summary>在目標的位置播放全部回饋。</summary>
    [ContextMenu("Play（測試）")]
    public void Play()
    {
        Transform t = target != null ? target : transform;
        PlayAt(t.position);
    }

    /// <summary>在指定位置播放（粒子與飄字出現在 position，例如命中點）。</summary>
    public void PlayAt(Vector3 position)
    {
        if (!Application.isPlaying) return;
        Transform t = target != null ? target : transform;

        if (scale == ScaleEffect.Squash) JuiceSquash.Squash(t, scaleAmount);
        else if (scale == ScaleEffect.Stretch) JuiceSquash.Stretch(t, scaleAmount);
        else if (scale == ScaleEffect.Punch) JuiceSquash.Punch(t, scaleAmount);

        if (flash) JuiceFlash.Play(t.gameObject, flashColor, flashDuration);
        if (particles > 0) JuiceBurst.Spawn(position, particleColor, particles, particleSpeed, particleSize);

        if (sound == SoundSource.Clip) JuiceSfx.Play(clip, volume, pitchJitter);
        else if (sound == SoundSource.BuiltInBlip) JuiceSfx.Blip(blip, volume, pitchJitter);

        if (!string.IsNullOrEmpty(floatingText)) JuiceFloatingText.Spawn(position, floatingText, floatingTextColor);

        if (shake > 0f) JuiceShake.Add(shake);
        if (zoomPunch > 0f) JuiceShake.Zoom(zoomPunch);
        if (slowMotion < 0.999f) JuiceTime.SlowMo(slowMotion, slowMotionDuration);
        if (hitStop > 0f) JuiceTime.HitStop(hitStop);

        onPlay.Invoke();
    }
}
