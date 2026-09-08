# Unity 环境准备清单（第 1 周 MVP）

> 这份清单让你能**和 Claude 写代码并行推进**。Claude 负责所有 `.cs` 脚本，你负责 Unity 编辑器里的操作。
> 每完成一项打勾。全部完成后，配合 Claude 给的「Prefab 挂载清单」即可跑通 Demo。

---

## 1. 安装包（Package Manager）

`Window → Package Manager`，左上角切到 **Unity Registry**，搜索安装：

| 包 | 版本（2022.3 LTS） | 用途 |
|----|----|----|
| **Cinemachine** | 2.9.x | 第三人称镜头 + Impulse 屏震 |
| **Input System** | 1.7.x | 新输入系统（键鼠） |

> ⚠️ 装 Input System 时会弹窗问 "Select Input System backend"，选 **Both**（保持旧 Input 兼容，避免现有脚本报错）。

---

## 2. 补齐 Mixamo 动画（Humanoid）

### 现有（已就绪）
`Assets/animation/` 下：`X Bot@Walking`、`X Bot@Fast Run`、`X Bot@Shooting`

### 需从 Mixamo 补齐（全部免费，FBX For Unity，Rig = Humanoid）

| 用途 | Mixamo 搜索建议 | 命名（导入后改 Clip 名） |
|----|----|----|
| 待机 | `Idle` | `Player_Idle` |
| 攻击 1（轻击） | `Punching Bag` 或 `Right Punch` | `Player_Attack1` |
| 攻击 2（连击） | `Punch` / `Jab` | `Player_Attack2` |
| 攻击 3（重击收尾） | `Kick` 或 `Spinning Punch` | `Player_Attack3` |
| 受击 | `Get Hit` / `React` | `Player_Hit` |
| 死亡 | `Falling Back Death` | `Player_Die` |

**统一放到**：`Assets/Game/Animations/Player/`

**导入设置**（每个 FBX）：
1. 选中 FBX → Inspector → **Rig** → Animation Type = `Humanoid`，点 Apply
2. **Animation** 标签页 → 把 Clip 名改成上表的命名。**Idle / Walk / Run 必须勾 *Loop Time*（循环播放）**，否则动画播一次就停在最后一帧，角色会保持一个姿势「滑步」。攻击/受击/死亡动画保持不勾。
   > 区分：*Loop Time* = 循环播放（必须）；*Loop Pose* = 循环时首尾帧对齐、更平滑（可选，锦上添花）。

> 敌人可暂时复用 `X Bot` + 现有 Walking/Shooting 动画，后续第 2 周再差异化。

---

## 3. 场景搭建（新建 Arena 场景）

`File → New Scene`，存到 `Assets/Game/Scenes/Arena.unity`。

- **地面**：Plane（Scale 视竞技场大小，建议 30×30）
- **围墙/障碍**：Cube 拼几面矮墙（给 NavMesh 挡边）
- **NavMesh**：`Window → AI → Navigation`，给地面和墙勾 *Navigation Static*（Object 标签下），Bake
- **光照**：保留默认 Directional Light，调整角度避免角色全黑
- **相机**：保留 Main Camera；Cinemachine 镜头等 Claude 写完镜头脚本后再加

---

## 4. 接下来

Claude 会陆续产出：
- `Assets/Game/Scripts/...` 全部脚本（已完成 Core）
- **Animator 参数表**（告诉你 Animator 里建哪些 Parameter、状态怎么连）
- **Prefab 挂载清单**（Player/Enemy Prefab 上挂哪些组件、拖什么引用）

你只需照着配 + 调数值，遇到报错把信息贴给 Claude。
