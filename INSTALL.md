# 給 AI Agent 的安裝步驟

你是 AI Agent，正在幫使用者把 Juice Skills 裝進他的 Unity 專案。照順序做完，最後回報。

## 1. 找到 Unity 專案

Unity 專案的根目錄是同時有 `Assets/` 與 `ProjectSettings/` 的資料夾。目前的工作目錄不是的話，往上層或下層找。找到不只一個就問使用者要裝在哪一個。

## 2. 取得這個 Repo

如果你正在讀的就是本機的這份檔案，跳過這一步。

把 Repo 放在**專案外面**的暫存位置（不要放進 `Assets/`，否則 Unity 會把說明檔與測試一起匯入）：

```bash
git clone --depth 1 https://github.com/yayapipi/unity-juice-skills
```

沒有 git 的話，下載 `https://github.com/yayapipi/unity-juice-skills/archive/refs/heads/main.zip` 再解壓縮。

## 3. 把 Skills 複製到你會讀取的資料夾

把 Repo 裡 `skills/` 底下的 12 個資料夾，整個複製到專案根目錄下、你這個 Agent 讀取 Skills 的位置。子資料夾（尤其是 `juice-kit/unity/`）要一起複製。

| Agent | 位置 |
|---|---|
| Codex、Cursor、Gemini CLI、GitHub Copilot、Antigravity、OpenCode | `.agents/skills/` |
| Claude Code | `.claude/skills/` |
| Windsurf | `.windsurf/skills/` |
| Trae | `.trae/skills/` |
| 其他，或你不確定 | `.agents/skills/` |

完成後應該長這樣（以 `.agents/skills/` 為例）：

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

專案裡已經有同名的資料夾（之前裝過）就整個覆蓋。

## 4. 安裝 Juice Kit

照 `skills/juice-kit/SKILL.md` 的步驟做。重點：把 `skills/juice-kit/unity/Juice/` 整個複製成 `<Unity 專案>/Assets/Juice/`，`.meta` 檔也要一起複製。

## 5. 確認

- Unity 重新編譯之後 Console 沒有錯誤。你有工具可以讀 Console（Unity CLI、Unity MCP）就自己確認；沒有就請使用者切回 Unity 看。
- `Assets/Juice/Juice.cs` 存在。

## 6. 收尾

刪掉第 2 步的暫存資料夾，然後告訴使用者：

- 裝了 12 個 Skill 與 Juice Kit，各放在哪裡。
- 有些 Agent 要重新開啟對話或重新啟動才會讀到新的 Skill。
- 接下來可以直接說：「幫我的遊戲加 Juice」，或是「在玩家受傷的時候加上鏡頭震動」。
- Unity 選單 `Tools ▸ Juice ▸ Create Demo Scene` 可以看示範。

不要順手幫使用者的遊戲加任何效果；這一步只負責安裝。
