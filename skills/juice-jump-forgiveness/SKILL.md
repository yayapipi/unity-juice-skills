---
name: juice-jump-forgiveness
description: 讓 Unity 平台跳躍角色的操作更順、更寬容：土狼時間（coyote time）、跳躍緩衝（jump buffer）、可變跳躍高度、頂點減重力、下落加速。這些是 Celeste 等遊戲「偷偷幫玩家」的設計。使用者說「我明明有按跳卻沒跳」「跳躍很飄」「跳躍手感很差」「操作不順」「平台遊戲手感」，或提到 coyote time、jump buffer、variable jump height 時使用。
license: MIT
metadata:
  author: yayapipi
  version: "1.0.0"
---

# 跳躍的隱形幫手

玩家不會發現這些設計，只會覺得「操作好順」。沒有它們，玩家會一直覺得「我明明有按」。

## 前置

專案裡要有 `Assets/Juice/`（Juice Kit）。沒有的話先照 `juice-kit` Skill 安裝。

這個 Skill 跟其他的不一樣：它要**修改專案現有的角色控制器**。先把控制器完整讀過，弄清楚三件事：哪裡判斷「在地上」、哪裡讀「跳躍鍵」、哪裡給「往上的速度」。

## 四個機制

| 機制 | 做什麼 | 起手值 |
|---|---|---|
| 土狼時間 | 離開平台後的一小段時間內還可以起跳 | 0.1 秒（Celeste 的數值） |
| 跳躍緩衝 | 落地前稍早按下的跳躍，落地那一刻自動補跳 | 0.15 秒 |
| 可變跳躍高度 | 輕按跳得低、按住跳得高：上升途中放開按鍵就把上升速度砍半 | 乘 0.5 |
| 頂點減重力、下落加速 | 最高點附近重力減半，多一點時間調整落點；下落時重力加重，不會飄 | 0.5 倍、1.5 倍 |

## 做法

`JuiceJumpAssist` 不是元件，是放進控制器裡的小工具，不限 2D 或 3D、不限哪一種輸入系統。

1. 在控制器加一個欄位（數值會出現在 Inspector）：

   ```csharp
   public JuiceJumpAssist jump = new JuiceJumpAssist();
   ```

2. 把原本「在地上而且按下跳躍就起跳」的判斷，換成下面的形式。以 2D 的 `Rigidbody2D` 為例：

   ```csharp
   void Update()
   {
       jump.Tick(isGrounded);                          // 每一格都告訴它有沒有踩在地上
       if (jumpPressedThisFrame) jump.Press();         // 按下跳躍的那一格

       Vector2 v = rb.linearVelocity;                  // Unity 6 之前的版本是 rb.velocity

       if (jump.TryJump())                             // 取代原本的 if (isGrounded && jumpPressed)
       {
           v.y = jumpSpeed;
           Juice.Jump(transform);                      // 順便加上起跳的回饋（可以不加）
       }

       if (jump.ShouldCutJump(jumpHeld, v.y))          // 上升途中放開按鍵
           v.y *= jump.jumpCutMultiplier;

       rb.linearVelocity = v;
       rb.gravityScale = baseGravityScale * jump.GravityMultiplier(v.y, jumpHeld);
   }
   ```

   - `jumpPressedThisFrame`：這一格剛按下（Input System 是 `action.WasPressedThisFrame()`，舊輸入是 `Input.GetButtonDown("Jump")`）。
   - `jumpHeld`：目前按著（`action.IsPressed()` 或 `Input.GetButton("Jump")`）。
   - `baseGravityScale`：在 `Awake` 記下原本的 `rb.gravityScale`。
   - 3D 的 `Rigidbody` 沒有 `gravityScale`：關掉 `useGravity`，在 `FixedUpdate` 自己加 `rb.AddForce(Physics.gravity * 倍率, ForceMode.Acceleration);`。
   - `CharacterController` 或自己算速度的控制器：把自己的重力值乘上 `GravityMultiplier` 的結果。

3. **保留專案原本的起跳方式與跳躍高度**。原本用 `AddForce` 起跳就照用，只把「能不能跳」的判斷換成 `jump.TryJump()`。

4. 專案原本是在 `FixedUpdate` 讀輸入的話，把「讀按鍵並呼叫 `jump.Press()`」移到 `Update`，否則按鍵偶爾會漏掉；`TryJump` 與設定速度可以留在 `FixedUpdate`。

5. 確認能編譯。請使用者試三件事：走出平台邊緣之後才按跳（應該跳得起來）、快落地時提早按跳（落地瞬間應該自動起跳）、輕按與長按（高度應該不同）。

## 二段跳、蹬牆跳

`TryJump()` 只管「從地面起跳」的那一次。專案有二段跳時：先呼叫 `TryJump()`；回傳 false 而且玩家這一格按了跳、還有空中跳躍次數，再走原本的二段跳邏輯。不要把空中跳躍的次數交給 `JuiceJumpAssist`。

## 調整

| 感覺 | 改法 |
|---|---|
| 邊緣還是常常跳不起來 | `coyoteTime` 調到 0.12–0.15 |
| 落地後常常沒跳 | `bufferTime` 調到 0.2 |
| 跳躍很飄 | `fallGravityMultiplier` 調到 1.8–2.5 |
| 最高點停太久 | `apexGravityMultiplier` 調回 0.7–1，或把 `apexThreshold` 調小 |
| 輕按與長按差太多 | `jumpCutMultiplier` 調到 0.6–0.7 |

`apexThreshold` 是「垂直速度的絕對值小於多少算是在頂點附近」，要配合專案的速度單位：跳躍初速的 10–20% 是合理的起點。

## 回報

告訴使用者改了控制器的哪幾行、原本的跳躍高度與速度沒有動，以及上面三個測試方式。數值都在控制器的 Inspector 裡 `Jump` 那一組。
