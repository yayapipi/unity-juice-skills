---
name: juice-hit-stop
description: 在 Unity 加上頓幀（hit stop，命中瞬間把時間停住幾十毫秒）與慢動作。這是動作遊戲與格鬥遊戲打擊感的最大來源。使用者提到頓幀、卡肉、打擊感、時間暫停一下、子彈時間、慢動作、hit stop、hit pause、freeze frame、slow motion 時使用。
license: MIT
metadata:
  author: yayapipi
  version: "1.0.1"
---

# 頓幀與慢動作

「在重要的瞬間停下來」。命中的那一格把時間停住，玩家才有時間感受到「打到了」。

## 前置

專案裡要有 `Assets/Juice/`（Juice Kit）。沒有的話先照 `juice-kit` Skill 安裝。

## 最快的做法

```csharp
JuiceTime.HitStop(0.06f);         // 時間停 0.06 秒（真實時間），然後自動恢復
JuiceTime.SlowMo(0.3f, 0.5f);     // 放慢到 0.3 倍，維持 0.5 秒（真實時間），再平順回到原速
```

## 建議數值

| 情境 | 呼叫 |
|---|---|
| 輕擊 | `HitStop(0.03f)` |
| 一般命中 | `HitStop(0.05f)` 到 `0.08f` |
| 重擊、玩家受傷 | `HitStop(0.1f)` 到 `0.12f` |
| 擊殺 | `HitStop(0.12f)` 到 `0.15f` |
| 大招、Boss 倒下 | `HitStop(0.15f)` 到 `0.2f` |
| 擊倒最後一個敵人、千鈞一髮閃過 | `SlowMo(0.3f, 0.5f)` |
| 過關、死亡 | `SlowMo(0.4f, 0.4f)` |

頓幀超過 0.2 秒，玩家會以為遊戲卡住。

## 步驟

1. 找到命中、受傷、擊殺的那一行，加上 `JuiceTime.HitStop(...)`。那一行已經有 `Juice.Hit` 之類的配方時，配方裡已經含頓幀（`Juice.Hit` 是 0.06 秒 × power）：想更長，在旁邊再加一行 `JuiceTime.HitStop(秒數)`，兩個會取比較長的那個，不會相加；想更短，把配方換成分開的幾行再改數字（對照 `juice-kit` 的「一行配方裡面是什麼」）。
2. 頓幀通常跟其他回饋一起才有效果：同一行附近加上 `JuiceShake.Add`、`JuiceFlash.Play`、`JuiceBurst.Spark`，或直接用 `Juice.Hit(target, point)`。震動與閃白用的是 unscaled time，時間停住時照樣會動，畫面會像定格漫畫。
3. **檢查專案裡會受影響的地方**（見下面兩節：會跟著停的東西、暫停選單），需要的話一起修。
4. 確認能編譯，請使用者試玩。

## 時間停住時，哪些東西會跟著停

頓幀是把 `Time.timeScale` 設成 0。用 `Time.deltaTime` 的東西全部會停，這正是我們要的；但下面這些通常**不應該**停，要改成 unscaled：

| 東西 | 改法 |
|---|---|
| UI 動畫（Animator） | Animator 的 Update Mode 設成 Unscaled Time |
| 自己寫的 UI 補間 | `Time.deltaTime` 改成 `Time.unscaledDeltaTime` |
| 協程裡的 `WaitForSeconds`（UI、計時器） | 改成 `WaitForSecondsRealtime` |
| 讀取按鍵 | 在 `Update` 讀。頓幀期間 `FixedUpdate` 不會執行，在那裡讀按鍵會漏掉 |

音效不受 `timeScale` 影響，不用處理。

## 它幫你處理好的事

- 頓幀期間再呼叫一次：延長到比較晚的那個時間，不會疊加，也不會提早結束。
- 遊戲已經暫停（`timeScale` 已經是 0）：什麼都不做，不會把暫停的遊戲解開。
- 慢動作中頓幀：頓幀結束後回到慢動作的速度。
- 慢動作會同步縮小 `Time.fixedDeltaTime`，物理不會一格一格跳；結束時還原。
- `JuiceTime.IsStopped` 可以查現在是不是在頓幀。

## 專案有暫停選單時，一定要做這件事

找到暫停的程式（把 `Time.timeScale` 設成 0 的地方），在進入暫停之前加一行。恢復遊戲時不用加。

```csharp
JuiceTime.Cancel();      // 先結束進行中的頓幀與慢動作
Time.timeScale = 0f;     // 原本的暫停
```

暫停是用同一個函式切換的話：

```csharp
if (paused) JuiceTime.Cancel();
Time.timeScale = paused ? 0f : 1f;
```

不加的話，玩家剛好在頓幀的那零點幾秒內按下暫停，頓幀結束時會把時間恢復，遊戲就在暫停選單開著的狀態下繼續跑。

## 注意

- 多人連線遊戲不要用全域的 `timeScale`，改成只暫停被打與打人的那兩個物件的動畫。
- 玩家把 `JuiceSettings.Intensity` 調低時，頓幀會等比例縮短。

## 回報

告訴使用者哪些事件加了頓幀、各是幾秒，以及「覺得卡就調短，覺得不夠重就調長，一次改 0.02 秒」。
