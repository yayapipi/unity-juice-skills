---
name: juice-particles
description: 在 Unity 用一行程式噴出粒子與加上拖尾：命中火花、起跳與落地的塵土、爆炸、過關彩帶、子彈與衝刺的拖尾。不用做 Prefab、不用素材、不用設定 Particle System。使用者提到粒子、火花、塵土、煙、爆炸特效、彩帶、拖尾、殘影、特效、particles、sparks、dust、explosion、confetti、trail、VFX 時使用。
license: MIT
metadata:
  author: yayapipi
  version: "1.0.1"
---

# 粒子與拖尾

## 前置

專案裡要有 `Assets/Juice/`（Juice Kit）。沒有的話先照 `juice-kit` Skill 安裝。

## 最快的做法

```csharp
JuiceBurst.Spark(hitPoint, hitNormal);              // 命中火花，往 hitNormal 的方向噴
JuiceBurst.Dust(feetPosition);                      // 起跳、落地、急停的塵土
JuiceBurst.Explosion(transform.position);           // 爆炸、死亡
JuiceBurst.Confetti(transform.position);            // 過關、獎勵
JuiceBurst.Spawn(position, Color.cyan, 12);         // 自己指定顏色與顆數
JuiceTrail.Attach(gameObject, Color.cyan);          // 拖尾（子彈、衝刺、揮劍）
```

粒子是程式畫出來的小圓點，所有呼叫共用幾個 Particle System，會自動回收，有數量上限，不用自己清。

## 步驟

1. 找到事件的那一行。
2. 決定位置與方向：
   - **命中**：位置用碰撞點。2D 的 `OnCollisionEnter2D(Collision2D c)` 是 `c.GetContact(0).point` 與 `c.GetContact(0).normal`；3D 的 `OnCollisionEnter(Collision c)` 是 `c.GetContact(0).point` 與 `.normal`。只有 Trigger（沒有接觸點）就用被打物件的位置，方向不填，粒子會往四面八方噴。
   - **塵土**：角色腳底，也就是角色位置往下半個身高。
   - **爆炸**：物件的位置。**要在 `Destroy(gameObject)` 之前呼叫**；粒子不會跟著物件消失。
3. 顏色盡量取被打物件的顏色，例如 `GetComponent<SpriteRenderer>().color`，看起來像是「它的碎片」。
4. 確認能編譯，請使用者試玩。

## 完整參數

```csharp
JuiceBurst.Spawn(position, color,
    count: 10,       // 顆數
    speed: 5f,       // 初速（單位／秒），每顆會隨機打折
    size: 0.2f,      // 大小（世界單位）
    lifetime: 0.4f,  // 存活秒數
    gravity: 0f,     // 重力倍率，0 = 不會掉，1 = 正常掉落
    direction: Vector3.up,
    spread: 360f);   // 散開角度，360 = 全方向，60 = 窄窄的一束
```

| 效果 | count | speed | size | lifetime | gravity | spread |
|---|---|---|---|---|---|---|
| 火花 | 8–12 | 8–10 | 0.12–0.18 | 0.2–0.3 | 0 | 60–80 |
| 塵土 | 5–8 | 1.5–2.5 | 0.25–0.4 | 0.4–0.5 | 0 | 150–180 |
| 爆炸 | 20–30 | 8–12 | 0.25–0.35 | 0.4–0.6 | 0 | 360 |
| 碎片 | 8–15 | 5–8 | 0.15–0.25 | 0.6–1 | 1–2 | 120 |
| 撿到東西 | 6–10 | 3–5 | 0.1–0.15 | 0.3–0.4 | 0 | 360 |

表裡的大小是以「角色大約 1 個單位高」為準。專案的角色特別大或特別小，`speed` 與 `size` 要等比例調整。

## 拖尾

```csharp
JuiceTrail.Attach(gameObject, color, width: 0.3f, time: 0.25f);  // 在 Start 或生成時呼叫一次
JuiceTrail.SetEmitting(gameObject, false);                       // 瞬間移動、重生之前先關掉，免得拉出一條長線
```

`width` 設成物件寬度的一半到一倍，`time` 是拖尾存在的秒數。拖尾放在自動建立的子物件上，不會動到原本的圖。只想在衝刺時有拖尾，就在衝刺開始與結束時切換 `SetEmitting`。

## 注意

- **粒子被擋住或看不到**：2D 專案把 `JuiceBurst.SortingOrder` 調大（預設 100），在第一次噴粒子之前設定；已經噴過的話要重新進入 Play 模式才會套用。
- 正交鏡頭（2D）時粒子在畫面平面上散開；透視鏡頭（3D）時往立體的四面八方散開。
- 粒子受 `Time.timeScale` 影響，頓幀時會定格在空中，這是想要的效果。
- 遊戲資訊優先：粒子不能多到蓋住敵人與子彈。彈幕類遊戲把 `count` 減半。
- 玩家把 `JuiceSettings.Intensity` 調低時，粒子數量會自動減少。
- 需要貼圖粒子、煙霧、火焰這類「有造型」的特效時，這個 Skill 不夠用，改用 Unity 的 Particle System 或 VFX Graph 做 Prefab。

## 回報

告訴使用者哪些事件加了哪一種粒子，以及想更誇張就調 `count` 與 `speed`。
