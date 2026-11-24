---
name: juice-camera-follow
description: 讓 Unity 的鏡頭跟得自然：阻尼跟隨（慢半拍跟上）、前視（看向前進方向）、死區（小範圍移動時鏡頭不動）。適合還沒有鏡頭跟隨、或鏡頭死死黏在角色身上的 2D 與 3D 專案。使用者提到鏡頭跟隨、鏡頭很硬、鏡頭晃得頭暈、看不到前面、相機跟著玩家、camera follow、smooth camera、look ahead、dead zone 時使用。專案已經在用 Cinemachine 時不要用這個 Skill。
license: MIT
metadata:
  author: yayapipi
  version: "1.1.0"
---

# 鏡頭跟隨

## 前置

專案裡要有 `Assets/Juice/`（Juice Kit）。沒有的話先照 `juice-kit` Skill 安裝。

## 先檢查專案現在怎麼做

| 現況 | 怎麼辦 |
|---|---|
| 專案有 Cinemachine（`Packages/manifest.json` 有 `com.unity.cinemachine`，場景裡有 Cinemachine Camera） | **不要用這個 Skill**。Cinemachine 內建同樣的功能：在 Position Composer（舊版叫 Framing Transposer）調 Damping、Lookahead、Dead Zone。告訴使用者這幾個欄位在哪裡，並依下面的建議數值幫他設定 |
| 鏡頭是玩家的子物件 | 鏡頭會跟角色完全同步，還會跟著角色翻面、旋轉。把鏡頭移出來變成場景最上層的物件，再加 `JuiceFollowCamera` |
| 有自己寫的跟隨腳本（`transform.position = target.position + offset`） | 停用那個腳本，換成 `JuiceFollowCamera`；不要兩個同時開 |
| 鏡頭固定不動，而且關卡就是一個畫面 | 不需要跟隨。跟使用者確認之後就結束 |

## 步驟

1. 在鏡頭上加 `JuiceFollowCamera` 元件，把 `target` 指到玩家。
2. `useStartOffset` 打勾（預設）：用場景裡鏡頭與玩家目前的相對位置當偏移，不用自己算。
3. 依遊戲類型填數值：

   | 遊戲類型 | `smoothTime` | `lookAhead` | `deadZone` |
   |---|---|---|---|
   | 橫向平台 | 0.12–0.2 | 1.5–2.5 | (0.4, 0.8) |
   | 俯視角動作、射擊 | 0.1–0.15 | 1–2 | (0.2, 0.2) |
   | 俯視角 RPG、探索 | 0.2–0.3 | 0–1 | (0.5, 0.5) |
   | 高速（跑酷、賽車） | 0.05–0.1 | 3–4 | (0, 0) |
   | 3D 第三人稱 | 0.1–0.2 | 0 | (0, 0) |

4. 確認能編譯，請使用者試玩：左右來回走，看鏡頭會不會太慢跟上，或換方向時甩得太快。

## 欄位

| 欄位 | 意思 |
|---|---|
| `smoothTime` | 阻尼：大約幾秒追上目標。0 = 緊跟不放。越大越柔，太大玩家會跑出畫面 |
| `lookAhead` | 前視：往移動方向多看多遠（世界單位），讓玩家看得到前面 |
| `lookAheadSmoothTime` | 換方向時，前視要多久才轉過去。太小會甩 |
| `deadZone` | 死區：角色在這個範圍內移動時鏡頭不動（X、Y 各自的半徑），畫面比較穩 |
| `offset`、`useStartOffset` | 鏡頭相對目標的位置 |

重生、換房間、傳送之後呼叫 `GetComponent<JuiceFollowCamera>().Snap();`，鏡頭會直接跳過去，不會慢慢滑過整張地圖。

## 注意

- 可以跟 `JuiceShake` 掛在同一個鏡頭上，不會互相干擾。
- 頓幀或暫停（`Time.timeScale = 0`）時鏡頭不動。
- 角色用 `Rigidbody` 移動而且畫面有細微抖動時，把角色 Rigidbody 的 Interpolate 設成 Interpolate。
- 這個元件沒有「限制鏡頭不超出地圖邊界」的功能。需要邊界時，請使用者改用 Cinemachine 的 Confiner，或在回報裡說明這是另一項工作。
- 前視是往「實際移動的方向」看。俯視角射擊想要往「瞄準方向」看的話，把 `lookAhead` 設成 0，改成另外寫幾行，把 `offset` 往瞄準方向推一點。

## 回報

告訴使用者鏡頭現在的三個數值，以及「跟太慢就調小 `smoothTime`，換方向太晃就調大 `lookAheadSmoothTime` 或調小 `lookAhead`」。
