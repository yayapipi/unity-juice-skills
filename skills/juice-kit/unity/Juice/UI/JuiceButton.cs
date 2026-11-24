using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 按鈕手感：滑過放大、按下縮小、放開彈回，還有「喀」一聲。掛在 UI 按鈕上就會動，不用寫程式。
/// 需要 Unity UI（uGUI）。用 unscaled time，暫停選單裡也會動。
/// </summary>
[AddComponentMenu("Juice/Juice Button")]
public class JuiceButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [Tooltip("滑鼠移上去（或手把選到）時的大小。")]
    public float hoverScale = 1.05f;

    [Tooltip("按下時的大小。")]
    public float pressScale = 0.95f;

    [Tooltip("彈簧的硬度。越大彈得越快。")]
    public float stiffness = 420f;

    [Tooltip("彈簧的阻尼。越小抖越久。")]
    public float damping = 18f;

    [Tooltip("按下時播放聲音。")]
    public bool playSound = true;

    [Tooltip("自己的按鈕音效。留空就用內建的合成音。")]
    public AudioClip clickClip;

    Selectable selectable;
    Vector3 baseScale = Vector3.one;
    bool hasBase;
    bool hovered;
    bool pressed;
    float current = 1f;
    float velocity;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
        baseScale = transform.localScale;
        hasBase = true;
    }

    void OnDisable()
    {
        hovered = false;
        pressed = false;
        current = 1f;
        velocity = 0f;
        if (hasBase) transform.localScale = baseScale;
    }

    bool Interactable
    {
        get { return selectable == null || selectable.IsInteractable(); }
    }

    public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; }
    public void OnSelect(BaseEventData eventData) { hovered = true; }
    public void OnDeselect(BaseEventData eventData) { hovered = false; }
    public void OnPointerUp(PointerEventData eventData) { pressed = false; }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!Interactable) return;
        pressed = true;
        if (!playSound) return;
        if (clickClip != null) JuiceSfx.Play(clickClip, 0.8f, 0.05f);
        else JuiceSfx.Blip(JuiceBlip.Click, 0.35f, 0.05f);
    }

    void Update()
    {
        float target = 1f;
        if (Interactable)
        {
            if (pressed) target = pressScale;
            else if (hovered) target = hoverScale;
        }
        if (Mathf.Approximately(current, target) && Mathf.Abs(velocity) < 0.001f) return;

        // 有一點彈過頭的彈簧
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
        velocity += (target - current) * stiffness * dt;
        velocity *= Mathf.Exp(-damping * dt);
        current += velocity * dt;
        transform.localScale = baseScale * current;
    }
}
