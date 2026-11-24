---
name: juice-audit
description: 幫 Unity 遊戲全面加上 Juice（遊戲手感、打擊感、回饋）。先盤點專案裡的事件，排出優先順序，再一個一個加上音效、擠壓拉伸、閃白、粒子、鏡頭震動、頓幀。使用者說「幫我的遊戲加 Juice」「打擊感不夠」「手感很乾」「讓遊戲更有質感」「polish」「game feel」「juice it up」，但沒有指定要哪一種效果時，從這個 Skill 開始。
license: MIT
metadata:
  author: yayapipi
  version: "1.1.0"
---

# Juice Audit：盤點、排序、逐一套用

Juice 的意思是「最小的輸入，換來最大的回饋」：玩法不變，只放大每個動作的感受。這個 Skill 是所有 `juice-*` Skill 的入口。

## 前置

專案裡要有 `Assets/Juice/`（Juice Kit）。沒有的話先照 `juice-kit` Skill 安裝（同一個 skills 資料夾下的 `juice-kit/SKILL.md`）。

## 流程

### 1. 盤點事件

搜尋專案的腳本，找出下面這些「瞬間」發生在哪個檔案的哪一行。找不到的就略過。

| 事件 | 常見的程式線索 |
|---|---|
| 起跳 | `Jump`、`AddForce`、把往上的速度設成正值 |
| 落地 | `isGrounded` 從 false 變 true、`OnCollisionEnter` |
| 攻擊命中 | `TakeDamage`、`OnTriggerEnter`、`Hit`、`health -=` |
| 玩家受傷 | 玩家自己的 `TakeDamage`、`hp -=` |
| 死亡、爆炸 | `Die`、`Destroy(gameObject)`、`health <= 0` |
| 撿到東西、得分 | `Collect`、`Pickup`、`score +=`、`coins++` |
| UI 按鈕 | `Button`、`onClick` |
| 過關、失敗 | `Win`、`GameOver`、`LevelComplete` |
| 換場景 | `SceneManager.LoadScene` |
| 對話、劇情文字 | 直接設定文字元件的 `.text` |

### 2. 排優先順序，列給使用者看

用這張表回報：事件｜現在有什麼回饋｜建議加什麼｜優先度。排序規則：

1. **先做每秒都會發生的**：移動、跳躍、按鈕。出現次數越多，打磨的回報越高。
2. **再做最重要的瞬間**：命中、受傷、死亡、過關。
3. **最後才是氣氛**：拖尾、慢動作、轉場。

同一個事件裡，效果的效益大致是：音效 ＞ 擠壓拉伸 ＞ 閃白與粒子 ＞ 鏡頭震動 ＞ 頓幀。

使用者有指定就照他的。沒指定「哪三個」時，預設做下面三個，其餘留在清單裡（起跳與落地算同一個事件）：

1. 玩家最常做的動作：有跳躍就做「起跳與落地」；沒有跳躍的射擊遊戲做「開火」；都沒有就做「UI 按鈕」。
2. 「攻擊命中」；沒有戰鬥就做「撿到東西、得分」。
3. 「玩家受傷」；沒有受傷機制就做「過關或失敗」。

### 3. 一次一個事件

對每個事件：在事件發生的那一行旁邊加上配方，確認能編譯，再做下一個。一般加在那一行後面；如果那一行可能會把目標 `Destroy`（例如 `TakeDamage` 裡面會判斷死亡），就加在它前面。先用下表的一行配方；使用者想細調某一項時，再改用對應的 Skill。

| 事件 | 一行配方 | 想細調時看 |
|---|---|---|
| 攻擊命中 | `Juice.Hit(target.gameObject, hitPoint);` | `juice-hit-stop`、`juice-screen-shake`、`juice-hit-flash`、`juice-particles` |
| 玩家受傷 | `Juice.Hurt(gameObject);` | 同上 |
| 起跳 | `Juice.Jump(transform);` | `juice-squash-stretch`、`juice-jump-forgiveness` |
| 落地 | `Juice.Land(transform);` | `juice-squash-stretch` |
| 開火 | `JuiceShake.Kick(-aimDirection, 0.15f);` 加上開火音效 | `juice-screen-shake`、`juice-sfx` |
| 撿到東西 | `Juice.Pickup(transform.position, null, "+1");` | `juice-particles`、`juice-sfx` |
| 連續撿金幣 | `Juice.Coin(transform.position, combo);` | `juice-sfx` |
| 死亡、爆炸 | `Juice.Explode(transform.position);` | `juice-particles`、`juice-screen-shake` |
| 顯示傷害 | `Juice.Damage(hitPoint, amount, isCritical);` | `juice-ui` |
| 過關 | `Juice.Win(player.position);` | `juice-hit-stop` |
| 失敗 | `Juice.Lose();` | |
| UI 按鈕 | 在按鈕上加 `JuiceButton` 元件 | `juice-ui` |
| 分數、金幣數字 | 在文字上加 `JuiceCounter`，改呼叫 `counter.Set(score)` | `juice-ui` |
| 換場景 | `JuiceFade.LoadScene("場景名");` | `juice-ui` |
| 對話文字 | 在文字上加 `JuiceTypewriter`，改呼叫 `typewriter.Play(line)` | `juice-ui` |

幾個常見的細節：

- **命中點**：有碰撞資訊就用接觸點（`collision.GetContact(0).point`）。只有 Trigger、沒有接觸點時，用子彈或被打物件的位置。
- **死亡**：在死亡的函式裡、`Destroy` 之前加 `Juice.Explode(transform.position);`。`Juice.Hit` 照常留在命中的地方，不用為了最後一擊另外判斷。
- **落地**：通常沒有現成的那一行。在更新「是否在地上」的地方記下上一格的值，「上一格不在地上、這一格在地上」就是落地。把「是否在地上」的初始值設成 true，開場第一格才不會誤播。
- **角色變形**：`Juice.Jump`、`Land`、`Hit`、`Hurt` 會讓物件變形，Kit 會自己挑安全的物件。角色的圖和會掉落的剛體在同一個物件上時不會變形（Console 會提示一次，塵土與音效照常）。你能操作編輯器時，把圖移到子物件；不能的話，在回報裡告訴使用者「把 Sprite 移到子物件就會有擠壓拉伸」。不要為了有變形而去縮放帶碰撞框的物件。

### 3.5 有暫停選單就一定要做

`Juice.Hit`、`Hurt`、`Explode`、`Win`、`Lose` 會動到 `Time.timeScale`（頓幀或慢動作）。只要加了其中任何一個，就搜尋專案裡把 `Time.timeScale` 設成 0 的地方，在進入暫停之前加一行：

```csharp
if (paused) JuiceTime.Cancel();   // 進入暫停前，先結束進行中的頓幀與慢動作
Time.timeScale = paused ? 0f : 1f;
```

不加的話，玩家剛好在頓幀的那零點幾秒內按下暫停，頓幀結束時會把時間恢復，遊戲就在暫停選單開著的狀態下繼續跑。

### 4. 強度要有層次

小事小回饋、大事大回饋。每個動作都大震動，重要的事反而沒感覺。

| 事情的大小 | 例子 | 震動 | 頓幀 |
|---|---|---|---|
| 每秒好幾次 | 走路、一般射擊、撿金幣 | 0 | 0 |
| 小 | 輕擊、落地 | 0–0.2 | 0–0.03 秒 |
| 中 | 一般命中、撿到重要道具 | 0.25–0.4 | 0.05–0.08 秒 |
| 大 | 玩家受傷、擊殺 | 0.45–0.6 | 0.08–0.12 秒 |
| 特大 | Boss 死亡、大爆炸、過關 | 0.7–1 | 0.15–0.2 秒 |

配方最後一個參數 `power` 就是用來做層次的：`Juice.Hit(target, point, 0.5f)` 是輕擊，`2f` 是重擊。

### 4.5 角色沒有臉的時候：加眼睛

主角或敵人只是方塊、圓球、膠囊的時候，加上眼睛是最便宜的「個性」。只在使用者想要、或物件明顯是角色時才加，不要替子彈、牆壁、道具加。

```csharp
JuiceEyes eyes = JuiceEyes.Attach(transform);             // 加上眼睛。第二次呼叫會拿回同一個
JuiceEyes.Attach(transform, 0.3f, 0.5f, true);            // 眼睛直徑、兩眼距離、要不要嘴巴
eyes.LookAt(player);                                      // 一直看著某個物件，也可以給座標
eyes.Squint();                                            // 被打到的那一行
eyes.Widen();                                             // 危險靠近、驚訝的那一行
```

眼睛直徑大約是身體寬度的四分之一。身體很扁時把 `eyes.mouthDrop` 設成 0.1，嘴巴會夾在兩眼中間。眼睛是 2D 的，用在 2D 遊戲或永遠正對鏡頭的物件。

### 5. 無障礙

專案有設定選單時，建議把 `JuiceSettings.ScreenShake`、`JuiceSettings.Flashes`、`JuiceSettings.Intensity` 接上去，讓玩家可以關掉震動與閃光。沒有設定選單就在回報裡提一句。

### 6. 回報

結束時告訴使用者：

- 改了哪些檔案、各加了哪一行。
- 每個事件現在有哪些回饋；想更強或更弱要改哪個數字。
- 清單裡還沒做的事件。
- 請他實際玩一次，再找一個沒玩過的人玩：自己覺得順不算，別人覺得順才算。

## 規則

- **不改玩法與數值**：不動傷害、速度、判定、關卡。Juice 只是放大感受。
- **不重構**：只在事件那一行加呼叫，不搬動原本的程式。
- **不刪原有的回饋**：專案已經有音效或粒子就保留，補上缺的那幾種就好。已經有音效的事件，不要再疊一個 `JuiceSfx.Blip`，改用 `JuiceSfx.Play(原本的 clip)` 讓它有音高隨機，或直接跳過聲音。
- **遊戲資訊優先於特效**：粒子與閃光不能擋住玩家需要看清楚的東西（敵人、子彈、平台邊緣）。
- Juice 是放大器，救不了核心不好玩的遊戲。如果盤點時發現玩家根本不知道目標是什麼、或失敗後要等很久才能重來，先在回報裡指出來。
