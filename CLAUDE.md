# 《别按那个键》(Don't Press That Button) — 项目架构总览

> 给新会话 AI 的快速上手文档。完整规划见 [程序模块清单](Assets/清单/程序模块清单.md) 与 [玩法流程](Assets/清单/玩法流程.txt)。

## 一、游戏概述

第一人称叙事解谜（步行模拟器 + 资源管理），单场景、单周目、约 10–20 分钟。
太空船「探索号」，玩家醒来后在休眠舱 + 设备室两房间内活动。

- **核心机制**：**电量（初始 15%）是唯一硬预算**，所有动作共用这笔电——制氧 3% / 电击恢复精神 2% / 解锁舱门 1% / 广播 10%。
- **三资源**：电能 / 氧气 / 精神。氧气随时间衰减；氧气过低时精神加速下降；制氧/电击各自回升但都耗电。
- **三结局**：① 广播成功（耗 10% 电，留信息后电源熄灭）；② 开右门跳入宇宙自杀；③ 失败（电量跌破 10% 或终端短路）→ 失败态被字幕怂恿自杀。

## 二、技术栈与工程约定

- **Unity 6 / URP 17.3.0**
- **Input System 1.18**：生成类 `InputSystem_Actions`（全局命名空间），位于 `Assets/Scripts/Player/`。改键位编辑 `.inputactions` 资产即可，无需重新生成 `.cs`。当前 Interact 绑定 `F`（已去掉 Hold）。
- **UGUI 2.0 + TextMeshPro**（`TMP_Text`）。
- **无 asmdef**：所有脚本编进 `Assembly-CSharp`，可直接互相引用。
- 不做存档。

## 三、目录与命名空间分层

`Assets/Scripts/` 按层分目录，命名空间统一 `DPTB.*`：

| 目录 | 命名空间 | 职责 |
|---|---|---|
| `Player/` | `DPTB.Player` | 第一人称控制 + 输入生成类 |
| `Interaction/` | `DPTB.Interaction` | 交互接口、射线探测、提示 UI |
| `Resource/` | `DPTB.Resource` | 三资源单一数据源 + 配置 SO |
| `Game/`（含 `States/`） | `DPTB.Game` | 游戏流程状态机、结局分发 |
| `Devices/` | `DPTB.Devices` | 耗电设备（制氧/电击/门） |
| `Terminal/` | `DPTB.Terminal` | 终端打字子系统（最复杂） |
| `UI/`、`UI/Subtitle/` | `DPTB.UI` | HUD、设备弹窗、字幕系统 |
| 根 `RetroEffect*` | （全局） | 已有的 URP 复古后处理特效 |

## 四、核心架构（已实现，按数据流：输入→交互→资源→状态→UI/字幕）

### 玩家 / 输入
- `FirstPersonController`：基于 `CharacterController` 的 WASD + 鼠标 look。API：`SetControlEnabled(bool)`、`static SetCursorLocked(bool)`、`CameraPivot`。

### 交互系统（开闭核心）
- `IInteractable` 接口 + `InteractableBase` 抽象基类（**模板方法**：`Interact()` 检查 `CanInteract` → `OnInteract()`）。
- `InteractionRaycaster`：屏幕中心射线，`event Action<IInteractable> FocusChanged`，按 F 触发。
- 新增设备只需继承基类，不改核心（**开闭原则 OCP**）。

### 资源系统（单一数据源 Single Source of Truth）
- `ResourceSystem`（**单例**）：电/氧/精神。`event` `PowerChanged/OxygenChanged/MentalChanged`（归一化 0..1）、`PowerSpendFailed`。
- **统一耗电入口** `TrySpendPower(cost)`——所有设备走此 API。
- `ResourceConfig`（**SO**，数据驱动）：初始值、衰减率、各动作消耗、阈值。

### 游戏状态机（State）
- `GameManager`（**单例**）持 `GameStateMachine` + 三状态 `PlayingState`/`FailedState`/`GameOverState`。
- 结局上报入口：`ReportBroadcastSuccess/ReportShortCircuit/ReportPowerDepleted/ReportSuicide`。
- `event` `PhaseChanged(GamePhase)`、`GameEnded(EndingType)`。枚举见 `GamePhase.cs`（`GamePhase{Playing,Failed,Over}`、`EndingType{None,BroadcastSuccess,Suicide}`、`FailureCause`）。
- 注意：`BroadcastSuccess` 与 `Suicide` 都走 `phase=Over`，**区分结局须用 `GameEnded(EndingType)` 而非 `PhaseChanged`**。

### 耗电设备（模板方法复用）
- `PowerConsumingDevice`（抽象基类）：固定「弹窗 → 扣电 → `OnPowered()`/`OnInsufficientPower()`」骨架，子类只填 `Cost` 与 `OnPowered()`。
- `OxygenGenerator`（制氧）、`ElectroshockDevice`（电击回精神）、`RightDoorButton`（门**自身**即耗电设备：未解锁→弹窗扣电解锁，解锁后→开门自杀；`suicideOnUnlock` 开关切换一步/两步）。
- `DevicePopupUI`（**单例**模态弹窗）：`Show(title, body, onConfirm)`，打开时冻结玩法输入 + 释放光标。

### 终端打字子系统（MVC）
- `TerminalScreen`（**View**）：富文本渲染，正确=全亮 / 错按=红 `*` / 未输入=低透明。
- `VirtualKeyboard` + `KeyboardKey`（**View**）：手摆的透明按钮，悬停微亮、随时间随机「短路」染红。
- `TypingController`（**Controller**）：首字母映射校验，错按不阻断、短路键即失败。**支持多段 `List<TerminalData>` 顺序输入、进度跨会话持久化（备忘录式）、完成 `requiredCompletedCount`（默认 2）段即可广播**。`event` `Completed`、`SegmentCompleted(int)`，属性 `CanBroadcast`/`AllCompleted`。
- `TerminalData`（**SO**）：`broadcastText`（逐字汉字）+ `targetLetters`（等长字母映射，空格=自动）。
- `BroadcastButton`：两段式（开玻璃罩 → 校验 `CanBroadcast` → 扣 10% → 广播成功）。
- `TerminalInteractable`：终端世界入口，开/关面板并冻结玩法输入。

### UI / 字幕
- `HUD`：订阅 `ResourceSystem` 三事件，`Image.fillAmount` 显示三条。
- **字幕系统（三层，单一职责）**：
  - `SubtitleTrigger`（**触发层 / 软引导大脑**）：订阅资源阈值、`GameManager.PhaseChanged`/`GameEnded`、`TypingController.Completed`，按 Inspector 规则推字幕。
  - `SubtitleSystem`（**单例 / 播放层**）：单条居中打字机、优先级队列 + 高优先打断、`CanvasGroup` 淡入淡出。`Show(SubtitleId)`/`Show(SubtitleLine)`/`Clear()`。
  - `SubtitleTable`（**SO，注册表 Registry**）+ `SubtitleId`（**enum 主键**，类型安全替代字符串）+ `SubtitleLine`（`texts` **多句列表**，一条字幕=一段可分多句的原子播放）。
- **交互提示与字幕共用同一个 TMP**：`InteractionPromptUI` 把提示登记给 `SubtitleSystem.SetPrompt/ClearPrompt`；字幕占用时提示让位（被挤掉），字幕播完恢复；提示显示 `promptDuration`（默认 2.5s）后自动消失（**依赖倒置 DIP**：提示依赖底层字幕显示服务）。

## 五、关键设计模式速查（中英）

- 面向接口 / 开闭原则（Interface / OCP）：`IInteractable` + 设备继承
- 模板方法（Template Method）：`InteractableBase`、`PowerConsumingDevice`
- 观察者（Observer）：资源/状态事件 → HUD/字幕订阅
- 状态模式（State）：`GameStateMachine` + 三状态
- 单例 / 服务定位（Singleton / Service Locator）：`ResourceSystem`/`GameManager`/`DevicePopupUI`/`SubtitleSystem`
- 单一数据源（Single Source of Truth）：`ResourceSystem`
- 数据驱动（Data-driven，SO）：`ResourceConfig`/`TerminalData`/`SubtitleTable`
- MVC：终端打字子系统
- 注册表（Registry）：`SubtitleTable`
- 备忘录式持久化（Memento-style）：`TypingController` 跨会话进度
- 依赖倒置（DIP）：`InteractionPromptUI` → `SubtitleSystem`

## 六、模块进度（对照清单）

- ✅ **已实现**：L0 输入、L1 玩家相机、L2 交互系统、L3 资源 + HUD、L4 状态机三结局骨架、设备（制氧/电击/门）+ 通用弹窗、L6 终端打字全套、L5 字幕系统 + 交互提示共用。
- ⬜ **待办**：L5 剩余（日记 UI、`UIManager`/面板栈、主菜单/设置/暂停）、L7（精神扭曲 `DistortionRendererFeature` + 死亡黑屏演出）、L8（`AudioManager`）、L9（场景搭建与挂载）、L10（SO 数据资产填充：`ResourceConfig`/`TerminalData`/`SubtitleTable` 实例与文案）。

## 七、协作约定（本项目）

- **全程中文**回复。
- 每次代码修改后给**修改汇报表**（文件 / 变更的函数类字段 / 行为影响 / 需手动操作）。
- 涉及架构或设计决策时**显式标注设计模式/原则（中英）**。
- **逐模块推进**：一个模块实现完，提供 Unity 手动配置步骤，等用户在编辑器验证（「验证完成，继续下一步」）后再继续。
- 单例、SO 资产、场景挂载关系多在 Unity Inspector 配置；改动涉及场景/预制体时须在汇报的「需手动操作」中列明。
