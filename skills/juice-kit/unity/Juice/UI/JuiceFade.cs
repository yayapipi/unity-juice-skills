using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 淡出淡入轉場，不用自己做黑幕。需要 Unity UI（uGUI）。
///     JuiceFade.LoadScene("Level2");            // 淡出 → 換場景 → 淡入
///     JuiceFade.Out(0.4f, Color.black, () => { /* 全黑的時候做事 */ JuiceFade.In(); });
/// 淡出期間會擋住下面的 UI 點擊。用 unscaled time。
/// </summary>
public static class JuiceFade
{
    static Image image;
    static Coroutine routine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        image = null;
        routine = null;
    }

    /// <summary>畫面目前被遮住多少（0 = 完全看得到，1 = 全黑）。</summary>
    public static float Amount
    {
        get { return image != null ? image.color.a : 0f; }
    }

    /// <summary>淡出到純色。done 會在完全遮住時呼叫。</summary>
    public static void Out(float duration = 0.4f, Color? color = null, Action done = null)
    {
        Run(1f, duration, color, done);
    }

    /// <summary>從純色淡入回遊戲畫面。</summary>
    public static void In(float duration = 0.4f, Action done = null)
    {
        Run(0f, duration, null, done);
    }

    /// <summary>淡出、載入場景、再淡入。場景要先加進 Build Settings 的場景清單。</summary>
    public static void LoadScene(string sceneName, float duration = 0.4f, Color? color = null)
    {
        Out(duration, color, delegate
        {
            SceneManager.LoadScene(sceneName);
            In(duration);
        });
    }

    static void Run(float target, float duration, Color? color, Action done)
    {
        if (!JuiceRunner.Available || !Ensure()) return;
        if (routine != null) JuiceRunner.Stop(routine);

        Color c = image.color;
        if (color.HasValue)
        {
            c.r = color.Value.r;
            c.g = color.Value.g;
            c.b = color.Value.b;
        }
        float from = c.a;
        image.raycastTarget = true;
        routine = JuiceRunner.Tween(duration, delegate(float e)
        {
            if (image == null) return;
            c.a = Mathf.Lerp(from, target, e);
            image.color = c;
        }, JuiceEaseType.InOutQuad, true, delegate
        {
            routine = null;
            if (image != null) image.raycastTarget = target > 0.01f;
            if (done != null) done();
        });
    }

    static bool Ensure()
    {
        if (image != null) return true;
        JuiceRunner runner = JuiceRunner.Instance;
        if (runner == null) return false;

        GameObject canvasObject = new GameObject("[Juice] Fade");
        canvasObject.transform.SetParent(runner.transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject imageObject = new GameObject("Image");
        imageObject.transform.SetParent(canvasObject.transform, false);
        image = imageObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return true;
    }
}
