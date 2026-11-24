# 線上 Demo 是怎麼做的

線上 Demo：<https://yayapipi.github.io/unity-juice-skills/>

它是 Juice Kit 的示範場景（`JuiceDemo`）打包成 WebGL 的結果，檔案放在這個 Repo 的 `gh-pages` 分支，由 GitHub Pages 提供。

這個資料夾有兩樣東西：

- `Editor/JuiceWebDemoBuilder.cs`：建置腳本。它建立一個只有 `JuiceDemo` 元件的場景，設定 WebGL 選項，然後打包。
- `WebGLTemplates/JuiceDemo/index.html`：示範頁的範本，Unity 打包時會把檔名填進去。

## 自己打包一次

需要 Unity 6 與它的 Web 建置模組（Unity Hub ▸ Installs ▸ Add modules ▸ Web Build Support）。

1. 準備一個 Unity 專案，把 `skills/juice-kit/unity/Juice` 複製成 `Assets/Juice`。
2. 把 `webdemo/Editor` 複製成 `Assets/JuiceWebDemo/Editor`，把 `webdemo/WebGLTemplates` 複製成 `Assets/WebGLTemplates`。
3. 在編輯器選單按 `Tools ▸ Juice ▸ Build Web Demo`，或用命令列：

```bash
JUICE_WEB_OUT=/tmp/juice-web Unity -batchmode -projectPath <專案路徑> -buildTarget WebGL -executeMethod JuiceWebDemoBuilder.Build -quit -logFile build.log
```

輸出在 `JUICE_WEB_OUT` 指定的資料夾；沒有指定時是專案裡的 `Builds/JuiceWebDemo`。用任何靜態網頁伺服器開啟就能玩，例如在輸出資料夾執行 `python3 -m http.server`。

## 幾個設定的原因

- **Brotli 壓縮，加上內建的解壓縮程式**（Decompression Fallback）：GitHub Pages 不會替 `.br` 檔加上 `Content-Encoding` 標頭，少了這個選項瀏覽器會讀不懂檔案。
- **Run In Background 打開**：點到網頁其他地方時，示範不會停住。
- **畫面上的開關只有英文**：網頁版只能用打包進去的字型，Unity 內建字型沒有中文字，所以中文說明寫在網頁上。你的遊戲要在網頁版顯示中文，請把中文字型放進專案，再指定給 `JuiceAssets.Font` 與你自己的介面。

## 發布到 GitHub Pages

把輸出資料夾的內容放到 `gh-pages` 分支的根目錄，另外加一個空的 `.nojekyll` 檔。這個分支每次更新都只留一個 commit（強制推送），Repo 才不會越來越大。
