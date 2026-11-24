---
name: juice-feedback
description: 用 JuiceFeedback 元件把一個事件的所有回饋（頓幀、震動、形變、閃白、粒子、音效、飄字）集中在一個地方，全部在 Unity Inspector 調整，程式只要呼叫 Play()。使用者想要「可以自己調數值」「不想改程式就能調手感」「做一個 FeedbackPlayer」「像 Feel 的 MMF Player 那樣」，或要把回饋接到按鈕與 UnityEvent 時使用。
license: MIT
metadata:
  author: yayapipi
  version: "1.1.0"
---

# Juice Feedback：一個事件，一個元件

`Juice.Hit()` 這類一行配方的數值寫在程式裡。使用者想自己在 Inspector 拉數值、反覆試手感時，改用 `JuiceFeedback` 元件：一個事件放一個，程式只負責在對的時間呼叫 `Play()`。

## 前置

專案裡要有 `Assets/Juice/`（Juice Kit）。沒有的話先照 `juice-kit` Skill 安裝。

## 步驟

1. **在腳本加一個欄位**，一個事件一個：

   ```csharp
   public JuiceFeedback hitFeedback;
   public JuiceFeedback jumpFeedback;
   ```

2. **在事件發生的那一行呼叫**：

   ```csharp
   if (hitFeedback != null) hitFeedback.PlayAt(hitPoint);  // 粒子與飄字出現在 hitPoint
   if (jumpFeedback != null) jumpFeedback.Play();           // 出現在 target（或元件自己）的位置
   ```

3. **在場景裡建立元件**：在角色底下建一個空的子物件（例如 `Feedbacks/Hit`），加上 `JuiceFeedback`，把 `target` 指到要變形與閃白的物件（通常是角色的圖），再把它拖到步驟 1 的欄位。你能操作編輯器（Unity CLI、Unity MCP）就自己做並填好數值；不能的話，把這一步的操作方式寫給使用者。

4. **填起手值**（見下表），請使用者按 Play 之後自己調。

## 欄位與起手值

| 欄位 | 意思 | 命中 | 起跳 | 落地 | 撿道具 |
|---|---|---|---|---|---|
| `hitStop` | 頓幀秒數，0 = 不用 | 0.06 | 0 | 0 | 0 |
| `slowMotion` | 慢動作倍率，1 = 不用 | 1 | 1 | 1 | 1 |
| `shake` | 鏡頭震動 0–1 | 0.3 | 0 | 0.1 | 0 |
| `zoomPunch` | 鏡頭推近比例 | 0 | 0 | 0 | 0 |
| `scale` | `None`／`Squash`／`Stretch`／`Punch` | Punch | Stretch | Squash | Punch |
| `scaleAmount` | 變形幅度 | 0.15 | 0.22 | 0.25 | 0.2 |
| `flash` | 閃白 | 開 | 關 | 關 | 關 |
| `particles` | 粒子顆數，0 = 不用 | 10 | 6 | 6 | 8 |
| `sound` | `None`／`Clip`／`BuiltInBlip` | BuiltInBlip | BuiltInBlip | BuiltInBlip | BuiltInBlip |
| `blip` | 內建合成音的種類 | Hit | Jump | Land | Pickup |
| `floatingText` | 飄字，留空 = 不用 | | | | +1 |

其他欄位：`flashColor`、`flashDuration`、`particleColor`、`particleSpeed`、`particleSize`、`clip`（自己的音效，`sound` 設成 `Clip`）、`volume`、`pitchJitter`、`floatingTextColor`、`slowMotionDuration`、`onPlay`（UnityEvent，播放時一起觸發）。

## 不寫程式的接法

`JuiceFeedback.Play` 可以直接拖進任何 UnityEvent：UI 按鈕的 `OnClick`、動畫事件、Timeline 的 Signal。

## 測試

執行中，在元件標題列右邊的「⋮」選單選「Play（測試）」，不用真的觸發事件就能看效果。

## 回報

告訴使用者每個 `JuiceFeedback` 放在哪個物件上、對應哪個事件，以及「按 Play 之後直接在 Inspector 改數值，改到滿意再把數值記下來（離開 Play 模式後 Inspector 的修改會還原）」。
