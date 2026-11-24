using UnityEngine;

/// <summary>
/// 鏡頭跟隨：阻尼（慢半拍跟上）、前視（看向前進方向）、死區（小範圍移動時鏡頭不動）。
/// 掛在鏡頭上，把 target 指到玩家。2D 與 3D 都能用。
/// 已經在用 Cinemachine 的專案不需要這個元件，Cinemachine 內建同樣的功能。
/// 跟 JuiceShake 可以同時掛在同一個鏡頭上。
/// </summary>
[AddComponentMenu("Juice/Juice Follow Camera")]
public class JuiceFollowCamera : MonoBehaviour
{
    [Tooltip("要跟的物件（通常是玩家）。")]
    public Transform target;

    [Tooltip("打勾：用一開始鏡頭與目標的相對位置當作偏移。取消：用下面的 offset。")]
    public bool useStartOffset = true;

    [Tooltip("鏡頭相對目標的位置。2D 通常是 (0, 0, -10)。")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Tooltip("阻尼：大約幾秒追上目標。0 = 緊跟不放，0.1–0.25 最自然。")]
    public float smoothTime = 0.15f;

    [Tooltip("前視：往目標移動的方向多看多遠（世界單位）。0 = 關閉。")]
    public float lookAhead = 1.5f;

    [Tooltip("前視切換方向時要多久才轉過去。")]
    public float lookAheadSmoothTime = 0.35f;

    [Tooltip("死區：目標在這個範圍內移動時鏡頭不動（X、Y 各自的半徑）。(0, 0) = 關閉。")]
    public Vector2 deadZone = new Vector2(0.4f, 0.6f);

    Vector3 focus;
    Vector3 lastTargetPosition;
    Vector3 look;
    Vector3 lookVelocity;
    Vector3 velocity;
    bool initialized;

    void OnEnable()
    {
        initialized = false;
    }

    /// <summary>立刻跳到目標位置（重生、換房間時用），不要慢慢滑過去。</summary>
    public void Snap()
    {
        if (target == null) return;
        focus = target.position;
        lastTargetPosition = target.position;
        look = Vector3.zero;
        lookVelocity = Vector3.zero;
        velocity = Vector3.zero;
        transform.position = focus + offset;
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (!initialized)
        {
            initialized = true;
            if (useStartOffset) offset = transform.position - target.position;
            focus = target.position;
            lastTargetPosition = target.position;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f) return; // 頓幀或暫停時不動

        Vector3 position = target.position;

        // 死區：目標超出範圍才把焦點拉過去
        Vector3 delta = position - focus;
        if (Mathf.Abs(delta.x) > deadZone.x) focus.x = position.x - Mathf.Sign(delta.x) * deadZone.x;
        if (Mathf.Abs(delta.y) > deadZone.y) focus.y = position.y - Mathf.Sign(delta.y) * deadZone.y;
        focus.z = position.z;

        // 前視：往移動方向多看一點
        Vector3 targetVelocity = (position - lastTargetPosition) / dt;
        lastTargetPosition = position;
        Vector3 wantedLook = Vector3.zero;
        if (lookAhead > 0f && targetVelocity.sqrMagnitude > 0.25f) wantedLook = targetVelocity.normalized * lookAhead;
        look = Vector3.SmoothDamp(look, wantedLook, ref lookVelocity, lookAheadSmoothTime);

        Vector3 desired = focus + offset + look;
        transform.position = smoothTime > 0f
            ? Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime)
            : desired;
    }
}
