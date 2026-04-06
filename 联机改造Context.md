# 联机改造 Context（实施步骤）

## 0. 已确认决策（持续更新）

- 网络方案：`Netcode for GameObjects (NGO)`（已确认）
- 会话模型：`Dedicated Server`（已确认，优先防作弊）
- Dedicated Server 部署方式：`本地进程`（已确认，开发阶段）
- 房间人数上限：`10`（已确认）
- 目标平台：`PC+移动`（已确认，分阶段推进）
- 稳定性阶段目标：`PC 4人` -> `移动端 4人` -> `移动端 10人`（已确认）
- 断线重连：`MVP 预留最小重连`（已确认）
- 经验分配：`全队共享`（已确认）
- 经验球归属规则：`最近玩家拾取后经验球消失，并向全队发放经验`（已确认）
- 升级暂停策略：`仅升级者本地等待`（已确认）
- 失败条件：`全员死亡失败`（已确认）
- 云部署切换时机：`联机核心稳定后`（已确认）

## 1. 项目背景

- 项目类型：Unity 3D 类幸存者（自动攻击 + 波次刷怪 + 升级）
- 当前状态：单机逻辑完整，未接入网络框架
- 目标：在保留现有玩法的前提下，改造为 `2-10` 人 Dedicated Server-Authoritative 联机

## 2. 关键现状（已从代码确认）

- 当前无网络包：`Packages/manifest.json` 未包含 NGO/Mirror
- 单例/全局停时较多：`GameManager`、`UpgradeManager`、`GameOverUI` 使用 `Time.timeScale`
- 玩家目标是单引用：`GameManager.playerTransform`，敌人只追一个玩家
- 刷怪与战斗为本地计算：`EnemySpawner`、`EnemyMovement`、`Weapon*`、`EnemyProjectile`
- 经验与升级为本地事件链：`DropManager -> ExperienceOrb -> Player -> UpgradeManager`
- 已知一致性风险：`Enemy.Die()` 和 `GameManager.HandleScore()` 可能重复加分

## 3. 总体实施策略

- 原则 1：先修“单机也有问题”的一致性，再上联机
- 原则 2：先跑通“进房 + 移动 + HUD”，再做“战斗权威化”
- 原则 3：所有影响数值结果的逻辑统一在 Dedicated Server 计算
- 原则 4：客户端负责输入、表现、UI，不负责最终结算

## 4. 分阶段实施步骤

### Phase A：一致性修复与去停时（优先）

目标：不接网也先保证逻辑闭环正确。

改动文件：
- `Assets/Scripts/Enemy/Enemy.cs`
- `Assets/Scripts/Manager/GameManager.cs`
- `Assets/Scripts/Manager/UpgradeManager.cs`
- `Assets/Scripts/UI/GameOverUI.cs`

执行内容：
- 去掉重复加分路径，只保留单一记分入口
- 把 `Time.timeScale` 暂停改为“逻辑状态暂停”（例如输入锁/UI状态）
- 明确死亡事件与对象回收顺序，避免回收时序导致漏结算/重复结算

完成标准：
- 单机下每个敌人死亡只加分一次
- 升级弹窗与失败界面不再依赖全局停时

### Phase B：联机底座 + 玩家同步

目标：可 Client 接入 Dedicated Server，双端看见彼此并可移动。

改动范围（新增 + 既有）：
- `Packages/manifest.json`（引入 NGO/Transport）
- 新增网络启动与会话脚本（建议目录：`Assets/Scripts/Netcode/`）
- `Assets/Scripts/Player/PlayerController.cs`
- `Assets/Scripts/Player/Player.cs`
- `Assets/Scripts/Camera/CameraFollow.cs`
- `Assets/Scripts/UI/GameHUD.cs`

执行内容（拆分 B1~B6，可独立验收）：
- `B1` 网络底座可启动：完成 `NetworkManager` 启动流程（Dedicated Server/Client）。
- `B2` 网络玩家最小原型：建立 NetworkPlayer 生成/销毁与 Owner 归属校验。
- `B3` 输入所有权接入：`PlayerController` 仅 Owner 读取输入。
- `B4` 位姿同步：先接 `NetworkTransform`，再按观感调基础平滑。
- `B5` 本地相机/HUD 归属：`CameraFollow/GameHUD/Joystick` 仅绑定本地玩家。
- `B6` 会话状态最小同步：同步 GameState/基础会话状态并驱动 UI 切换。

完成标准（模块化）：
- B1 通过：DS 启动 + Client 连接/断开稳定。
- B2 通过：多人入场实体与 Owner 归属正确。
- B3 通过：只有本地玩家可控制自己角色。
- B4 通过：双端位姿同步无明显异常抖动。
- B5 通过：相机与 HUD 不串号。
- B6 通过：会话状态跨端一致可观测。

`B1` 本地启动方式（开发环境）：
- 命令行 Dedicated Server：`-batchmode -nographics -hklServer -ip 127.0.0.1 -port 7777`
- 命令行 Client：`-hklClient -ip 127.0.0.1 -port 7777`
- 编辑器内也可用左上角 `B1 Netcode Bootstrap` 面板手动点击 `Start Dedicated Server / Start Client`。

`B1` 验收日志观察点（建议固定执行）：
- 服务端必看：`[B1] Launch args parsed => mode=DedicatedServer`
- 服务端必看：`[B1] StartServer result => True`
- 客户端必看：`[B1] Launch args parsed => mode=Client`
- 客户端必看：`[B1] StartClient result => True`
- 连接事件：`[B1] Event => OnClientConnectedCallback`
- 断开事件：`[B1] Event => OnClientDisconnectCallback`
- 异常事件：`[B1] Event => OnTransportFailure`（出现即判失败）

### Phase C：战斗与刷怪权威化（核心）

目标：敌人、武器、伤害结果全部以 Dedicated Server 为准。

改动文件：
- `Assets/Scripts/Enemy/Spawner/EnemySpawner.cs`
- `Assets/Scripts/Enemy/Enemy.cs`
- `Assets/Scripts/Enemy/EnemyMovement.cs`
- `Assets/Scripts/Enemy/EnemyContactDamage.cs`
- `Assets/Scripts/Enemy/EnemyShooter.cs`
- `Assets/Scripts/Enemy/EnemyProjectile.cs`
- `Assets/Scripts/Manager/WeaponManager.cs`
- `Assets/Scripts/Weapon/*`

执行内容：
- 刷怪只在 Dedicated Server 跑（波次计时、权重随机、Boss 生成）
- 敌人 AI 和命中判定只在 Dedicated Server 计算
- 客户端只做表现同步（位置、动画、伤害数字展示）

完成标准：
- 双端同一时刻敌人血量/死亡结果一致
- 不出现“我这边怪死了，你那边没死”的分叉

### Phase D：掉落、升级、结算联机化

目标：升级和经验链路跨端一致。

改动文件：
- `Assets/Scripts/Manager/DropManager.cs`
- `Assets/Scripts/Level/ExperienceOrb.cs`
- `Assets/Scripts/Level/MagnetArea.cs`
- `Assets/Scripts/Manager/UpgradeManager.cs`
- `Assets/Scripts/UI/WaveInfoUI.cs`
- `Assets/Scripts/UI/GameOverUI.cs`

执行内容：
- 掉落与拾取由 Dedicated Server 判定
- 升级候选由 Dedicated Server 生成并下发，客户端提交选择
- 失败条件由会话状态统一广播

完成标准：
- 升级候选与结果两端一致
- 经验不会重复领取，结算触发一致

## 5. 权威边界（必须长期保持）

Dedicated Server 负责：
- 刷怪、AI、命中、伤害、掉落、经验、升级随机、波次推进、胜负判定

Client 负责：
- 输入采集、UI交互请求、表现层播放（特效/动画/数字）

## 6. 风险点与规避

- 风险：`Time.timeScale` 影响所有客户端。
  - 规避：改为会话状态 + 输入锁。
- 风险：对象池与网络生成冲突。
  - 规避：网络对象与纯表现对象分池。
- 风险：事件重复订阅导致重复结算。
  - 规避：所有结算入口集中到 Dedicated Server 单点。

## 7. 验收顺序（每阶段都回归）

1. 阶段 1（PC 4人）：先确保 4 人 PC 长局稳定，打通完整战斗闭环。
2. 阶段 2（移动端 4人）：在阶段 1 基础上验证移动端输入/UI/性能与网络稳定性。
3. 阶段 3（移动端 10人）：再扩大到 10 人并做高并发压力验证。
4. 每阶段都执行：单机回归 + 跨端一致性回归 + 网络抖动场景回归。

## 8. 当前建议的下一步（实施中）

- 进入 `B3`：输入所有权接入（仅 Owner 读取输入并驱动移动）。
- 每完成一个 B 模块就做一次短回归并更新清单勾选状态。

## 9. 当前开发进度

- Phase A：已完成（重复加分修复 + 去全局停时 + 死亡结算顺序收敛）。
- Phase B：
  - `B1` 已完成并已验收通过（NGO/Transport 入包 + 运行时 Netcode 启动底座 + 本地 DS/Client 启动入口 + 断开后重连验证通过）。
  - `B2` 已完成并验收通过（自动注册 PlayerPrefab + 自动生成 NetworkPlayer；单/双客户端验证 `owner=1/2` 与 `isOwner` 归属正确；客户端断开后服务端可见 `despawned`）。
  - 下一步：`B3` 输入所有权接入（仅 Owner 可读输入）。
