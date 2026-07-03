# NightHeron（夜鹭便便大作战）程序架构说明

> 本文档供程序开发小伙伴对接使用。详细说明项目结构、核心模块、工作流程和关键 API。

---

## 1. 项目概述

**游戏玩法**：玩家在关卡地图上放置锚点（贝塞尔曲线控制点），画出夜鹭的飞行路线。确认后夜鹭沿路径飞行，自动拉便便砸中地面上的目标（行人、摩托车、建筑等）获得分数。

**技术栈**：Unity（纯 2D，无第三方插件），C# 脚本，所有 UI/精灵均为代码动态生成。

---

## 2. 目录结构

```
NightHeron/Assets/
├── Editor/                         # 编辑器工具（不在构建中）
│   ├── SceneBuilder.cs             # 一键生成所有场景（烘焙关卡 + MenuScene）
│   └── EditorSceneAutoBootstrap.cs # 编辑器启动自动配置（Tag、BuildSettings、打开场景）
│
├── Resources/                      # 运行时动态加载的资源
│   ├── StartScreen.png             # 开始界面背景图
│   └── MapScreen.png              # 关卡地图界面背景图
│
├── Scenes/                         # 场景文件
│   ├── NightHeronScene.unity       # 【厨房水槽】空的源场景，仅供 SceneBuilder 烘焙用
│   ├── MenuScene.unity             # 菜单场景（空壳，运行时由 GameManager 创建 UI）
│   └── Level1~6.unity              # 6 个关卡场景（由 SceneBuilder 烘焙生成）
│
├── Scripts/                        # 运行时 C# 脚本
│   ├── RuntimeBootstrap.cs         # 运行时入口：自动创建核心管理器
│   ├── GameManager.cs              # 全局游戏管理器（UI 流程 + 锚点库存）
│   ├── LevelManager.cs             # 关卡管理器（关卡状态 + 场景切换 + 进度持久化）
│   ├── LevelBuilder.cs             # 关卡构建器（生成关卡内容 + 烘焙恢复）
│   ├── AnchorEditor.cs             # 锚点编辑器（贝塞尔曲线路径设计）
│   ├── PlayerBird.cs               # 夜鹭玩家（沿曲线飞行 + 自动拉便便）
│   ├── Poop.cs                     # 便便行为（渐变缩小消失 + 碰撞检测）
│   ├── Target.cs                   # 可被砸中的目标（行人/建筑/车辆/路牌）
│   ├── CameraFollow.cs             # 摄像机跟随玩家
│   └── AIChatManager.cs            # AI 聊天助手（OpenAI 兼容接口）
│
└── Sprites/
    └── NightHeron.png              # 夜鹭鸟精灵图（运行时加载）
```

---

## 3. 核心模块详解

### 3.1 RuntimeBootstrap — 运行时入口

```csharp
// RuntimeInitializeLoadMethod 在场景加载后自动执行
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
static void AutoBuildGameFlow()
```

**职责**：
- 自动创建 `LevelManager` GameObject（单例，DontDestroyOnLoad）
- 自动创建 `GameManager` GameObject（单例，DontDestroyOnLoad）
- 在菜单场景自动创建纯黑占位相机

**注意**：这两个 Manager 都不需要放在任何场景中，完全由代码动态创建。

---

### 3.2 GameManager — 全局游戏管理器

**核心职责**：
- **UI 流程控制**：开始界面 → 关卡地图 → 关卡场景
- **锚点库存管理**：最多 4 个锚点，放置消耗、撤销归还
- **跨场景持久化**：DontDestroyOnLoad，跟随整个游戏生命周期
- **输入处理**：`Update()` 中处理按键（任意键继续、1~6 选关、ESC 返回菜单）

**关键 API**：

| 方法/属性 | 说明 |
|---|---|
| `GameManager.Instance` | 全局单例访问 |
| `maxAnchors` (int) | 最大锚点数量（默认 4） |
| `RemainingAnchors` | 剩余可用锚点数 |
| `UsedAnchors` | 已使用锚点数 |
| `CanPlaceAnchor()` | 是否还能放置锚点 |
| `TryUseAnchor()` | 消耗 1 个锚点，返回是否成功 |
| `ReturnAnchor()` | 归还 1 个锚点 |
| `ResetAnchorStock()` | 重置锚点库存（进关卡时调用） |
| `CurrentLevel` | 当前关卡索引（0=菜单） |
| `RequestEnterLevel(int)` | 进入指定关卡（1~6） |
| `ReturnToMenu()` | 返回菜单 |
| `TryShowMenuUI()` | 强制显示开始界面 |

**UI 流程**：
```
MenuScene 加载
  → GameManager.ShowStartScreen()  显示"按任意键继续"背景
  → 玩家按任意键
  → GameManager.ShowLevelMap()     显示关卡地图 + 6 个透明热区
  → 玩家按 1~6 或点击热区
  → LevelManager.LoadLevelScene() 加载关卡场景
  → ESC → ReturnToMenu()          返回菜单
```

**场景识别**：`scene.name == "MenuScene" || scene.name == "NightHeronScene"` 视为菜单场景。

---

### 3.3 LevelManager — 关卡管理器

**核心职责**：
- **关卡元信息**：6 个 `LevelInfo`（index、sceneName、displayName、unlocked、completed）
- **场景切换**：`LoadLevelScene(int)` / `ReturnToMenu()`
- **进度持久化**：通过 `PlayerPrefs` 位掩码存储（低 6 位=解锁，高 6 位=通关）
- **事件通知**：`OnSceneChanged` 事件

**关键 API**：

| 方法/属性 | 说明 |
|---|---|
| `LevelManager.Instance` | 全局单例 |
| `levels` (LevelInfo[]) | 6 个关卡的信息数组 |
| `CurrentLevelIndex` | 当前关卡索引（0=菜单） |
| `CompletedCount` / `TotalLevels` | 通关数 / 总数 |
| `LoadLevelScene(int)` | 加载指定关卡（1~6），会检查 unlocked 和 Build Settings |
| `ReturnToMenu()` | 加载 MenuScene |
| `CompleteLevel(int)` | 标记通关并解锁下一关 |
| `IsLevelUnlocked(int)` / `IsLevelCompleted(int)` | 查询状态 |
| `UnlockAllLevels()` / `ResetAllProgress()` | 调试方法 |
| `GetLevelInfo(int)` | 获取关卡详情 |

**当前状态**：开发阶段所有关卡默认 `unlocked = true`（`InitLevels()` 中设置）。

---

### 3.4 LevelBuilder — 关卡构建器

**核心职责**：
- **程序化生成关卡内容**：相机、地面、障碍物、目标（行人/摩托车）、起点终点标记、玩家鸟、锚点编辑器、UI
- **烘焙恢复**：识别场景是否已烘焙，恢复按钮监听、UI 引用、PlayerBird/AnchorEditor 引用
- **开始游戏**：锁定编辑模式，鸟沿玩家画的路径飞行并自动拉便便

**关键逻辑**：

```csharp
void Awake() {
    if (!Application.isPlaying) return;  // 编辑器模式跳过
    
    // 菜单场景：不生成任何内容，由 GameManager 控制
    if (sceneName == "NightHeronScene" || sceneName == "MenuScene") return;
    
    // 已烘焙场景：恢复引用和按钮监听
    bool alreadyBaked = Camera.main != null && GameObject.Find("Ground") != null;
    if (alreadyBaked) { /* 恢复绑定 */ return; }
    
    // 未烘焙：动态生成所有内容
    GenerateLevel();
}
```

**障碍物布局**：每关有不同布局（`GetObstaclePositions(levelIndex)`），通过 `levelIndex` 区分。

**地图底图**：尝试加载 `Assets/Sprites/Map.png`，不存在则用纯色填充。

**鸟精灵**：从 `Assets/Sprites/NightHeron.png` 加载，自动水平翻转。

**确认按钮回调重新绑定**：烘焙场景中 `Button.onClick` 的序列化回调可能失效，`Awake()` 中强制重新绑定 `StartGame`。

---

### 3.5 AnchorEditor — 锚点编辑器

**核心职责**：
- 玩家点击空白区域放置锚点（向 GameManager 申请库存）
- 选中锚点后显示蓝色手柄（贝塞尔曲线控制点）
- 拖拽手柄调整曲线形状（镜像对称：handleIn = -handleOut）
- 退格键撤销最后一个锚点（归还库存）
- 生成 Cubic Bezier 曲线路径点

**核心数据结构**：

```csharp
public class AnchorData {
    public Vector3 position;    // 锚点世界坐标
    public Vector3 handleIn;    // 进入手柄（相对锚点）
    public Vector3 handleOut;   // 离开手柄（相对锚点）
}
```

**关键 API**：

| 方法 | 说明 |
|---|---|
| `GetCurvePath()` | 返回当前贝塞尔曲线路径点列表（供 PlayerBird 飞行） |
| `isEditing` (bool) | 是否编辑模式（false=锁定，游戏开始后） |

**曲线算法**：每段使用 Cubic Bezier（`p0→p1`，控制点 `c0,c1`），默认手柄长度为段长的 30%。

**UI 绑定**：需要 `anchorCountText` (Text) 和 `anchorStockImages` (List\<Image\>)，烘焙场景中由 LevelBuilder 恢复引用。

---

### 3.6 PlayerBird — 夜鹭玩家

**核心职责**：
- 沿贝塞尔曲线路径点逐点移动
- 自动连续拉便便（`autoPoop = true` 时）
- 根据移动方向翻转精灵（`sr.flipX`）

**关键字段**：

| 字段 | 默认值 | 说明 |
|---|---|---|
| `flySpeed` | 5f | 飞行速度 |
| `poopSpeed` | 10f | 便便速度（当前未使用，便便直接出生在当前位置） |
| `autoPoop` | true | 是否自动拉便便 |
| `autoPoopInterval` | 0.3f | 自动拉便便间隔（秒） |
| `poopCooldown` | 0.25f | 手动拉便便冷却（秒） |
| `poopPoint` | Transform | 便便出生点（鸟下方的子物体） |

**关键方法**：

| 方法 | 说明 |
|---|---|
| `SetPath(List<Vector3>)` | 设置飞行路径 |
| `DropPoop(bool bypassCooldown)` | 拉一坨便便 |
| `IsFinished()` | 是否已飞完路径 |

**注意**：在编辑阶段 `enabled = false`（不飞行），点击"开始拉屎！"后 `enabled = true`。

---

### 3.7 Poop — 便便

- 随时间渐变缩小 + 透明度降低（`lifetime = 2.5s`）
- 碰撞到 Target/Building 触发 `GetPooped()` 并自毁
- `alreadyHit` 标记防止重复碰撞

---

### 3.8 Target — 可被砸中的目标

```csharp
public enum TargetType { Person, Building, Car, Sign }
```

- `GetPooped()`：视觉反馈（颜色变棕 + 放大）+ 溅射粒子效果
- 不同目标类型有不同分数（Person=100, Building=50, Car=150, Sign=75）
- **注意**：当前分数只计算但未显示在 UI 上

---

### 3.9 CameraFollow — 摄像机跟随

- 在 `LateUpdate` 中平滑跟随 Player（`Lerp`）
- 有 `clampMin/Max` 范围限制

---

### 3.10 AIChatManager — AI 聊天助手（独立功能）

- 支持 OpenAI 兼容 API（ChatGPT / DeepSeek / Qwen 等）
- 在 Inspector 填入 `apiUrl`、`apiKey`、`modelName` 即可使用
- 需要绑定 UI 引用：`inputField`、`chatText`、`scrollRect`、`sendButton`
- **当前未集成到主流程中**，需要手动挂载到场景

---

## 4. 编辑器工具

### 4.1 SceneBuilder — 场景烘焙工具

**菜单入口**：`Tools → Build All Scenes (Night Heron)`

**工作流程**：
1. 创建空的 `MenuScene.unity`
2. 打开 `NightHeronScene.unity`（厨房水槽）
3. 动态创建 `LevelBuilder`，调用 `GenerateLevel()` 生成所有关卡内容
4. 保存为 `Level1.unity`（内容已烘焙在场景中）
5. 复制 `Level1.unity` → `Level2~6.unity`，设置不同 `levelIndex`
6. 还原 `NightHeronScene.unity` 为空
7. 更新 `Build Settings`（MenuScene 在索引 0，Level1~6 在 1~6）

**什么时候需要运行**：
- 第一次拉取项目
- 修改了 `LevelBuilder.GenerateLevel()` 的内容生成逻辑
- 场景文件丢失或损坏

### 4.2 EditorSceneAutoBootstrap — 编辑器自动配置

`[InitializeOnLoad]` 在编辑器启动时自动执行：
- 确保 TagManager 中有 `Player`、`Target`、`Building`、`Ground`、`Poop` 五个 Tag
- 自动打开 `MenuScene`（如果存在）
- 确保 Build Settings 包含所有场景

---

## 5. 场景文件说明

| 场景 | 用途 | 运行时内容 |
|---|---|---|
| `NightHeronScene.unity` | 厨房水槽（空的源场景） | 空，仅供编辑器烘焙用 |
| `MenuScene.unity` | 菜单场景 | 空壳，运行时由 GameManager 动态生成 UI |
| `Level1~6.unity` | 6 个关卡 | 已烘焙：相机、地面、障碍物、目标、玩家鸟、锚点编辑器、UI |

**重要**：`NightHeronScene.unity` 必须保持为空，不要手动编辑它。关卡修改应该直接编辑 `Level1~6.unity`。

---

## 6. 游戏流程

```
[编辑器启动]
  → EditorSceneAutoBootstrap 配置 Tag + Build Settings
  → 打开 MenuScene

[按 Play]
  → RuntimeBootstrap 创建 GameManager + LevelManager
  → GameManager 显示开始界面（"按任意键继续"）
  → 玩家按任意键
  → GameManager 显示关卡地图（6 个热区 + 背景图）
  → 玩家按 1~6 或点击热区
  → LevelManager.LoadLevelScene(N)
  → 场景加载完成
  → LevelBuilder.Awake() 检测到已烘焙场景，恢复引用
  → 玩家在画面上放置锚点，拖拽手柄调整路线
  → 点击"开始拉屎！"
  → LevelBuilder.StartGame() 锁定编辑
  → PlayerBird 沿路径飞行 + 自动拉便便
  → 便便砸中 Target 得分
  → ESC 返回菜单
```

---

## 7. 常见问题 & 注意事项

### 7.1 场景显示为空 / 看不到开始界面

- 确保运行过 `Tools → Build All Scenes (Night Heron)`
- 确保 `MenuScene` 在 Build Settings 索引 0
- 确保 `NightHeronScene.unity` 是空场景（无残留组件）

### 7.2 关卡场景加载失败

- 检查 Console 日志：`[LevelManager] 场景 LevelN 无法加载`
- 确保 Build Settings 包含 Level1~6
- 确保场景文件存在于 `Assets/Scenes/LevelN.unity`

### 7.3 烘焙后确认按钮无响应

- 这是已知问题，LevelBuilder.Awake() 中有修复代码强制重新绑定
- 如果仍然无效，检查 ConfirmButton 是否存在、EventSystem 是否存在

### 7.4 锚点编辑器不显示 / 手柄不响应

- AnchorEditor 依赖 GameManager.Instance 获取锚点库存
- 需要 Camera.main 存在
- 手柄只在选中锚点后显示

### 7.5 修改关卡内容

直接编辑对应的 `LevelN.unity` 场景即可，不需要重新烘焙。
如果需要修改所有关卡的生成逻辑，修改 `LevelBuilder.GenerateLevel()` 后重新运行 `Tools → Build All Scenes`。

---

## 8. 对接待办

以下功能当前未实现或需要完善，可以作为后续开发方向：

- [ ] **分数系统**：当前 `Target.GetScore()` 计算分数但未显示，需要在 UI 上加分数面板
- [ ] **通关判定**：目前无通关条件判断（飞到终点后 nothing happens）
- [ ] **AIChatManager 集成**：需要挂载到场景并绑定 UI 引用
- [ ] **关卡差异**：Level2~6 当前内容完全相同（复制自 Level1），需要独立编辑
- [ ] **锚点库存 UI**：确认 4 个黑色方块在烘焙场景中是否正确显示
- [ ] **音效**：目前完全没有音频

---

*最后更新：2026-07-03*
