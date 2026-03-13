## Codex 自定义命令

本目录保存项目级的 Codex custom prompts 源文件。

注意事项：

- Codex 当前只会扫描 `~/.codex/prompts` 顶层的 `.md` 文件。
- 仓库内的 `.codex/prompts/` 不会被自动加载。
- 因此本仓库使用 `sync-prompts.ps1` 将项目内 prompts 同步到本机 `~/.codex/prompts`。

首次使用：

```powershell
powershell -ExecutionPolicy Bypass -File .\.codex\sync-prompts.ps1
```

同步完成后，重启 `codex` 会话，再通过 `/prompts:<name>` 调用。

当前命令名：

- `commit`
- `prime`
- `system-review`
- `github-bug-fix-implement-fix`
- `github-bug-fix-rca`
- `validation-code-review-fix`
- `validation-code-review`
- `validation-execution-report`
- `validation-validate`
