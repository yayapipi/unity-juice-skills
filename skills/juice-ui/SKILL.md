---
name: juice-ui
description: 讓 Unity 的介面與劇情演出有手感：按鈕滑過放大與按下回彈、分數數字滾動、傷害數字與飄字、視窗與立繪彈出、對話打字機效果、淡出淡入轉場。適合選單、經營、卡牌、視覺小說與劇情遊戲。使用者提到按鈕沒反應、UI 很死、選單動畫、數字跳動、傷害數字、飄字、打字機、對話逐字出現、立繪、轉場、淡入淡出、UI juice、button feedback、damage numbers、typewriter、fade 時使用。
license: MIT
metadata:
  author: yayapipi
  version: "1.0.1"
---

# 介面與劇情演出

UI 是玩家看最久的地方。劇情、經營、卡牌類的遊戲沒有打擊感可以做，Juice 幾乎全在這裡。

## 前置

專案裡要有 `Assets/Juice/`（Juice Kit）。沒有的話先照 `juice-kit` Skill 安裝。按鈕與轉場需要 Unity UI（uGUI）；UI Toolkit 做的介面不適用這個 Skill。

這個 Skill 大多是「掛元件」。你能操作編輯器（Unity CLI、Unity MCP）就自己掛；不能的話，用程式在 `Awake` 裡 `AddComponent`，或把操作步驟寫給使用者。

## 按鈕：JuiceButton

在每個 UI 按鈕上加 `JuiceButton` 元件，不用寫程式。滑過放大到 1.05 倍、按下縮到 0.95 倍、放開彈回，按下時有「喀」一聲；手把與鍵盤選到按鈕時也會放大。

一次加到場景裡所有按鈕：

```csharp
foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include))
    if (b.GetComponent<JuiceButton>() == null) b.gameObject.AddComponent<JuiceButton>();
```

比較舊的 Unity 編譯不過時，改成 `FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)`；再不行就用 `FindObjectsOfType<Button>(true)`。

欄位：`hoverScale`（1.05）、`pressScale`（0.95）、`stiffness` 與 `damping`（彈簧的硬度與阻尼，`damping` 調小會抖比較久）、`playSound`、`clickClip`（自己的按鈕音效，留空用內建合成音）。專案的按鈕已經有自己的點擊音效時，把 `playSound` 關掉。

不是 UI 的可點擊物件（場景裡的選項、卡牌）：在被點到的地方呼叫 `Juice.Click(transform);`。

## 數字滾動：JuiceCounter

掛在顯示分數、金幣的文字物件上（TextMeshPro 或 UI Text 都可以），把原本的 `text.text = score.ToString();` 換成：

```csharp
counter.Set(score);        // 從目前顯示的數字滾到 score，同時彈一下
counter.Add(10);           // 加 10
counter.SetInstant(0);     // 直接設定，不播動畫（開場、讀檔時用）
```

欄位：`format`（預設 `{0}` 跟原本的顯示一樣；`{0:N0}` 會加千分位；也可以寫 `分數：{0}`）、`duration`（0.4 秒）、`punch`（彈多大，0 = 不彈）。

不能操作編輯器時，在用到它的腳本裡自動掛上，使用者之後自己在 Inspector 掛了也不會重複：

```csharp
JuiceCounter counter;

void Awake()
{
    counter = scoreText.GetComponent<JuiceCounter>();
    if (counter == null) counter = scoreText.gameObject.AddComponent<JuiceCounter>();
}
```

## 傷害數字與飄字：JuiceFloatingText

```csharp
Juice.Damage(hitPoint, amount, isCritical);                       // 傷害數字，暴擊會變黃、放大
JuiceFloatingText.Spawn(position, "+1");                          // 任何文字
JuiceFloatingText.Spawn(position, "Perfect!", Color.yellow, 1.5f);  // 顏色、大小
```

位置是世界座標，文字會面向鏡頭往上飄、放大再淡出，用完自動回收。不需要 TextMeshPro。Web 版要顯示中文，先指定字型：`JuiceAssets.Font = 你的字型;`（內建字型在瀏覽器裡沒有中文）。

## 彈出：JuicePop

掛在視窗、對話框、立繪、道具圖示上，每次 `SetActive(true)` 就會從小彈到原本的大小。多個物件設不同的 `delay`（例如 0、0.05、0.1），就會一個接一個彈出來。角色說話時讓立繪彈一下：`JuiceSquash.Punch(portrait, 0.06f);`

## 打字機：JuiceTypewriter

掛在對話的文字物件上，把原本的 `text.text = line;` 換成：

```csharp
typewriter.Play(line);

// 玩家點一下：還在打字就直接顯示整句，已經顯示完才換下一句
if (typewriter.IsPlaying) typewriter.Complete();
else ShowNextLine();
```

欄位：`charsPerSecond`（中文 20–30、英文 40–60）、`punctuationPause`（標點多停幾秒）、`blip` 與 `blipEvery`（每幾個字響一聲）、`onComplete`（整句顯示完時觸發，可以用來顯示「繼續」的箭頭）。建議搭配 TextMeshPro：Rich Text 標籤（顏色、粗體）不會被拆開。

## 轉場：JuiceFade

```csharp
JuiceFade.LoadScene("Level2");                 // 淡出 → 換場景 → 淡入，取代 SceneManager.LoadScene
JuiceFade.Out(0.4f, Color.black, () => {       // 自己控制：全黑的時候做事
    SwitchRoom();
    JuiceFade.In(0.4f);
});
```

不用自己做黑幕。淡出期間會擋住下面的 UI 點擊。顏色可以換，回憶場景用白色。

## 血條殘影

不需要新元件：在血條底下多放一條白色或淡色的 Image 當殘影，扣血時紅條立刻變短，殘影用補間慢慢追上：

```csharp
healthFill.fillAmount = target;
float from = ghostFill.fillAmount;
JuiceRunner.Tween(0.5f, e => ghostFill.fillAmount = Mathf.Lerp(from, target, e), JuiceEaseType.InOutQuad, true);
```

## 注意

- 這裡的元件預設都用 unscaled time，暫停選單與頓幀期間照樣會動。
- 物件的 Scale 被 Animator 或 Layout Group 控制時，縮放類的效果會被蓋掉；改掛在裡面一層的子物件。
- 不要讓 UI 動畫擋住操作：按鈕回彈在 0.15 秒內、視窗彈出在 0.3 秒內。

## 回報

列出哪些物件掛了哪個元件、哪些程式改了呼叫方式，以及每個效果想調整時看哪個欄位。
