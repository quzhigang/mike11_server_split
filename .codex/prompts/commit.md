---
description: 为当前项目的未提交修改生成并执行一次规范提交
---

请为当前工作区的未提交修改创建一次新的 commit。

## 执行要求

1. 先检查当前目录是否为 git 仓库
2. 如果是 git 仓库，再运行：
   - `git status`
   - `git diff HEAD`
   - `git status --porcelain`
3. 明确本次提交包含哪些文件，避免把无关文件混入
4. commit message 应准确反映当前工作的类型，例如：
   - `feat: ...`
   - `fix: ...`
   - `refactor: ...`
   - `docs: ...`
   - `chore: ...`
5. 如果本次工作主要是大文件拆分、结构整理、方法归位，优先使用：
   - `refactor`
   - `docs`
6. 不要使用含糊的提交信息，例如“update”或“修改一下”

## 针对当前项目的提交建议

本项目当前常见提交类型包括：

- `refactor`: 拆分 `Res11`、`Nwk11` 等大文件
- `docs`: 更新拆分计划、README、AGENTS、提示词
- `fix`: 修正拆分过程中引入的引用、重复定义、方法归类问题

## 输出要求

- 先简要说明准备提交的变更范围
- 给出最终采用的 commit message
- 如果成功提交，报告 commit hash
- 如果当前目录不是 git 仓库，明确说明并停止，不要伪造提交结果
