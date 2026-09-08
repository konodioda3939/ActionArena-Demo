# Action Arena Demo — 进度交接（给接手 AI）

> **读本文件即可理解项目全貌与当前进度。** 用户陈禹帆正在 Unity 编辑器里**组装**一个第三人称近战动作 Demo（求职 P0 作品）。所有 `.cs` 代码已由上一轮 AI 写好落盘，当前工作重点是 Unity 组装 + 调试。请从文末「下一步」继续，并务必先看「已踩过的坑」。

---

## 1. 项目定位

- **是什么**：第三人称近战动作·波次生存 Demo（Unity）。
- **为什么**：补足简历「游戏本体」空缺，冲 2026 秋招「游戏客户端开发」岗。
- **差异化**：全部美术资产由自研 AIGC 管线产出 + 游戏运行时集成 AI 生成（IGC）。
- **用户**：陈禹帆，福州大学软工 2027 届；AIGC 工具链是强项，缺游戏本体作品。GitHub: konodioda393939。中文交流，习惯一步步跟着文档做、遇到问题报现象/Console 报错。

## 2. 技术栈

- Unity **2022.3.62 LTS**，**Built-in 渲染管线**（未用 URP）
- **Cinemachine 2.10.7**（FreeLook 第三人称镜头）
- **Input System 1.14.2**（Active Input Handling = **Both**）
- **AI Navigation 1.1.7**（NavMesh）
- 命名空间统一 `ActionArena`
- 架构：`EventBus` + `IGameEvent` 结构体（解耦）；`IDamageable` 接口；`CharacterController` 驱动移动（非 Rigidbody）

## 3. 代码状态：✅ 全部写好落盘

脚本目录：`D:\Unity\My_AIGC_Testproject\Assets\Game\Scripts\`

```
Core/    SingletonBehaviour, EventBus, Events, GameManager（状态机单例）
Input/   GameInput（新 Input System，代码内绑键，零配置）
Player/  PlayerController, PlayerAnimator, PlayerCombat, PlayerHealth
Combat/  DamageInfo, IDamageable, HitBox, HurtBox, HitStop, CameraShake
Pooling/ ObjectPool, PooledLifetime
Enemy/   EnemyController（状态机+NavMesh）, EnemyHealth, EnemySpawner
Waves/   WaveConfig(ScriptableObject), WaveManager
UI/      HUDController, FloatingText, FloatingTextSpawner
```

配套文档（同目录）：

- `SETUP.md` — 装包 / 补 Mixamo 动画 / 搭场景 烘焙 NavMesh
- `ASSEMBLY.md` — Animator 参数表 + Player/Enemy Prefab 挂载清单 + 场景组装 + Play 检查清单 + 常见问题
- 完整计划：`C:\Users\15703\.claude\plans\snoopy-snacking-crane.md`

## 4. 当前进度

### 阶段 ① Player —— ✅ 已验证通过

Play 后角色能 WASD 移动（相机相对）、左键挥拳播 Attack1、不再自杀/入土/滑步。

**已跑通的 Player 配置**（供参照）：

- 根物体 Tag=`Player`
- Animator = `PlayerAnim.controller`，**Apply Root Motion 关闭**
- CharacterController：Center=(0,0.9,0), Height=1.8, Radius=0.3
- 组件：GameInput / PlayerAnimator(`_maxMoveSpeed`=6) / PlayerController(`_moveSpeed`=6) / PlayerHealth / PlayerCombat / HurtBox
- HurtBox 用单独 **CapsuleCollider(isTrigger)**（不能复用 CharacterController）
- 右手 HitPoint 子物体：BoxCollider(isTrigger) + HitBox
- 所有 Mixamo 动画 Rig=**Humanoid**；Idle/Walk/Run 勾 **Loop Time**
- Cinemachine FreeLook：Follow=Player，LookAt=Player 或 `CameraTarget`(Player 下空物体 Y=1.4)
- 场景有 **GameManager** 物体（`_startInPlaying`=true）

### 阶段 ② 打击反馈 —— ✅ 已挂好（待联机验证）

- `HitStop` + `CameraShake` 挂到 **Main Camera**（订阅 `HitLandedEvent`）。
- ⚠️ 待验证：Cinemachine FreeLook 控相机时，CameraShake 直接动 Main Camera 的 transform 可能被 Virtual Camera 每帧覆盖（屏震无效）。若 Play 时无震感 → 改用 **Cinemachine Impulse**（给 FreeLook 加 `CinemachineImpulseListener`，在 CameraShake 里调 `CinemachineImpulseSource.GenerateImpulse`）。

### 阶段 ③ 敌人 —— ✅ 已完成并验证

- **Enemy Animator Controller**：`Assets/Game/Animations/Enemy/EnemyAnim.controller`
  - 参数：`Speed`(Float) / `Attack`(Trigger) / `Dead`(Bool)
  - 状态：Walk（默认，有运动，可循环）→ Attack（Trigger，Motion=`Player_Attack1`，Exit Time≈0.8 回 Walk）→ Die（Any State→Die，条件 `Dead=true`，Motion=`Player_Die`）
- **Enemy Prefab** 已组装：`NavMeshAgent` + `EnemyController` + `EnemyHealth` + `HurtBox` + 右手 `HitBox`（直接挂在 RightHand 骨骼上，与玩家做法一致）。**Apply Root Motion 关闭**。
- 场景 NavMesh 已烘焙。
- 验证通过：NavMesh 追击 Player → 进入范围攻击命中扣血 → 被玩家打死则死亡消失。

### 阶段 ④ 波次 —— ✅ 已完成并验证

- `WaveManager` + `WaveConfig`(SO)（Project 右键 Create→ActionArena→Wave Config）。
- 已建 Wave1 / Wave2（loop 勾选，打完循环回 Wave1）。验证：Wave1(5)、Wave2(8) 正常刷怪，loop 正常。
- `EnemySpawner.SpawnOnRing` 已用 `NavMesh.CalculatePath` 验可达性（见坑表「波次卡死」）。

### 阶段 ④ HUD —— ✅ 已完成并验证（中文显示正常）

- `HUDController.cs` 用 `TMPro.TMP_Text`（4 文本字段 `_scoreText`/`_waveText`/`_enemiesText`/`_healthText`）+ `_healthFill`(`Image`，靠 `fillAmount` 0~1 控血条长度，见 [HUDController.cs:44-45](Game/Scripts/UI/HUDController.cs#L44-L45)）。
- Canvas + 控件已建，`HUDController` 挂在 **Canvas** 上。
- **5 个字段全部已拖引用**（场景序列化确认）：`_scoreText`/`_waveText`/`_enemiesText`/`_healthFill`/`_healthText` 均非空。
- HealthFill 的 Image 已配好：Source Image=`UISprite` / Image Type=`Filled` / Fill Method=`Horizontal` / Fill Origin=`Left` / Fill Amount=1 / 绿色。
- 事件链已核实全通（HUD 三个订阅都有发布方）：`EnemyHealth`→`AddScore`→`ScoreChangedEvent`；`PlayerHealth`/`EnemyHealth`→`HealthChangedEvent`；`WaveManager`→`WaveChangedEvent`。

## 5. ⚠️ 已踩过的坑（务必避免重蹈）

| 现象 | 根因 | 修复 |
|---|---|---|
| 播放某动画时半身入土 | 该动画 Rig=**Generic**（Mixamo 默认可能是） | FBX→Rig→Animation Type=**Humanoid**→Apply，**每个动画逐个查**（Idle/Walk/Run/Attack1/Hit/Die） |
| 移动时滑步 / 停下时卡在一帧 | Walk/Run/Idle 没勾 **Loop Time** | FBX→Animation→勾 **Loop Time**（不是 Loop Pose） |
| Walk/Run 姿势诡异（像同时播） | `PlayerAnimator._maxMoveSpeed` ≠ `PlayerController._moveSpeed` | 两个都设 **6**（Speed 归一化才到 1） |
| 挥拳几次把自己打死（GameOver） | HitBox 没排除攻击者自身 | **已修代码**：HitBox 缓存 `_owner=GetComponentInParent<IDamageable>()`，命中时 `if(target==_owner) return;` |
| 角色完全不动、只播 Idle | 场景无 GameManager / 没勾 `_startInPlaying` | 场景建 GameManager 物体 + 勾 `_startInPlaying`（IsPlaying 才解锁输入） |
| `A Character Controller cannot be a trigger` | HurtBox 把 CharacterController 当 trigger 设 | `HurtBox.EnsureTriggerCollider()` 已跳过 CC，自动补触发 CapsuleCollider |
| 相机不跟随 | 没配 Cinemachine | FreeLook：Follow/LookAt=Player |
| 视角方向和鼠标相反 | FreeLook 轴方向 | Axis Control→对应轴切换 **Invert** |
| 初始视角不在角色背后 | Orbits 参数 | 调 Middle Rig 的 Height(≈2)/Radius(≈3.5)；正后方靠 Horizontal Axis Value |
| Attack 入土（即便 Rig=Humanoid、Root Motion 关） | Attack1 **状态没挂 Motion** | 确认 Animator 里 Attack1 状态的 Motion 字段挂了正确 clip |
| 波次卡死：某波有几个敌人刷在**围墙外**，进不来也打不到，本波永远清不完 | 生成圆环半径(`_spawnRadius`) > 墙内空间；且 NavMesh **烘到了围墙外**（地面 Plane 比墙内区域大）。`SamplePosition` 只验"有蓝色"不验"可达"，墙外有蓝色照样通过 | `EnemySpawner.SpawnOnRing` 已改用 `NavMesh.CalculatePath` 验"**能寻路到玩家(PathComplete)**"才生成；或把关卡可行走地面缩小到墙内、重新 Bake |
| 场景里**手放**的敌人站着不动、不追玩家 | `EnemyController.Initialize(player)` 只在 `EnemySpawner.Spawn()` 里调用，场景放置的敌人 `_player` 为空 → `Update()` 直接 return | `EnemyController.Update` 已加兜底：`_player` 为空时按 Tag 找 `Player`。正式波次生成时 Spawner 仍会显式注入（覆盖即可） |
| HUDController 的文本字段**拖不上引用 / 类型不匹配** | Unity 2022 的 UI 菜单默认是 **TextMeshPro**(`TMP_Text`)，不是旧版 `UnityEngine.UI.Text`；用户在 UI 菜单只能看到 "Text - TextMeshPro" | `HUDController.cs` 已把 4 个文本字段全改成 `TMP_Text` + `using TMPro;`。**新建 UI 文本一律用 "Text - TextMeshPro"**。`_healthFill` 仍是 `Image`（走 `fillAmount`） |
| HealthFill 的 Inspector 里**找不到 Image Type 那一行**（连 Set Native Size 按钮都没有） | `Image` 组件的 `Image Type`（含 Fill Method / Fill Origin / Fill Amount / Set Native Size）在 **Source Image = None 时整行隐藏**，不是滚不到，是根本不画。**空 Source Image 时只有 Source Image/Color/Material/Raycast Target/Raycast Padding/Maskable** | 先给 **Source Image 赋一个 Sprite**：点该行最右 ● → 选自带的 `UISprite` 或 `Background`（白色底图，任何用了 Canvas 的项目都自带）→ Image Type 立刻出现 → 设 **Filled** / **Horizontal** / **Left** / Fill Amount=1。脚本 `fillAmount` 只在 Filled 下有效，非 Filled 血条不动 |
| **点 Canvas 下的 TMP 文本弹窗「TMP Importer / Import TMP Essentials」；游戏运行时 HUD 一个字都不显示** | 项目**从未导入 TMP 核心资源**（默认字体 `LiberationSans SDF`、TMP 着色器等）。HUD 用的是 TextMesh Pro，缺这些资源 TMP 文本组件**无法渲染**——组件挂了、引用也拖了，但「没通电」。点文本时 TMP 检测到首次访问就弹导入窗 | 在弹窗里点 **「Import TMP Essentials」**（**不要**点 Examples & Extras，会塞一堆没用的）。弹窗关了则走菜单 **Window → TextMeshPro → Import TMP Essential Resources**。导完后会生成 `Assets/TextMesh Pro/` 文件夹，**等导入/编译完**再 Play。**新项目只要用了 TMP 文本，第一件事就是导这个，否则所有 TMP 字一律不显示** |
| **中文（分数/波次/敌人等）全显示成 □「口」，Console 报 `character ... was not found in the [LiberationSans SDF] font asset`** | TMP 默认字体 `LiberationSans SDF` **只含拉丁字母，没有中文字形**，碰到中文就回退成 □(□)。不是字体坏了，是默认字体压根没有中文字 | 跑菜单 **Tools ▸ ActionArena ▸ Setup Chinese Font (SimHei)**（见 `Game/Scripts/Editor/HUDLayoutTool.cs`）：复制系统 `simhei.ttf` 进项目 → 生成**动态**TMP 字体资源（`AtlasPopulationMode.Dynamic`，用到任何中文自动补字形）→ 赋给所有 HUD 文本并设为项目默认。只跑一次。手动做法：Window → TextMeshPro → Font Asset Creator，Source Font 选中文字体、Atlas Population Mode=Dynamic、Generate |
| **HUD 元素（文本/血条）全堆在屏幕正中央重叠** | 新建 UI 文本默认锚点=中心、偏移=0、尺寸很小，多个叠一起就在正中成一坨；且默认 CanvasScaler=ConstantPixelSize 换分辨率会乱 | 跑菜单 **Tools ▸ ActionArena ▸ Layout HUD**：把 4 文本排到四角、血条排左下，CanvasScaler 设 1920×1080 ScaleWithScreenSize。非破坏性、可 Ctrl+Z |
| **CS0234: 'GetKeyDown' does not exist in namespace 'ActionArena.Input'**（任何 `Input.xxx` 在 `ActionArena.*` 脚本里报「不存在」） | 项目有命名空间 **`ActionArena.Input`**（放 `GameInput`）。在 `ActionArena.*` 子命名空间里写**裸** `Input.`，C# 会把 `Input` 解析成那个命名空间而非 `UnityEngine.Input`，于是 `GetKeyDown`/`GetAxis` 等全报「不存在于 ActionArena.Input」 | 用**全限定** `UnityEngine.Input.GetKeyDown(...)`。同理，`ActionArena.*` 里用到与子命名空间重名的引擎类型（`Input`/`Camera`/`SceneManagement` 等）都要全限定 |
| **Unity 连不上服务（`/health` 超时 / `NO_HTTP_RESPONSE` / 端口 8000 不监听），报「请确认推理服务已启动」** | 服务进程根本没在跑。`netstat -ano \| findstr :8000` 找不到 LISTENING；`tasklist \| findstr python` 找不到 python 进程。**根因不是代码/显卡/依赖**（torch+CUDA+权重都正常），而是 `start.bat` 的**黑窗口被关了**，或从没真正启动。日志里的 `triton not found / xformers` 是**无害警告**（少几个优化核），不是失败原因 | 双击 `D:\aigc-project\inference_server\start.bat`，**黑窗口保持开着别关**（关了 = 服务停）。看到 `Uvicorn running on http://127.0.0.1:8000` 才算起来。`tasklist \| findstr python` 有进程 / 浏览器开 `http://127.0.0.1:8000/docs` 出页面 = 起来了。别同时跑两个 start.bat（第二个报 address already in use） |
| **多模型切换：texture 模型（`dream-textures/texture-diffusion`）`fast_mode=true` 请求崩 HTTP 500，日志 `RuntimeError: ... size mismatch ... to_k ... (768 vs 1024)`；而且崩过一次后，**后续同模型请求持续崩**（即使重启 LCM 分支也救不回来）** | 两层根因：① texture 是 **SD2.x 架构**（UNet `cross_attention_dim=1024`，配 OpenCLIP ViT-H），而 LCM-LoRA `lcm-lora-sdv1-5` 是 **SD1.5 专用**（768，配 CLIP ViT-L）。把 SD1.5 的 LCM-LoRA 挂到 SD2.x UNet，cross-attention 的 to_k/to_v 权重形状对不上 → 形状不匹配崩。②（更阴）`pipe.load_lora_weights()` 崩在半路（`set_peft_model_state_dict` 时），但 **peft 已经把空 LoRA adapter 结构建到 UNet 上了**，而应用层 `_lcm_active` 没置 True → 下次再 load 就和这些残留 adapter 冲突，持续崩（peft 部分适配器污染） | (1) 注册表每条加 `lcm_compatible` 标志，texture=false；(2) `ensure_lcm_mode(active)` 改成**返回 bool 且永不抛**：load 前先 `pipe.unload_lora_weights()` 清掉上次失败残留的空 adapter，load 包 try/except——失败则 unload + 恢复 `DPMScheduler` + `_lcm_active=False` + 返回 False；(3) `/generate` fast_mode 分支按返回值分支：True→LCM 6 步，False→标准 DPM 20 步降级（不崩请求）。**判断 SD1.5 vs SD2.x**：读 UNet config 的 `cross_attention_dim`，768=SD1.5，1024=SD2.x |

## 6. 下一步

### 阶段 ④ 终：MVP Checklist 状态

> HUD 收尾 3 步 + TMP Essentials + 中文字体 + 布局 全部完成。运行时验证状态：

- [x] WASD 移动（相机相对）— 阶段①
- [x] 左键攻击挥拳 — 阶段①
- [x] 敌人受击 / 死亡消失 — 阶段③
- [ ] 打击反馈：屏震 + HitStop（命中时）— ⚠️ 待确认（Cinemachine 控相机，CameraShake 动 transform 可能被覆盖，需改 Impulse）
- [x] 敌人攻击玩家扣血 — 阶段③
- [x] 波次推进 Wave1→Wave2→loop — 阶段④
- [x] HUD 文本中文显示 + 布局 — 本轮已修（SimHei 动态字体 + Layout HUD 工具）
- [ ] 玩家血量 0 → GameOver — 待确认

### 🟡 打磨（暂缓，用户要求以后再来）

- **HUD 文字改深色 / 加描边**：画面偏白，白字看不清。选中 4 个文本一起改 TMP `Color`（Inspector→TextMeshPro 组件→Color），或做 Outline 工具。
- **画面偏白**：调弱 Directional Light 强度 / 换暗天空盒。
- 待确认：屏震、GameOver（见上表未勾项）。

### 阶段 ⑤：AIGC 运行时集成（当前进行中）—— 简历差异化卖点

见第 7 节复用点。`RuntimeAIGCClient`（HTTP 调推理服务 `/generate`，fast_mode LCM）+ `AIGCPromptPanel`（运行时输入 UI）+ `AIGCSkinApplier`（生成贴图替换玩家 `_MainTex`）。

**服务端：✅ 已验证跑通（2026-08-05）**——`/health` 返回 `ready`，`/generate`(fast_mode) 4.2 秒出 512×512 PNG。
**Unity 端：⏳ 待联机验证**——关掉 `_offlineMode` 后实跑一次生成+换皮确认。

启动服务：双击 `D:\aigc-project\inference_server\start.bat`，**黑窗口保持开着**，看到 `Uvicorn running on http://127.0.0.1:8000` 才算起来。

#### 阶段 ⑤.5：多模型切换（anime / realistic / texture）—— ✅ 服务端已验证

**为什么**：原 anime 模型（Counterfeit + 原神 LoRA）天生画二次元少女立绘，把整张人物立绘当 `mainTexture` 贴到 3D 角色上是「贴纸」，脸/身体对不上 UV（用户反馈：即便 prompt 只要黑白纹路，出的仍是穿黑白条纹衣的二次元少女）。加纹理模型 `dream-textures/texture-diffusion` 生成平铺纹路（鳞片/迷彩），**无脸无身体结构**，平铺到角色身上不违和，正好解决最初需求。

**服务端（`model_loader.py` + `main.py`）—— 已写好并复测通过**：
- `MODEL_REGISTRY` 注册 3 个模型（key / label / base_id / lcm_compatible / prompt_suffix）；`get_pipeline_for_model(key)` 懒加载换装（`_swap_lock` 防并发竞态，换装时丢旧管线 + 清 `_shared_components`/ControlNet 缓存 + 重置 `_lcm_active`）。
- `/generate` 加 `model` 字段（默认 `anime`，不传 = 原行为）；新增 `GET /models` 列模型；`/health` label 动态反映当前活动模型。
- texture 模型自动拼 prompt_suffix：`seamless tileable texture, flat, game asset, no human, no face`。

| key | label | HF base_id | 架构 | lcm_compatible | 首用下载 |
|---|---|---|---|---|---|
| `anime` | 二次元（原神风） | `gsdf/Counterfeit-V2.5` | SD1.5 | ✅ | 否（已缓存） |
| `realistic` | 写实风 | `SG161222/Realistic_Vision_V5.1_no_inpaint` | SD1.5 | ✅ | 是（~2GB） |
| `texture` | 纹理/图案 | `dream-textures/texture-diffusion` | **SD2.x** | ❌ | 是（~4GB fp32） |

**复测结果（2026-08-06）**：
- ✅ **texture fast_mode** → LCM 不兼容，自动降级 DPM 20 步 → 512×512 PNG，200 OK。**视觉确认**是平铺无缝裂纹鳞片纹理（橄榄绿/棕褐色），无人脸/无角色身体——解决了「贴纸」问题。
- ✅ **anime fast_mode** → LCM 6 步路径回归正常，200 OK（`steps=6, cfg=1.5`，改动没破坏 SD1.5 LCM）。
- ⏳ **realistic** 未实测（SD1.5，与 anime 同 LCM 路径，预期可用；首用会下载 ~2GB）。
- ⚠️ 出图偏慢（~5-8 s/step 而非 ~1 s/step）是因为有**残留 python 进程占显存**，非代码问题——演示前用 `tasklist | findstr python` 看看有没有多个，关掉多余的（见文末「交付前清理」）。

**Unity 端：⏳ 待组装**——代码已就绪（`RuntimeAIGCClient.GenerateImage` 已加 `model` 参数；`AIGCPromptPanel` 已加 `_modelDropdown`(TMP_Dropdown) + `_modelKeys` 映射 + `SetupModelDropdown()`）。**用户需在 Inspector 里**：
1. 面板下加一个 **TMP_Dropdown**（Hierarchy 右键 → UI → Dropdown - TextMeshPro），拖到 `AIGCPromptPanel._modelDropdown`。
2. Dropdown 的 Options 配 3 项：`二次元` / `写实风` / `纹理图案`。
3. `AIGCPromptPanel._modelKeys` 数组填 `anime` / `realistic` / `texture`（**顺序必须对齐 Options**）。
4. Play → 按 **G** 开面板 → 选「纹理图案」→ 输入 `dragon scales` → 「生成」→ 「套用」→ 角色身上出现龙鳞平铺（不再是人脸贴纸）。切「二次元」再生成对比。

⚠️ **关键坑**：texture 模型是 **SD2.x**（`cross_attention_dim=1024`），与 SD1.5 专用 LCM-LoRA 架构不兼容——直接叠会 RuntimeError。服务端已用 `lcm_compatible` 标志 + `ensure_lcm_mode` try/except 兜住，详见坑表末行「texture 模型 fast_mode 崩」。

## 7. 第 3 周 AIGC 差异化（复用点，勿重写）

- 推理服务：`D:\aigc-project\inference_server\main.py`（FastAPI :8000，接口 `/generate` `/generate-3d` `/health`）
- `D:\aigc-project\unity_plugin\Assets\Editor\AIGCAssetGenerator\AIGCClient.cs` —— **纯 `UnityEngine`/`UnityEngine.Networking`，无 `UnityEditor` 依赖，Runtime 直接可用**
- 计划：`RuntimeAIGCClient`（搬 HTTP/multipart/解析逻辑）+ `AIGCPromptPanel`（运行时 UI）+ `AIGCSkinApplier`（动态替换 `_MainTex`）

## 8. 工作约定

- 用户一步步跟文档操作，报 **Console 报错 / 现象描述**，AI 诊断后给编辑器步骤或改代码
- 改了 `.cs` 后**等 Unity 编译完**再 Play
- 代码引用用 `路径:行号` 格式；改代码前先 Read 确认当前内容
- 命名空间 `ActionArena`，事件走 `EventBus`，别在逻辑层直接引用 UI/表现层
