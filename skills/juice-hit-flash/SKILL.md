---
name: juice-hit-flash
description: 在 Unity 加上受擊閃白：角色或敵人被打到的瞬間整個變成白色（或指定顏色）再變回來。2D Sprite 與 3D 模型都能用，不需要自己寫 Shader 或換材質。使用者提到閃白、受擊閃爍、被打沒感覺、變白一下、閃紅、hit flash、damage flash、flash white 時使用。
license: MIT
metadata:
  author: yayapipi
  version: "1.0.1"
---

# 受擊閃白

被打到的那幾格把圖換成純色，是最便宜也最清楚的「我打到了」。

## 前置

專案裡要有 `Assets/Juice/`（Juice Kit），而且 `Assets/Juice/Resources/` 裡的兩個 Shader 要在。沒有的話先照 `juice-kit` Skill 安裝。

## 最快的做法

```csharp
JuiceFlash.Play(gameObject);                                     // 白色，0.06 秒
JuiceFlash.Play(gameObject, new Color(1f, 0.25f, 0.25f), 0.1f);  // 指定顏色與時間
```

會把這個物件**與所有子物件**上的 `SpriteRenderer`、`MeshRenderer`、`SkinnedMeshRenderer` 一起換成純色，時間到自動換回原本的材質。第一次呼叫會自動加上 `JuiceFlash` 元件。

## 步驟

1. 找到「被打到」的那一行，通常在 `TakeDamage` 之類的函式裡。
2. 加上 `JuiceFlash.Play(gameObject);`。傳進去的物件要是「整個角色」的根物件，身上各部位才會一起閃。
3. 物件在這一擊就會被 `Destroy` 的話，閃白看不到，改成在死亡位置放爆炸粒子（`juice-particles`）。
4. 確認能編譯，請使用者試玩。

## 建議數值

| 情境 | 顏色 | 時間 |
|---|---|---|
| 敵人被打 | 白 | 0.04–0.08 秒（60 FPS 大約 2–5 格） |
| 玩家受傷 | 紅 `(1, 0.25, 0.25)` 或白 | 0.08–0.12 秒 |
| 中毒、燃燒、冰凍 | 綠、橘、淡藍 | 0.1 秒，每次跳傷害閃一次 |
| 無敵時間 | 白，重複閃 | 每 0.1 秒呼叫一次 |

超過 0.15 秒看起來是「變色」而不是「閃」。

## 它是怎麼做到的

Unity 預設的 Sprite 材質只能把顏色「乘」上去，所以沒辦法把圖變成純白。`JuiceFlash` 在閃的期間把材質換成 Kit 內建的純色 Shader（2D 版會保留圖的透明度），結束後換回原本的材質。內建管線與 URP（含 2D Renderer）都能用，不用依管線準備不同的材質。

用的是 unscaled time，頓幀（`Time.timeScale = 0`）期間也看得到，而且至少會畫出一格。

## 注意

- **讓玩家能關掉**：`JuiceSettings.Flashes = false;` 之後不會再閃。對光敏感的玩家需要這個選項。
- 不要整個畫面一起閃，也不要每秒閃超過三次。
- UI 的 `Image`、`TextMeshPro` 不是 Renderer，不會閃；UI 要用 `JuiceSquash.Punch` 或改顏色。
- 粒子、拖尾、線條不會被換色，這是刻意的。
- 3D 模型閃的那幾格不會投射陰影；透明或鏤空（Cutout）材質的模型會閃成整塊面片的形狀。
- 有其他程式在同一時間更換這個物件的材質（例如換裝）時，閃白結束會把材質換回「閃之前」的那一份。

## 回報

告訴使用者哪些物件會閃、顏色與時間，以及設定選單可以接 `JuiceSettings.Flashes`。
