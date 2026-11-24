---
name: juice-squash-stretch
description: 在 Unity 加上擠壓與拉伸（squash and stretch）以及縮放彈跳，讓角色與物件有彈性、有生命。用在起跳、落地、衝刺、被打、撿到東西、物件出現的時候。使用者提到擠壓拉伸、Q 彈、彈性、果凍感、角色很僵硬、squash、stretch、punch scale、bounce 時使用。
license: MIT
metadata:
  author: yayapipi
  version: "1.0.0"
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

1. 找到事件的那一行（起跳、落地、受擊…）。落地通常是「上一格不在地上、這一格在地上」的地方；專案沒有這個判斷就加一個 `bool wasGrounded`。
2. **決定要變形哪個物件**。物件上有碰撞框（Collider）或剛體時，縮放會連碰撞框一起變形，角色可能卡進地板。這時候把圖（SpriteRenderer 或模型）放在子物件上，對子物件變形：`JuiceSquash.Squash(visual, 0.25f);`。專案原本就把圖放在子物件就直接用；沒有的話，幅度不超過 0.2 通常沒問題，並在回報裡提醒使用者。
3. 加上呼叫，確認能編譯。
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
