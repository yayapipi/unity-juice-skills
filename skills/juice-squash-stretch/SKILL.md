---
name: juice-squash-stretch
description: 在 Unity 加上擠壓與拉伸（squash and stretch）以及縮放彈跳，讓角色與物件有彈性、有生命。用在起跳、落地、衝刺、被打、撿到東西、物件出現的時候。使用者提到擠壓拉伸、Q 彈、彈性、果凍感、角色很僵硬、squash、stretch、punch scale、bounce 時使用。
license: MIT
metadata:
  author: yayapipi
  version: "1.0.1"
---

# 擠壓與拉伸

動畫十二法則的第一條。連一個方塊都能靠它變得有生命，而且不需要任何美術資源。

## 前置

專案裡要有 `Assets/Juice/`（Juice Kit）。沒有的話先照 `juice-kit` Skill 安裝。

## 最快的做法

在事件發生的那一行加一行，不用在場景裡放任何東西：

```csharp
JuiceSquash.Stretch(transform, 0.22f);  // 起跳、衝刺：變窄變高
JuiceSquash.Squash(transform, 0.25f);   // 落地、被踩：變寬變矮
JuiceSquash.Punch(transform, 0.2f);     // 被打、撿到、得分：整個放大再彈回
JuiceSquash.Punch(transform, -0.1f);    // 按下的感覺：縮小再彈回
```

體積大致不變：`Squash(0.25f)` 是寬 1.25 倍、高 0.8 倍，然後在 0.18 秒內彈回原狀。

## 步驟

1. 找到事件的那一行（起跳、落地、受擊…）。落地通常是「上一格不在地上、這一格在地上」的地方；專案沒有這個判斷就記下上一格的值，並把初始值設成 true，開場第一格才不會誤播。
2. **看角色的結構**。縮放會連同一個物件上的碰撞框、以及底下的地面偵測點一起移動，所以 Kit 會自己挑安全的物件：
   - 傳進去的物件沒有碰撞框與剛體：直接變形它。
   - 有物理，而圖（SpriteRenderer 或模型）放在子物件：只變形放圖的子物件。這是最理想的結構，直接傳角色的 `transform` 就好。
   - 圖和會受重力掉落的剛體（Dynamic 的 Rigidbody、CharacterController）在同一個物件上：**不會變形**，Console 會提示一次。這時候要把圖移到子物件：你能操作編輯器（Unity CLI、Unity MCP）就自己做，注意 Animator 與腳本裡對 SpriteRenderer 的引用要跟著改；不能的話，在回報裡清楚告訴使用者。不要為了有變形而去縮放帶碰撞框的物件。
   - 自己寫的 Kinematic 角色控制器 Kit 判斷不出來，一樣請把圖放在子物件。
3. 加上呼叫，確認能編譯。想強制指定變形的物件，就在角色上自己掛 `JuiceSquash` 元件並設定 `target`，或呼叫 `JuiceSquash.Get(visual).Squash(0.25f);`。
4. 想調回彈的感覺時，設定元件的欄位：

   ```csharp
   JuiceSquash s = JuiceSquash.Get(visual);
   s.duration = 0.25f;                    // 彈回要幾秒
   s.ease = JuiceEaseType.OutElastic;     // OutBack（預設，彈過頭一點）或 OutElastic（抖幾下）
   s.unscaledTime = true;                 // UI 或暫停畫面用
   ```

## 建議數值

| 情境 | 呼叫 | 幅度 |
|---|---|---|
| 起跳 | `Stretch` | 0.15–0.25 |
| 落地 | `Squash` | 0.2–0.35，落得越重越大 |
| 衝刺、發射 | `Stretch` | 0.25–0.4 |
| 被打 | `Punch` | 0.1–0.2 |
| 撿到東西、得分 | `Punch` | 0.2–0.3 |
| 按鈕按下 | `Punch` | -0.05 到 -0.1 |

落地的幅度可以跟落下速度連動：`JuiceSquash.Squash(visual, Mathf.Clamp(-velocityY * 0.02f, 0.1f, 0.4f));`

## 注意

- 用 `localScale.x = -1` 翻面的角色可以直接用，正負號會保留。
- 動畫中再觸發一次沒關係，結束時一定會回到原本的大小。
- 有 Animator 在控制同一個物件的 Scale 時，Animator 會蓋掉這裡的結果。改對沒有被動畫控制 Scale 的父物件或子物件做。
- 要「物件出現時從小彈到大」，用 `JuicePop` 元件（見 `juice-ui`）。

## 回報

告訴使用者加在哪些事件、幅度各是多少，以及「幅度調大比較卡通、調小比較寫實」。
