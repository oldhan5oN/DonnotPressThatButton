# DonnotPressThatButton

一个 Unity 项目。

## 技术栈

- Unity 6000.x（URP）
- Input System
- TextMesh Pro

## 协作说明：资源文件通过 Release 分发

本仓库**不包含** `Assets/Res/` 目录下的实际二进制资源文件（模型、贴图、字体、材质、Prefab、场景等），以避免 Git LFS 容量不足。

仓库中保留了 `Assets/Res/` 下所有文件的 `.meta`，以确保 Unity 的 GUID 引用不会丢失。

### 首次克隆后

1. 打开本仓库的 [Releases](https://github.com/oldhan5oN/DonnotPressThatButton/releases) 页面。
2. 下载最新版本的 `DonnotPressThatButton-Res-vX.X.X.zip`。
3. 在项目根目录解压：

   ```bash
   unzip -o DonnotPressThatButton-Res-v0.1.0.zip -d Assets/Res/
   ```

   或者手动将 zip 内的内容拖入 `Assets/Res/` 文件夹。

4. 打开 Unity，场景和 Prefab 中的资源引用应保持完整。

> **注意**：Res 包必须解压到 `Assets/Res/` 路径下，不能改名或移动到 `Assets/Resources/` 等其他位置，否则 GUID 引用会失效。

## 项目结构

```
Assets/
├── Editor/          # 编辑器扩展脚本
├── Game/            # 运行时游戏逻辑
├── Res/             # 二进制资源（通过 Release 分发）
├── Scenes/          # 场景文件
├── Scripts/         # 通用脚本
├── TextMesh Pro/    # TMP 资源
└── 清单/            # 项目清单/文档
```

## 更新 Res 资源后

如果你修改了 `Assets/Res/` 下的资源并想同步给其他协作者：

1. 确保 `.meta` 文件已提交到本仓库（这些文件会随代码一起 push）。
2. 重新打包 `Assets/Res/`（不包含 `.meta`）：

   ```bash
   cd Assets/Res
   zip -r ../../DonnotPressThatButton-Res-vX.X.X.zip . -x "*.meta"
   ```

3. 在 GitHub 上新建 Release，上传新的 zip 包，并更新版本号。

## 注意事项

- 不要删除或修改 `Assets/Res/` 下的 `.meta` 文件，否则会导致场景/Prefab 引用丢失。
- 如果从 Release 导入资源后 Unity 提示 Missing，请检查：
  - Res 包是否解压到了正确的 `Assets/Res/` 路径。
  - 下载的 Res 包版本是否与代码仓库版本匹配。
