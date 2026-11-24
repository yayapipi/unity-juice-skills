---
name: juice-jump-forgiveness
description: 讓 Unity 平台跳躍角色的操作更順、更寬容：土狼時間（coyote time）、跳躍緩衝（jump buffer）、可變跳躍高度、頂點減重力、下落加速。這些是 Celeste 等遊戲「偷偷幫玩家」的設計。使用者說「我明明有按跳卻沒跳」「跳躍很飄」「跳躍手感很差」「操作不順」「平台遊戲手感」，或提到 coyote time、jump buffer、variable jump height 時使用。
license: MIT
metadata:
  author: yayapipi
  version: "1.1.0"
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

`JuiceJumpAssist` 不是元件，是放進控制器裡的小工具，不限 2D 或 3D、不限哪一種輸入系統。分成兩段，**使用者只說「土狼時間、跳躍緩衝」時只做基本那一段**。

### 基本：土狼時間與跳躍緩衝（不會改變跳躍高度）

1. 在控制器加一個欄位（數值會出現在 Inspector）：

   ```csharp
   public JuiceJumpAssist jump = new JuiceJumpAssist();
   ```

2. 加三行，並把原本「在地上而且按下跳躍」的判斷換成 `jump.TryJump()`。以 2D 的 `Rigidbody2D` 為例：

   ```csharp
   void Update()
   {
       jump.Tick(isGrounded);                          // 每一格都告訴它有沒有踩在地上
       if (jumpPressedThisFrame) jump.Press();         // 按下跳躍的那一格

       if (jump.TryJump())                             // 取代原本的 if (isGrounded && jumpPressed)
       {
           // 原本起跳的程式原封不動放在這裡
       }
   }
   ```

   - `jumpPressedThisFrame`：這一格剛按下（Input System 是 `action.WasPressedThisFrame()` 或 `Keyboard.current.spaceKey.wasPressedThisFrame`，舊輸入是 `Input.GetButtonDown("Jump")`）。
   - `jump.Tick` 要放在更新 `isGrounded` 之後。

3. **保留專案原本的起跳方式、跳躍速度與重力**。原本用 `AddForce` 起跳就照用，只換「能不能跳」的判斷。

4. 專案原本是在 `FixedUpdate` 讀輸入的話，把「讀按鍵並呼叫 `jump.Press()`」移到 `Update`，否則按鍵偶爾會漏掉；`TryJump` 與設定速度可以留在 `FixedUpdate`。

5. 確認能編譯。請使用者試兩件事：走出平台邊緣之後才按跳（應該跳得起來）、快落地時提早按跳（落地瞬間應該自動起跳）。

起跳後的那一兩格，地面偵測通常還碰得到地。`JuiceJumpAssist` 已經處理好這件事，不會因此在空中多跳一次，不用另外加判斷。

### 進階：可變跳躍高度與重力調整（會改變跳躍的高度與滯空時間）

使用者有要求「輕按跳低、長按跳高」「跳躍不要那麼飄」時才加。加之前先告訴他跳躍的手感會變。

```csharp
Vector2 v = rb.linearVelocity;                         // Unity 6 之前的版本是 rb.velocity

if (jump.ShouldCutJump(jumpHeld, v.y))                 // 上升途中放開按鍵：把上升速度砍半
    v.y *= jump.jumpCutMultiplier;

rb.linearVelocity = v;
rb.gravityScale = baseGravityScale * jump.GravityMultiplier(v.y, jumpHeld);   // 頂點變輕、下落變重
```

- `jumpHeld`：目前按著（`action.IsPressed()` 或 `Input.GetButton("Jump")`）。
- `baseGravityScale`：在 `Awake` 記下原本的 `rb.gravityScale`。
- 3D 的 `Rigidbody` 沒有 `gravityScale`：關掉 `useGravity`，在 `FixedUpdate` 自己加 `rb.AddForce(Physics.gravity * 倍率, ForceMode.Acceleration);`。
- `CharacterController` 或自己算速度的控制器：把自己的重力值乘上 `GravityMultiplier` 的結果。
- 沒有做進階這一段時，Inspector 裡 `jumpCutMultiplier`、`apexThreshold`、`apexGravityMultiplier`、`fallGravityMultiplier` 四個欄位沒有作用。

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

告訴使用者改了控制器的哪幾行、做的是基本還是進階，以及對應的測試方式。只做基本時要說明「原本的跳躍高度與速度沒有動」。數值都在控制器的 Inspector 裡 `Jump` 那一組。
