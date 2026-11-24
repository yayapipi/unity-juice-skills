# 給 AI Agent 的安裝步驟

你是 AI Agent，正在幫使用者把 Juice Skills 裝進他的 Unity 專案。照順序做完，最後回報。

## 1. 找到 Unity 專案

Unity 專案的根目錄是同時有 `Assets/` 與 `ProjectSettings/` 的資料夾。目前的工作目錄不是的話，往上層或下層找。找到不只一個就問使用者要裝在哪一個。

## 2. 取得這個 Repo

使用者的指令通常已經讓你執行過 `git clone`，那這一步就完成了。記下 clone 出來的資料夾在哪裡（通常是目前資料夾底下的 `unity-juice-skills`），下面稱它為 `<Repo>`，第 6 步要刪掉它。

- clone 在 Unity 專案資料夾裡面沒有關係，只要不是在 `Assets/` 底下。如果在 `Assets/` 底下，先把它移出來，否則 Unity 會把說明檔與測試一起匯入。
- 還沒 clone：`git clone --depth 1 https://github.com/yayapipi/unity-juice-skills`
- 沒有 git：下載 `https://github.com/yayapipi/unity-juice-skills/archive/refs/heads/main.zip` 再解壓縮。

## 3. 把 Skills 複製到你會讀取的資料夾

把 `<Repo>/skills/` 底下的 12 個資料夾，複製到專案根目錄下、你這個 Agent 讀取 Skills 的位置。子資料夾（尤其是 `juice-kit/unity/`）要一起複製。

| Agent | 位置 |
|---|---|
| Codex、Cursor、Gemini CLI、GitHub Copilot、Antigravity、OpenCode | `.agents/skills/` |
| Claude Code | `.claude/skills/` |
| Windsurf | `.windsurf/skills/` |
| Trae | `.trae/skills/` |
| 其他，或你不確定 | `.agents/skills/` |

以 `.agents/skills/` 為例（第一次安裝與之後更新都用同一組指令，同名檔案會被覆蓋）：

```bash
mkdir -p "<專案>/.agents/skills"
cp -R "<Repo>/skills/." "<專案>/.agents/skills/"
```

```powershell
New-Item -ItemType Directory -Force "<專案>\.agents\skills" | Out-Null
Copy-Item -Recurse -Force "<Repo>\skills\*" "<專案>\.agents\skills\"
```

完成後應該長這樣：

```text
<Unity 專案>/
├── .agents/skills/
│   ├── juice-audit/SKILL.md
│   ├── juice-kit/SKILL.md
│   ├── juice-kit/unity/Juice/...
│   └── ...（共 12 個資料夾）
├── Assets/
└── ProjectSettings/
```

## 4. 安裝 Juice Kit

把 `<Repo>/skills/juice-kit/unity/Juice/` **裡面的內容**複製到 `<專案>/Assets/Juice/`，`.meta` 檔也要一起複製：

```bash
mkdir -p "<專案>/Assets/Juice"
cp -R "<Repo>/skills/juice-kit/unity/Juice/." "<專案>/Assets/Juice/"
```

```powershell
New-Item -ItemType Directory -Force "<專案>\Assets\Juice" | Out-Null
Copy-Item -Recurse -Force "<Repo>\skills\juice-kit\unity\Juice\*" "<專案>\Assets\Juice\"
```

注意來源結尾的 `/.` 與 `\*`：少了它，在 `Assets/Juice` 已經存在時會複製成 `Assets/Juice/Juice`，腳本重複、整個專案編譯失敗。

專案沒有 Unity UI（`Packages/manifest.json` 裡沒有 `com.unity.ugui`）時，刪掉 `Assets/Juice/UI/` 與 `Assets/Juice/UI.meta`。其餘細節見 `skills/juice-kit/SKILL.md`。

## 5. 確認

- `Assets/Juice/Juice.cs` 存在，而且**沒有** `Assets/Juice/Juice/` 這個多出來的資料夾。
- Unity 重新編譯之後 Console 沒有錯誤。你有工具可以讀 Console（Unity CLI、Unity MCP）就自己確認；沒有就請使用者切回 Unity 看。

## 6. 收尾

刪掉 clone 下來的 `<Repo>` 資料夾（第 2 步記下的位置，不管它在專案裡面還是外面），然後告訴使用者：

- 裝了 12 個 Skill 與 Juice Kit，各放在哪裡。
- 有些 Agent 要重新開啟對話或重新啟動才會讀到新的 Skill。
- 接下來可以直接說：「幫我的遊戲加 Juice」，或是「在玩家受傷的時候加上鏡頭震動」。
- Unity 選單 `Tools ▸ Juice ▸ Create Demo Scene` 可以看示範。

不要順手幫使用者的遊戲加任何效果；這一步只負責安裝。
