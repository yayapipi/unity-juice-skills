---
name: juice-screen-shake
description: 在 Unity 加上鏡頭震動（創傷值 trauma 模型）、開槍後座與重擊時的鏡頭推近，不需要 Cinemachine，也不會跟現有的鏡頭跟隨打架。使用者提到畫面震動、鏡頭晃動、打擊感不夠、爆炸沒感覺、後座力、screen shake、camera shake、recoil、zoom punch 時使用。
license: MIT
metadata:
  author: yayapipi
  version: "1.1.0"
---

# 鏡頭震動

## 前置

專案裡要有 `Assets/Juice/`（Juice Kit）。沒有的話先照 `juice-kit` Skill 安裝。

## 最快的做法

```csharp
JuiceShake.Add(0.4f);                     // 震動。0–1，可以累加
JuiceShake.Kick(-shootDirection, 0.3f);   // 後座：鏡頭往某個方向頓一下再彈回（單位是世界座標）
JuiceShake.Zoom(0.05f, 0.25f);            // 推近 5%，0.25 秒內彈回
```

第一次呼叫時會自動在主鏡頭（Tag 是 `MainCamera` 的鏡頭）加上 `JuiceShake` 元件，不用手動掛。

## 原理：創傷值

每次呼叫 `Add` 是「累加創傷值 trauma」，上限 1，每秒自動減少。實際的震動量是 **trauma 的平方**：小事幾乎不震，大事才明顯，連續命中會越震越大。晃動用 Perlin Noise，比亂數順。這個做法出自 Squirrel Eiserloh 在 GDC 的演講〈Juicing Your Cameras With Math〉。

## 步驟

1. 找到該震的事件。**不是每件事都要震**：走路、一般射擊、撿金幣不要震。
2. 依事情的大小選數值：

   | 事件 | `Add` 的值 |
   |---|---|
   | 輕擊、重落地 | 0.15–0.25 |
   | 一般命中 | 0.3–0.4 |
   | 玩家受傷、擊殺、一般爆炸 | 0.45–0.6 |
   | 大爆炸、Boss 倒下 | 0.7–1 |

3. 射擊遊戲再加後座：`JuiceShake.Kick(-aimDirection, 0.2f);`（往射擊的反方向）。
4. 重擊、過關再加推近：`JuiceShake.Zoom(0.04f);`
5. 確認能編譯，請使用者試玩。

## 調整整體手感

改主鏡頭上 `JuiceShake` 元件的欄位（Inspector，或用程式 `JuiceShake.Main.maxOffset = 0.3f;`）：

| 欄位 | 預設 | 意思 |
|---|---|---|
| `maxOffset` | 0.5 | trauma = 1 時的最大位移（世界單位）。畫面很小（orthographic size 3 以下）調到 0.2–0.3；很大就調高 |
| `maxAngle` | 3 | 最大旋轉角度。不想要畫面歪就設 0 |
| `decay` | 1.4 | 每秒減少多少 trauma。越大停得越快 |
| `frequency` | 22 | 晃動頻率。越大越急促 |

正交鏡頭（2D）以位移為主；透視鏡頭（3D）自動改成以旋轉為主、位移很小。

## 為什麼不會跟別的鏡頭程式打架

位移只在「畫這一格」的時候套用，畫完立刻還原，鏡頭的 Transform 實際上沒有被改。所以可以直接跟鏡頭跟隨腳本、`JuiceFollowCamera`、Cinemachine 一起用，不用把鏡頭包進父物件。內建管線與 URP 都實際測過；HDRP 用的是同一組事件，但沒有實際測過。

用的是 unscaled time，頓幀（`Time.timeScale = 0`）的時候照樣會震，兩個可以一起用。

## 注意

- **一定要讓玩家能關掉**：`JuiceSettings.ScreenShake = false;`。專案有設定選單就接上去。
- 場景裡有多個鏡頭時，預設震的是 `Camera.main`。要震別的鏡頭，就在那個鏡頭上自己加 `JuiceShake` 元件，呼叫 `GetComponent<JuiceShake>().AddTrauma(0.4f);`。
- Screen Space - Overlay 的 UI 不會跟著震（通常這樣比較好，血條才看得清楚）。
- VR 專案不要用鏡頭震動。

## 回報

告訴使用者哪些事件加了震動、各是多少，以及「整體太晃就調小鏡頭上的 `maxOffset`，個別事件太晃就調小那一行的數字」。
