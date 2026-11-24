---
name: juice-kit
description: 把 Juice Kit（一組不需要素材、不需要付費插件的 Unity C# 腳本）安裝或更新到 Unity 專案的 Assets/Juice。所有 juice-* Skill 都需要它。使用者說「安裝 Juice Kit」「安裝 Juice Skills」，或專案出現找不到 Juice、JuiceShake、JuiceSfx 等類別的編譯錯誤時使用。Installs the Juice Kit runtime scripts into a Unity project.
license: MIT
metadata:
  author: yayapipi
  version: "1.1.0"
---

# Juice Kit：安裝與總覽

Juice Kit 是 28 個 C# 腳本加兩個 Shader（每個檔案旁邊有一個 Unity 用的 `.meta`）。全部用程式產生畫面與聲音，不需要任何素材、Prefab 或付費插件。其他 `juice-*` Skill 都是在教你怎麼用這些腳本。

## 安裝步驟

1. **找到 Unity 專案根目錄**：同時有 `Assets/` 與 `ProjectSettings/` 的那個資料夾。
2. **檢查是否已安裝**：如果 `Assets/Juice/Juice.cs` 已經存在，讀裡面的 `Version`。跟這份 Skill 的版本（1.1.0）相同就不用重裝，直接結束。
3. **複製內容**：把這個 Skill 資料夾裡 `unity/Juice/` **裡面的內容**複製到專案的 `Assets/Juice/`，子資料夾 `UI`、`Resources`、`Demo`、`Editor` 與所有 `.meta` 檔都要保留。第一次安裝與更新舊版都用同一組指令，同名檔案會被覆蓋：
   - macOS、Linux：`mkdir -p "<專案>/Assets/Juice" && cp -R "<這個 Skill 的路徑>/unity/Juice/." "<專案>/Assets/Juice/"`
   - Windows PowerShell：`New-Item -ItemType Directory -Force "<專案>\Assets\Juice" | Out-Null; Copy-Item -Recurse -Force "<這個 Skill 的路徑>\unity\Juice\*" "<專案>\Assets\Juice\"`
   - 來源結尾的 `/.` 與 `\*` 不能少：少了它，在 `Assets/Juice` 已經存在時會複製成 `Assets/Juice/Juice`，腳本重複、整個專案編譯失敗。複製完確認沒有 `Assets/Juice/Juice/` 這個資料夾。
   - 如果這個 Skill 資料夾裡沒有 `unity/Juice`，從 GitHub 取得：`git clone --depth 1 https://github.com/yayapipi/unity-juice-skills`，來源是 `skills/juice-kit/unity/Juice`。
4. **檢查 uGUI**：`Packages/manifest.json` 沒有 `com.unity.ugui` 的話，刪掉 `Assets/Juice/UI/` 與 `Assets/Juice/UI.meta`（裡面兩個檔案需要 Unity UI）。
5. **等 Unity 編譯**，確認 Console 沒有錯誤。你有工具可以讀 Console（Unity CLI、Unity MCP）就自己讀；沒有就請使用者切回 Unity 看一眼。
6. **回報**：告訴使用者裝在哪裡，以及可以用選單 `Tools ▸ Juice ▸ Create Showcase Scene` 看完整展示。

`.meta` 檔裡是固定的 GUID，一定要跟著複製：場景與 Prefab 是靠 GUID 找到 `JuiceFeedback`、`JuiceButton` 這些元件的，少了它，之後更新 Kit 時場景裡的元件會變成 Missing Script。`Assets/Juice` 資料夾本身的 `.meta` 由 Unity 自己產生。

### 專案自己的腳本放在 asmdef 裡的時候

Juice Kit 預設編進 `Assembly-CSharp`，放在 asmdef 裡的腳本引用不到它。這種情況才需要：在 `Assets/Juice/` 加一個 `JuiceKit.asmdef`（references 加 `UnityEngine.UI`），在 `Assets/Juice/Editor/` 加一個只限 Editor 的 `JuiceKit.Editor.asmdef`（references 加 `JuiceKit`），再讓專案的 asmdef 引用 `JuiceKit`。專案沒有用 asmdef 就完全不要加。

## 裡面有什麼

| 類別 | 一句話 | 最常用的一行 |
|---|---|---|
| `Juice` | 常見事件的一行配方 | `Juice.Hit(enemy.gameObject, hitPoint);` |
| `JuiceShake` | 鏡頭震動、後座、推近 | `JuiceShake.Add(0.4f);` |
| `JuiceTime` | 頓幀、慢動作 | `JuiceTime.HitStop(0.06f);` |
| `JuiceSquash` | 擠壓、拉伸、彈一下 | `JuiceSquash.Squash(transform);` |
| `JuiceSpring` | 彈簧式的移動：推一下彈回、搖晃、掉進場、跟隨 | `JuiceSpring.Nudge(transform, Vector3.down * 0.3f);` |
| `JuiceEyes` | 替物件加上會看東西、會眨眼的眼睛 | `JuiceEyes.Attach(transform);` |
| `JuiceFlash` | 閃白 | `JuiceFlash.Play(gameObject);` |
| `JuiceBurst` | 火花、塵土、爆炸、彩帶 | `JuiceBurst.Spark(point);` |
| `JuiceTrail` | 拖尾 | `JuiceTrail.Attach(gameObject, Color.cyan);` |
| `JuiceSfx` | 音高隨機、連擊音階、內建合成音 | `JuiceSfx.Blip(JuiceBlip.Jump);` |
| `JuiceMusic` | 背景音樂的淡入淡出，附一段內建的合成循環 | `JuiceMusic.Play();` |
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

所有 `static` 的回饋呼叫都會自己建立需要的物件（鏡頭上的元件、粒子系統、AudioSource），**不用在場景裡先放任何東西**。在編輯模式（沒按 Play）呼叫不會有任何作用，也不會出錯。

## 一行配方裡面是什麼

| 配方 | 等於這幾行 |
|---|---|
| `Juice.Hit(target, point, power)` | `JuiceTime.HitStop(0.06f * power)`、`JuiceShake.Add(0.3f * power)`、`JuiceFlash.Play(target)`、`JuiceSquash.Punch(target.transform, 0.15f * power)`、`JuiceBurst.Spark(point)`、`JuiceSfx.Blip(JuiceBlip.Hit)` |
| `Juice.Hurt(target, power)` | `HitStop(0.1f * power)`、`Add(0.5f * power)`、`JuiceFlash.Play(target, 紅色, 0.1f)`、`JuiceSquash.Squash(target.transform, 0.2f * power)`、`Blip(Hurt)` |
| `Juice.Jump(transform)` | `JuiceSquash.Stretch(transform, 0.22f)`、`JuiceBurst.Dust(腳底)`、`Blip(Jump, 0.4f)` |
| `Juice.Land(transform, impact)` | `JuiceSquash.Squash(transform, 0.25f * impact)`、`JuiceBurst.Dust(腳底)`、`Blip(Land, 0.4f)`；`impact` 1.5 以上再加 `Add(0.12f * impact)` |
| `Juice.Pickup(position, color, text)` | `JuiceBurst.Spawn(position, color, 8, 4f, 0.14f, 0.35f)`、`Blip(Pickup)`；有 `text` 再加 `JuiceFloatingText.Spawn` |
| `Juice.Coin(position, combo)` | `JuiceBurst.Spawn(position, 金色, 6, 3.5f, 0.12f, 0.3f)`、`JuiceSfx.PlayCombo(金幣音, combo, 0.45f)` |
| `Juice.Explode(position, power)` | `HitStop(0.12f * power)`、`Add(0.6f * power)`、`JuiceShake.Zoom(0.04f * power, 0.3f)`、`JuiceBurst.Explosion(position, null, power)`、`Blip(Explosion, 0.6f)` |
| `Juice.Win(position)` | `JuiceBurst.Confetti(position)`、`Blip(PowerUp)`、`JuiceTime.SlowMo(0.4f, 0.4f)`、`JuiceShake.Zoom(0.06f, 0.5f)` |
| `Juice.Lose()` | `Blip(Lose)`、`JuiceTime.SlowMo(0.3f, 0.6f)`、`JuiceShake.Add(0.5f)` |

使用者要其中一項不一樣（例如更短的頓幀、不要震動），就把配方那一行換成表裡的幾行，再改數字或拿掉其中一行。

## 兩件一定要知道的事

1. **變形只會作用在安全的物件上**。`JuiceSquash.Squash`、`Stretch`、`Punch`（以及用到它們的 `Juice.Jump`、`Land`、`Hit`、`Hurt`）會自己挑物件：
   - 角色的圖放在子物件上：只變形那個子物件，碰撞框不受影響。
   - 圖和「會受重力掉落的剛體」（Dynamic 的 Rigidbody 或 CharacterController）在同一個物件上：**不變形**，Console 會提示一次，其他回饋照常。因為這種物件一縮放，碰撞框與地面偵測點會跟著移動，角色會浮起來或重複觸發落地。
   - 遇到第二種情況：你能操作編輯器時，把圖（Sprite 或模型）移到子物件；不能的話，在回報裡告訴使用者這樣做就會有擠壓拉伸。
2. **`Juice.Hit`、`Hurt`、`Explode`、`Win`、`Lose` 會動到 `Time.timeScale`**（頓幀或慢動作）。專案有暫停選單時，在進入暫停之前加上 `JuiceTime.Cancel();`，細節見 `juice-hit-stop`。

## 示範場景

有兩個，都不需要任何素材：

- **完整展示**（`JuiceShowcase`）：一個打磚塊小遊戲，所有回饋一開始都是打開的，左邊的開關可以一項一項關掉。人用選單 `Tools ▸ Juice ▸ Create Showcase Scene`；AI Agent 呼叫 `JuiceMenu.AddShowcaseToScene();`。想知道某種回饋怎麼寫，讀 `Assets/Juice/Demo/JuiceShowcase.cs` 裡的 `OnBrick`、`OnPaddle`、`OnWall`。
- **最小示範**（`JuiceDemo`）：五個方塊、七個開關，一開始全部關閉。人用選單 `Tools ▸ Juice ▸ Create Demo Scene`；AI Agent 呼叫 `JuiceMenu.AddDemoToScene();`。

兩個呼叫都不會跳對話框，加完再進入 Play 模式。

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
