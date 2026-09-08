# 组装手册：把脚本挂起来跑通（第 1 周 MVP）

> 前提：先完成 `SETUP.md`（装好 Cinemachine / Input System，补好 Mixamo 动画，搭好 Arena 场景并烘焙 NavMesh）。
> 报错就把 Console 信息贴给 Claude。

---

## 1. Player Animator Controller

打开 `Assets/animation/PlayerAnim.controller`（或新建一个），添加以下 **Parameters**：

| 参数名 | 类型 | 说明 |
|----|----|----|
| `Speed` | Float | 移动速度 0~1（脚本自动写） |
| `Grounded` | Bool | 是否着地 |
| `Attack` | Trigger | 触发攻击 |
| `AttackIndex` | Int | 连击段（第 1 周只用 0） |
| `Hit` | Trigger | 受击 |
| `Dead` | Bool | 死亡（保持） |

**状态与过渡建议**：
- 建 **Locomotion** 状态 → 用 Blend Tree（Speed 参数：0=Idle, 0.5=Walk, 1=Run）
- `Locomotion` ——条件 `Attack`——> `Attack1`
- `Attack1` ——Exit Time 0.85——> `Locomotion`
- `Any State` ——条件 `Hit`——> `Hit` ；`Hit` ——Exit Time 0.8——> `Locomotion`
- `Any State` ——条件 `Dead = true`——> `Die`

> 第 1 周只用 1 段攻击；Attack2/3 第 2 周加。

---

## 2. Enemy Animator Controller

参数：`Speed` (Float)、`Attack` (Trigger)、`Dead` (Bool)

过渡：`Walk`(循环) ——`Attack`——> `Attack` ——Exit Time 0.8——> `Walk`；`Any State` ——`Dead=true`——> `Die`。
（敌人可暂时复用 Player 的 Walking/Shooting 动画。）

---

## 3. Player Prefab 组装

把 Mixamo `X Bot` 拖进场景，根物体 **Tag 设为 `Player`**，然后挂组件并拖引用：

```
Player  (Tag = Player)
 ├─ Animator .................. Controller = PlayerAnim.controller
 ├─ CharacterController ....... Radius 0.3, Height 1.8, Step Offset 0.4
 ├─ GameInput
 ├─ PlayerAnimator ............ _maxMoveSpeed = 6
 ├─ PlayerController .......... _input=GameInput, _animator=PlayerAnimator, _camera=Main Camera
 ├─ PlayerHealth .............. _maxHealth=100, _animator=PlayerAnimator
 ├─ PlayerCombat .............. _input=GameInput, _controller=PlayerController,
 │                              _animator=PlayerAnimator, _hitBox=见下方右手 HitBox
 ├─ CapsuleCollider .......... isTrigger ✅（HurtBox 用，半径比 CC 略大）
 ├─ HurtBox ................... (挂在同一物体，配合上面的 Collider)
 └─ mixamorig:RightHand (右手骨骼，或新建空物体 "HitPoint")
     ├─ BoxCollider ........... isTrigger ✅，Size 约 0.3×0.3×0.3
     └─ HitBox ................ _damage=25, _knockbackForce=6
```

> HurtBox 的 Collider 与 CharacterController 可共存：前者 isTrigger 不参与物理阻挡。
> HitBox 会自动补 Kinematic Rigidbody，无需手动加。

---

## 4. Enemy Prefab 组装

新建敌人（可先复用 `X Bot`），挂：

```
Enemy
 ├─ Animator .................. Controller = EnemyAnim.controller
 ├─ NavMeshAgent .............. Radius 0.3, Speed 3, Stopping Distance 1.4
 ├─ EnemyController ........... _hitBox=右手 HitBox, _animator=子级 Animator, _health=EnemyHealth
 ├─ EnemyHealth ............... _maxHealth=50, _scoreReward=100
 ├─ CapsuleCollider .......... isTrigger ✅（HurtBox 用）
 ├─ HurtBox ...................
 └─ 右手 HitPoint: BoxCollider(trigger) + HitBox(_triggerFeedback 可关，避免敌人打玩家也顿帧过强)
```

把配好的敌人拖成 Prefab，供 WaveConfig 引用。

---

## 5. 场景组装清单（Arena 场景）

新建这些空物体并挂脚本：

| 物体名 | 脚本 | 关键引用 |
|----|----|----|
| `GameManager` | GameManager | _startInPlaying=true（调试直接开局） |
| `Player` | (上一步的 Player Prefab 实例) | — |
| `Main Camera` | CameraShake, HitStop | （自动找 Camera.main） |
| `EnemySpawner` | EnemySpawner | _player=Player（不填会自动按 Tag 找） |
| `WaveManager` | WaveManager | _spawner=EnemySpawner, _waves=[Wave1,Wave2…], _spawnRadius=8 |

**Canvas（HUD）**：
- 新建 Canvas（Screen Space - Overlay）。
- 子物体：`ScoreText` / `WaveText` / `EnemiesText` / `HealthFill`(Image, Type=Filled) / `HealthText`，各加 `Text` 或 `Image` 组件。
- Canvas 上挂 `HUDController`，把上面的控件拖进对应字段。
- （可选）`FloatingTextSpawner`：先做飘字 prefab（空物体 + 子 Text + 挂 `FloatingText` 脚本），新建空物体挂 `ObjectPool`（_prefab=飘字 prefab），再挂 `FloatingTextSpawner`（_pool=该 ObjectPool）。

---

## 6. 创建 WaveConfig

1. Project 窗口 右键 → **Create → ActionArena → Wave Config**，命名 `Wave1`。
2. Inspector 里 Groups → 添加元素：Prefab = 敌人 Prefab，Count = 5，SpawnInterval = 0.6。
3. 再建 `Wave2`（Count 调大 / 加第二种敌人）。
4. 选中场景里的 `WaveManager`，把 Wave1/Wave2 拖进 `_waves` 数组。

---

## 7. 首次运行 Checklist

按 ▶ Play，逐项确认：
- [ ] WASD 移动，角色朝相机方向跑（动画 Speed 随之变化）
- [ ] 鼠标左键挥拳，播放 Attack1 动画
- [ ] 攻击命中追来的敌人 → 敌人扣血 → 死亡消失 → 分数增加
- [ ] 命中时有明显**顿帧 + 屏震 + 飘字**
- [ ] 敌人会追过来并攻击你，你被命中扣血（HUD 血条减少）
- [ ] 清空一波后等 2 秒刷下一波，HUD 波次 +1
- [ ] 血量归零 → 进入 GameOver（Console 打印 `→ GameOver`）

## 8. 常见问题

| 现象 | 原因 / 处理 |
|----|----|
| 攻击打不到敌人 | HitBox Collider 太小或位置不对；检查右手 HitPoint 位置；确认敌人有 HurtBox(trigger Collider) |
| 敌人不动 | 没烘焙 NavMesh；或 NavMeshAgent 的 Area 未通行 |
| 报错找不到 GameInput/InputSystem | Input System 包没装，或未在 PlayerSettings 启用 |
| 血条/分数不更新 | HUD 引用没拖；或 Canvas 没 EventSystem（不影响 Text 显示，但影响交互） |
