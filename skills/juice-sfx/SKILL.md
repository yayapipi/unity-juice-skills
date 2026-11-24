---
name: juice-sfx
description: 在 Unity 加上音效回饋：每次播放音高隨機（連打不膩）、連擊時音階上升、十種不需要音檔的內建合成音效（按鈕、跳躍、落地、命中、受傷、撿取、金幣、爆炸、升級、失敗）。不用在場景裡放 AudioSource。使用者提到音效、沒有聲音、聲音很單調、音高隨機、連擊音效、按鈕音、SFX、sound effect、pitch variation、audio feedback 時使用。
license: MIT
metadata:
  author: yayapipi
  version: "1.0.0"
---

# 音效

如果只能加一種 Juice，加音效。沒有聲音的動作，等於沒發生。

## 前置

專案裡要有 `Assets/Juice/`（Juice Kit）。沒有的話先照 `juice-kit` Skill 安裝。場景裡要有一個 `AudioListener`（預設的 Main Camera 上就有）。

## 最快的做法

```csharp
JuiceSfx.Blip(JuiceBlip.Jump);            // 還沒有音檔：先用內建的合成音
JuiceSfx.Play(jumpClip);                  // 有音檔：播放，音高自動在 0.9–1.1 倍之間隨機
JuiceSfx.PlayCombo(coinClip, combo);      // 連擊：combo 每加 1 升半音，最多升一個八度
```

不用加 `AudioSource`，Kit 會自己管理一組共用的播放器。

## 內建的十個合成音

`JuiceBlip.Click`、`Jump`、`Land`、`Hit`、`Hurt`、`Pickup`、`Coin`、`Explosion`、`PowerUp`、`Lose`

是程式即時產生的 8-bit 風格音效，用來「先有聲音」。之後有了正式音檔，把 `JuiceSfx.Blip(...)` 換成 `JuiceSfx.Play(clip)` 就好。

## 步驟

1. **盤點**：列出專案裡每個動作，標出哪些已經有聲音。每個動作都該有聲音：跳、落地、攻擊、命中、受傷、撿東西、按鈕、過關、失敗。
2. **已經有音效的動作**：把原本的 `audioSource.PlayOneShot(clip)` 或 `audioSource.Play()` 換成 `JuiceSfx.Play(clip);`，馬上就有音高隨機。背景音樂、環境音、3D 空間音效（需要聽出方向與距離的聲音）不要換，維持原本的 `AudioSource`。
3. **沒有音效的動作**：
   - 專案的 `Assets` 裡已經有合適的音檔：在腳本加 `public AudioClip jumpClip;`，呼叫 `JuiceSfx.Play(jumpClip);`，並把音檔指定上去（或告訴使用者要拖哪個檔案到哪個欄位）。
   - 沒有音檔：用 `JuiceSfx.Blip(...)`，並在回報裡告訴使用者可以到 sfxr.me 自己做一個再換上。
4. **會連續發生的事**（連續撿金幣、連擊、連消）：記一個 `combo` 計數，改用 `PlayCombo`；一段時間（約 1–2 秒）沒有再發生就把計數歸零。音高一路往上，玩家會想繼續連下去。
5. 確認能編譯，請使用者戴耳機或開喇叭試玩。

## 參數

```csharp
JuiceSfx.Play(clip, volume: 1f, pitchJitter: 0.1f, pitch: 1f);
JuiceSfx.Blip(JuiceBlip.Hit, volume: 0.5f, pitchJitter: 0.08f, pitch: 1f);
```

| 情境 | 建議 |
|---|---|
| 一般動作（跳、命中、撿取） | `pitchJitter` 0.08–0.12 |
| 腳步聲、連射 | `pitchJitter` 0.15–0.2，`volume` 低一點 |
| 有旋律的聲音（過關、升級） | `pitchJitter` 0，不要變調 |
| 大的東西、重擊 | `pitch` 0.7–0.85，聲音比較沉 |
| 小的東西、輕擊 | `pitch` 1.2–1.4 |

## 音量層次

每秒都會響的聲音（腳步、射擊）音量 0.2–0.4；偶爾才響的重要聲音（受傷、擊殺、過關）0.6–1。全部一樣大聲，重要的就聽不出來。

## 設定選單

```csharp
JuiceSfx.Volume = slider.value;   // 音效總音量 0–1
JuiceSfx.Muted = true;            // 靜音
```

這兩個值不會自動存檔，要存的話用 `PlayerPrefs`。專案已經用 AudioMixer 管音量的話，這個 Skill 的聲音不會經過 Mixer，要請使用者知道。

## 注意

- 同一個音效在 0.02 秒內重複播放會被擋掉，避免一次打到十個敵人時爆音。
- 音效不受 `Time.timeScale` 影響，頓幀與慢動作時照常播放。
- Web 版（WebGL）要等玩家第一次點擊或按鍵之後瀏覽器才允許出聲，這是瀏覽器的規定。

## 回報

列出每個動作現在用的是內建合成音還是音檔，並附上找免費音效的地方：sfxr.me（自己做 8-bit 音效）、freesound.org、kenney.nl。
