# 可以直接複製的 Prompt

把整段複製、貼給你的 AI Agent（Codex、Claude Code、Cursor…）就可以用。方括號裡的字換成你自己的遊戲內容。

## 安裝（第一次用）

```text
請幫我安裝 Juice Skills：先 git clone https://github.com/yayapipi/unity-juice-skills ，再照裡面 INSTALL.md 的步驟，把 Skills 和 Juice Kit 裝進這個 Unity 專案。
```

## 不知道從哪裡開始

```text
用 juice-audit 檢查我的遊戲，列出最值得加回饋的事件與優先順序，先不要改程式。
```

```text
用 juice-audit 幫我的遊戲加 Juice，先做優先度最高的三個事件。不要改玩法與數值。
```

## 一個事件，一次加好

```text
在[玩家的子彈打中敵人]的時候加上命中回饋：頓幀、鏡頭震動、敵人閃白、火花和音效。
```

```text
在[玩家被敵人碰到]的時候加上受傷回饋，要比打中敵人更重，閃紅色。
```

```text
在[角色起跳和落地]的時候加上擠壓拉伸、腳邊的塵土和音效。
```

```text
在[敵人死掉]的時候加上爆炸：粒子、比較大的鏡頭震動、頓幀 0.1 秒。
```

```text
在[撿到金幣]的時候加上小粒子、+1 的飄字和音效，連續撿的時候音高要越來越高。
```

```text
在[過關]的時候噴彩帶、慢動作 0.4 秒，並用淡出淡入切到下一關。
```

## 單一效果

```text
用 juice-screen-shake，在[爆炸]的時候加鏡頭震動，強度 0.6。
```

```text
用 juice-hit-stop，在[重擊命中]的時候頓幀 0.1 秒。我的遊戲有暫停選單，請一起處理。
```

```text
用 juice-hit-flash，讓所有敵人被打到的時候閃白 0.06 秒。
```

```text
用 juice-squash-stretch，讓[史萊姆]每次跳起來和落地都有彈性。
```

```text
[敵人]出場的時候不要直接出現，從畫面上方用彈簧掉進來，一隻一隻有時間差。被打到但沒死的時候左右晃一下。
```

```text
替[主角]加上一對眼睛，一直看著[最近的敵人]，被打到的時候瞇一下，敵人靠近的時候睜大。
```

```text
用 juice-particles，幫[玩家的子彈]加上青色的拖尾，打到牆壁時噴火花。
```

```text
用 juice-sfx，幫所有還沒有聲音的動作先加上內建的合成音效，已經有音效的改成音高隨機。
```

```text
用 juice-sfx 加上背景音樂。我還沒有音樂檔，先用內建的；過關的時候音樂要壓低一下。
```

## 介面與劇情

```text
用 juice-ui，幫場景裡所有的按鈕加上滑過放大、按下回彈和點擊音。
```

```text
用 juice-ui，讓分數用滾動的方式跳上去，每次加分彈一下。
```

```text
用 juice-ui，把對話改成打字機效果，玩家點一下可以直接顯示整句；角色說話時立繪彈一下。
```

```text
用 juice-ui，把所有換場景的地方改成淡出淡入。
```

## 操作與鏡頭

```text
用 juice-jump-forgiveness，幫我的角色控制器加上土狼時間和跳躍緩衝，不要改變原本的跳躍高度。
```

```text
用 juice-camera-follow，讓鏡頭有阻尼地跟著玩家，並稍微看向前進的方向。
```

```text
把玩家的圖移到子物件，讓起跳和落地的擠壓拉伸可以生效，Animator 和腳本裡的引用也一起改好。
```

## 自己調數值

```text
用 juice-feedback，把[命中、起跳、落地]的回饋做成 JuiceFeedback 元件，我要在 Inspector 自己調數值。
```

```text
現在的鏡頭震動太強了，全部調弱一半；頓幀再長一點點。
```

## 無障礙與收尾

```text
在設定選單加上「畫面震動」和「閃光效果」兩個開關，接到 JuiceSettings。
```

```text
列出目前專案裡所有用到 Juice 的地方，整理成一張表：事件、用了哪些回饋、數值。
```

## 沒有安裝 Skill 也能用的寫法

AI 沒辦法安裝 Skill（例如網頁版的聊天機器人）時，把 `skills/juice-kit/unity/Juice` 資料夾放進專案的 `Assets/Juice`，再直接講類別名稱：

```text
我的專案有 Juice Kit（Assets/Juice）。請在[玩家攻擊命中]的那一行加上 Juice.Hit(target.gameObject, hitPoint); 並告訴我每個數值調大調小的差別。
```
