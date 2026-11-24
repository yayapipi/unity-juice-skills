using UnityEngine;

/// <summary>
/// 跳躍的「隱形幫手」：土狼時間、跳躍緩衝、可變跳躍高度、頂點減重力。
/// 不是元件，是放在你自己的角色控制器裡的小工具，不限 2D 或 3D、不限哪一種輸入系統：
///
///     public JuiceJumpAssist jump = new JuiceJumpAssist();
///
///     void Update()
///     {
///         jump.Tick(isGrounded);                        // 每一格都要呼叫
///         if (jumpPressedThisFrame) jump.Press();       // 按下跳躍的那一格
///         if (jump.TryJump()) { 設定往上的速度; }          // 這一格該起跳就回傳 true（只會回傳一次）
///         if (jump.ShouldCutJump(jumpHeld, velocityY)) { velocityY *= jump.jumpCutMultiplier; }
///         重力 *= jump.GravityMultiplier(velocityY, jumpHeld);
///     }
/// </summary>
[System.Serializable]
public class JuiceJumpAssist
{
    [Tooltip("土狼時間：離開平台後多少秒內還可以跳。Celeste 是 0.1。")]
    public float coyoteTime = 0.1f;

    [Tooltip("跳躍緩衝：落地前多少秒內按下的跳躍，落地時會自動補跳。0.1–0.2。")]
    public float bufferTime = 0.15f;

    [Tooltip("可變跳躍高度：上升途中放開跳躍鍵時，往上的速度乘以這個值。0.5 = 砍半。")]
    public float jumpCutMultiplier = 0.5f;

    [Tooltip("頂點減重力：垂直速度的絕對值小於這個數字、而且還按著跳躍鍵時，視為在頂點附近。")]
    public float apexThreshold = 1.5f;

    [Tooltip("頂點附近的重力倍率。0.5 = 重力減半，多一點時間調整落點。")]
    public float apexGravityMultiplier = 0.5f;

    [Tooltip("下落時的重力倍率。大於 1 會落得比較快，跳躍比較不飄。")]
    public float fallGravityMultiplier = 1.5f;

    float lastGroundedTime = -999f;
    float lastPressTime = -999f;
    float lastJumpTime = -999f;
    bool leavingGround;
    bool cutUsed;

    /// <summary>每一格（或每個 FixedUpdate）都要呼叫，告訴它現在有沒有踩在地上。</summary>
    public void Tick(bool grounded)
    {
        if (!grounded)
        {
            leavingGround = false;
            return;
        }
        // 剛起跳的那幾格，地面偵測通常還碰得到地。這時候不能算成「又踩到地」，否則可以在空中再跳一次。
        // 0.2 秒後還在地上，代表這次起跳沒有成功離地（例如頭頂被擋住），恢復正常。
        if (leavingGround && Time.time - lastJumpTime < 0.2f) return;
        leavingGround = false;
        lastGroundedTime = Time.time;
    }

    /// <summary>玩家按下跳躍鍵的那一格呼叫。</summary>
    public void Press()
    {
        lastPressTime = Time.time;
    }

    /// <summary>
    /// 現在該不該起跳。條件：最近按過跳躍（緩衝內）而且最近踩過地（土狼時間內）。
    /// 回傳 true 的同時會把兩個計時清掉，所以一次按鍵只會跳一次。
    /// </summary>
    public bool TryJump()
    {
        float now = Time.time;
        if (now - lastPressTime > bufferTime) return false;
        if (now - lastGroundedTime > coyoteTime) return false;
        lastPressTime = -999f;
        lastGroundedTime = -999f;
        lastJumpTime = now;
        leavingGround = true;
        cutUsed = false;
        return true;
    }

    /// <summary>上升途中放開了跳躍鍵 → 該把往上的速度砍掉（每次跳躍只會回傳一次 true）。</summary>
    public bool ShouldCutJump(bool jumpHeld, float verticalVelocity)
    {
        if (cutUsed || jumpHeld || verticalVelocity <= 0f) return false;
        cutUsed = true;
        return true;
    }

    /// <summary>這一格重力要乘多少：頂點附近變輕，下落時變重，其他時候是 1。</summary>
    public float GravityMultiplier(float verticalVelocity, bool jumpHeld)
    {
        if (jumpHeld && Mathf.Abs(verticalVelocity) < apexThreshold) return apexGravityMultiplier;
        if (verticalVelocity < 0f) return fallGravityMultiplier;
        return 1f;
    }
}
