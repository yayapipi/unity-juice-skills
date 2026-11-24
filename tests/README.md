# 測試

`JuiceSelfTest.cs` 會在 Play 模式下把 Juice Kit 的每個功能實際跑一遍，並檢查結果（縮放有沒有回到原狀、時間有沒有恢復、閃白時畫面上的像素是不是真的變白…），共 90 項。

這個資料夾**不需要**放進你的遊戲專案。

## 怎麼跑

1. 準備一個 Unity 專案，把 `skills/juice-kit/unity/Juice` 複製到 `Assets/Juice`。
2. 把 `JuiceSelfTest.cs` 與 `Editor/` 複製到 `Assets/JuiceTests/`。
3. 用批次模式執行（不要加 `-quit`，跑完會自己結束）：

```bash
JUICE_TEST_OUT=/tmp/juice-out Unity -batchmode -projectPath <專案> -executeMethod JuiceTestRunner.Run -logFile /tmp/juice-out/unity.log
```

結束碼是失敗的項目數，`results.txt` 會寫進 `JUICE_TEST_OUT` 指定的資料夾（要先建立）。

## 測過的環境

| Unity | 渲染管線 | 結果 |
|---|---|---|
| 6000.5.2f1（macOS） | URP 17.5 | 全部通過 |
| 6000.5.2f1（macOS） | 內建管線 | 全部通過 |

其他版本沒有實際測過。遇到問題歡迎開 Issue。
