---
name: juice-kit
description: 把 Juice Kit（一組不需要素材、不需要付費插件的 Unity C# 腳本）安裝或更新到 Unity 專案的 Assets/Juice。所有 juice-* Skill 都需要它。使用者說「安裝 Juice Kit」「安裝 Juice Skills」，或專案出現找不到 Juice、JuiceShake、JuiceSfx 等類別的編譯錯誤時使用。Installs the Juice Kit runtime scripts into a Unity project.
license: MIT
metadata:
  author: yayapipi
  version: "1.0.0"
---

# Juice Kit：安裝與總覽

Juice Kit 是 24 個 C# 腳本加兩個 Shader（每個檔案旁邊有一個 Unity 用的 `.meta`）。全部用程式產生畫面與聲音，不需要任何素材、Prefab 或付費插件。其他 `juice-*` Skill 都是在教你怎麼用這些腳本。

## 安裝步驟

1. **找到 Unity 專案根目錄**：同時有 `Assets/` 與 `ProjectSettings/` 的那個資料夾。
2. **檢查是否已安裝**：如果 `Assets/Juice/Juice.cs` 已經存在，讀裡面的 `Version`。跟這份 Skill 的版本（1.0.0）相同就不用重裝，直接結束。
3. **複製資料夾**：把這個 Skill 資料夾裡的 `unity/Juice/` 整個複製成專案的 `Assets/Juice/`，子資料夾 `UI`、`Resources`、`Demo`、`Editor` 與所有 `.meta` 檔都要保留。更新舊版時直接覆蓋同名檔案。
   - macOS、Linux：`cp -R "<這個 Skill 的路徑>/unity/Juice" "<專案>/Assets/Juice"`
   - Windows PowerShell：`Copy-Item -Recurse "<這個 Skill 的路徑>\unity\Juice" "<專案>\Assets\Juice"`
   - 如果這個 Skill 資料夾裡沒有 `unity/Juice`，從 GitHub 取得：`git clone --depth 1 https://github.com/yayapipi/unity-juice-skills`，來源是 `skills/juice-kit/unity/Juice`。
4. **檢查 uGUI**：`Packages/manifest.json` 沒有 `com.unity.ugui` 的話，刪掉 `Assets/Juice/UI/`（裡面兩個檔案需要 Unity UI）。
5. **等 Unity 編譯**，確認 Console 沒有錯誤。你有工具可以讀 Console（Unity CLI、Unity MCP）就自己讀；沒有就請使用者切回 Unity 看一眼。
6. **回報**：告訴使用者裝在哪裡，以及可以用選單 `Tools ▸ Juice ▸ Create Demo Scene` 看示範。

`.meta` 檔裡是固定的 GUID，一定要跟著複製：場景與 Prefab 是靠 GUID 找到 `JuiceFeedback`、`JuiceButton` 這些元件的，少了它，之後更新 Kit 時場景裡的元件會變成 Missing Script。`Assets/Juice` 資料夾本身的 `.meta` 由 Unity 自己產生。

### 專案自己的腳本放在 asmdef 裡的時候

Juice Kit 預設編進 `Assembly-CSharp`，放在 asmdef 裡的腳本引用不到它。這種情況才需要：在 `Assets/Juice/` 加一個 `JuiceKit.asmdef`（references 加 `UnityEngine.UI`），在 `Assets/Juice/Editor/` 加一個只限 Editor 的 `JuiceKit.Editor.asmdef`（references 加 `JuiceKit`），再讓專案的 asmdef 引用 `JuiceKit`。專案沒有用 asmdef 就完全不要加。

## 裡面有什麼

| 類別 | 一句話 | 最常用的一行 |
|---|---|---|
| `Juice` | 常見事件的一行配方 | `Juice.Hit(enemy, hitPoint);` |
| `JuiceShake` | 鏡頭震動、後座、推近 | `JuiceShake.Add(0.4f);` |
| `JuiceTime` | 頓幀、慢動作 | `JuiceTime.HitStop(0.06f);` |
| `JuiceSquash` | 擠壓、拉伸、彈一下 | `JuiceSquash.Squash(transform);` |
| `JuiceFlash` | 閃白 | `JuiceFlash.Play(gameObject);` |
| `JuiceBurst` | 火花、塵土、爆炸、彩帶 | `JuiceBurst.Spark(point);` |
| `JuiceTrail` | 拖尾 | `JuiceTrail.Attach(gameObject, Color.cyan);` |
| `JuiceSfx` | 音高隨機、連擊音階、內建合成音 | `JuiceSfx.Blip(JuiceBlip.Jump);` |
| `JuiceFloatingText` | 傷害數字、飄字 | `JuiceFloatingText.Spawn(point, "12");` |
| `JuiceFeedback` | 把上面全部放在一個元件，在 Inspector 調 | `feedback.PlayAt(point);` |
| `JuiceButton` | UI 按鈕的滑過、按下、彈回 | 掛在按鈕上 |
| `JuiceCounter` | 分數滾動 | `counter.Set(score);` |
| `JuicePop` | 物件出現時彈出來 | 掛在物件上 |
| `JuiceTypewriter` | 對話逐字出現 | `typewriter.Play(line);` |
| `JuiceFade` | 淡出淡入轉場 | `JuiceFade.LoadScene("Level2");` |
| `JuiceJumpAssist` | 土狼時間、跳躍緩衝 | 放進角色控制器 |
| `JuiceFollowCamera` | 鏡頭阻尼跟隨、前視、死區 | 掛在鏡頭上 |
| `JuiceSettings` | 讓玩家關掉震動與閃光 | `JuiceSettings.ScreenShake = false;` |
| `JuiceEase`、`JuiceRunner` | 緩動曲線與最小的補間 | `JuiceRunner.Tween(0.3f, e => ...);` |

所有 `static` 的呼叫都會自己建立需要的物件（鏡頭上的元件、粒子系統、AudioSource），**不用在場景裡先放任何東西**。在編輯模式（沒按 Play）呼叫不會有任何作用，也不會出錯。

## 示範場景

- 人：選單 `Tools ▸ Juice ▸ Create Demo Scene`，按 Play，用左邊的開關一項一項打開回饋。
- AI Agent：呼叫 `JuiceMenu.AddDemoToScene();`（不會跳對話框），再進入 Play 模式。

## 無障礙

有些玩家看到畫面晃動或閃爍會不舒服。Juice Kit 內建三個設定，會自動存檔：

```csharp
JuiceSettings.ScreenShake = false;  // 關掉震動、後座、推近
JuiceSettings.Flashes = false;      // 關掉閃白
JuiceSettings.Intensity = 0.5f;     // 整體強度 0–1
```

專案有設定選單時，主動建議把這三個接上去。

## 規則

- 要改手感，改「呼叫時傳的數字」或元件在 Inspector 的欄位，不要改 `Assets/Juice/` 裡的檔案；之後更新 Kit 會整個覆蓋。
- 建議 Unity 2022.3 以上。實際測過的是 Unity 6（6000.5）的 URP 與內建管線。
