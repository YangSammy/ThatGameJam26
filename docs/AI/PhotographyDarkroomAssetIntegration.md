# B01–B04 与 C01–C11 游戏接入记录

日期：2026-10-31。Unity 2022.3.62f3，Built-in。此次基于工作区原有 B01–B04 接入继续修改；没有重导模型、改 GUID、改存档格式或修改 ParkScene 布局。

## 结论与验收边界

C01–C11 已实际保存到 HouseScene 原有暗室物品根节点下，使用交付 Prefab 的视觉子层，18 个视觉实例（17 个独立资产，加一个罐内隐藏的卷曲底片替身）、88 个动画引用。新流程没有独立输入监听或计时器。原 PhotoWorld3D、ParkFilmStore、DarkroomPrintState 仍决定玩法与持久化。

Play Mode 已用本次公园实际拍下的三张照片走到印样完成。**全流程拖放是控制器／射线接收区测试，不是完整的原生鼠标验收。** 实际鼠标操作单独列在下面；不能将这份记录表述成所有交互已人工跑通。

## 场景与引用

在 HouseScene 查找 `DarkroomPrintStation`，查看 `DarkroomPrintSceneBinding` 和新增的 `DarkroomAssetVisuals`。原来的 Prop ID、target、placement 保留。模型挂在对应 target 下；原视觉 Renderer 关闭，原逻辑节点不删除。

| 资产 | 原交互 ID / 实际父对象 | 驱动来源与表现 |
|---|---|---|
| B01 / B02 | House 摄影入口、操作相机、SafeInspection 副本；Park 手部仪器 | 复用 PhotographyCameraVisual；后盖、倒片、过片、快门；50 mm 定焦，对焦环／光圈／叶片独立 |
| B03 四档 | `film-100/200/400/800`、`leader`、`chamber`、`spool`、`exposed-film` | 保留原 ISO 和胶卷身份；卡仓命中只在有效阶段启用 |
| B04 | House `meter`；Park Meter 模式 | 复用 PhotographyMeterDisplay / LightMeterVisual 实际读数接口 |
| C01 | `bag` / Dark Bag | BagTurns → Work、FilmInside；取消后根据保存进度恢复，袋体不再整块缩放 |
| C02 | C03 的 ReelSeat 下 | BagTurns → LoadRatchet；内部底片对齐 FilmCoilAnchor，卷曲形变；取片后内部替身隐藏 |
| C03 | `tank` / Film Developing Tank | 主盖仅在取出已处理底片时抬起；加排液只开 LiquidCap；原摇罐／倒罐根节点动作保留 |
| C04 三瓶 | `chemical-0/1/2` | 原 developer / stop / fixer 配置；拖动开瓶盖、原受控倾倒姿态，动画不推进药液结果 |
| C05 | `film-drain-basin`；`water` 保留原命中位置 | 原水洗状态 → WaterOn；有效排液拖放 → DrainOn；龙头视觉对齐现有控制位置 |
| C06 底片 | `print-negative` | 原 printObjectsState 的位置；三份真实图片取反形成底片显示；湿态／平放／悬挂分开 |
| C06 夹子 | `print-negative-clip` | 对齐原接收定位；拖动时开夹、放置后闭夹 |
| C07 | `print-frame`，原 `print-frame-lid` 仍负责点击 | frameClosed → 压板、锁扣；纸面与底片分别对齐交付 PaperAnchor / NegativeAnchor，只补偿视觉，不移动游戏定位 |
| C08 | `print-exposure` | printHeld → 按键和真实聚光灯；松手即灭，曝光计时仍由现有流程负责 |
| C09 | `print-paper` | 原 contactTexture → M_C_PaperContent；Reveal / Wetness 按处理阶段、剩余时间驱动，4:3 输出适配相纸 |
| C10 四盘 | `print-developer/stop/fixer/wash`；`print-tap` | 原定时流程 → 液面涟漪；水洗的进水／排水通道分开驱动，装饰网格无碰撞 |
| C11 | `print-photo-clip` | PaperHangAnchor 对齐原 placement；相纸形变与晾架朝向一致，夹子跟随拖放状态 |

内部底片只作罐内视觉替身，没有第二份物品 ID、数据或输入。未定影阶段主盖始终遮光；正式取片后只显示原 `print-negative` 物品。

## 修改文件

- `Assets/Scenes/HouseScene.unity`：实际 Prefab 实例、视觉适配、动画引用、鼠标控制区标记与曝光灯。
- `Assets/Scripts/Chapters/Photography/DarkroomAssetVisuals.cs`：采样、每实例图片／材质参数、当前形变包围盒、纸与底片命中区。
- `Assets/Scripts/Chapters/Photography/DarkroomVisualIdentity.cs`：对应完整 RestAll 动画的资产名称，不承担物品身份。
- `Assets/Scripts/Chapters/Photography/PhotoWorld3D.DarkroomAssets.cs`：原状态到 C 模型的映射；视觉姿态、均匀比例、纸片层次。
- `Assets/Scripts/Chapters/Photography/PhotoWorld3D.PrintObjects.cs`：绑定新视觉、停止旧 Renderer 参数和旧框盖驱动，适配 4:3 印样。
- `Assets/Scripts/Chapters/Photography/PhotoWorld3D.cs`：新模型存在时停止旧暗袋整体缩放／旧盖位移。
- `Assets/Scripts/Chapters/Photography/PhotoWorld3D.Assets.cs`：只在有效阶段启用 chamber / spool，避免遮挡取卷。
- `Assets/Scripts/Interaction/ObjectDragSession.cs`：拖动前烘焙当前形变并重算包围盒；不再用包含所有可能展开形态的巨大导入包围盒。
- `Assets/Scripts/Park/ParkFilmStore.cs`：内部物理底片预览读取接口与相纸预览比例参数；存档字段、曝光文件和图库解锁条件不变。
- `Assets/Editor/Photography/DarkroomAssetIntegration.cs`：一次性接入工具。已接入场景拒绝重复运行，保护 Inspector 调整。
- `Assets/Editor/Validation/PhotographyDarkroomAssetChecks.cs`：显式 Play Mode 控制器回归，不作为原生鼠标验收。

以上文件的 `.meta` 已由 Unity 生成。B01–B04 上一轮新增文件继续复用，本次没有覆盖模型源文件。初始工作区记录：`QA/BC-Integration/baseline-status.txt`；不要把整个 git diff 都当作本次改动。

## 已修复的问题

1. B03 收回后的片头导入包围盒约 1.2 米，导致胶卷拖到暗袋时碰撞误阻挡。改用当前形变的真实包围盒。
2. 已倒片、开盖后 chamber / spool 仍抢到取卷射线。改为按阶段启用。
3. 原水龙头宽点击区阻挡药瓶移动。点击区与物品运输碰撞分离，接收区没有移动。
4. 新纸张悬挂形变叠加旧白盒的旋转和非均匀缩放，造成方向错误／拉伸。只补偿视觉子层并保持统一展示比例。
5. BakeMesh 不自动给新临时 Mesh 更新 bounds；补上 RecalculateBounds，避免纸张被错误剔除。
6. 相纸与底片贴到框内时，旧偏移会让底片高于透明压板。视觉分别对齐模型提供的纸／底片挂点。
7. 相纸挂起后点击区域仍停在旧白盒中心。命中区现在覆盖当前形变后的可见纸张。
8. 新组件首次测试时 MaterialPropertyBlock 字段初始化触发 Unity 构造期异常，已改为按需初始化并重新进入 Play 复核。

## 实际验证

### 原生鼠标／键盘

- House：点击后盖关闭、点击过片杆 → Ready；点击 Go to park 成功切换。
- Park：三次鼠标快门生成真实图片；草地先点击牵手，再拍摄；显示 3/3 胶卷完成；点击 Return to darkroom 返回 House。
- 为集中验证资产与拍摄链路，测试通过接口把玩家／同伴放到既有摄影定位点、设置匹配参数及虚拟指针；**没有验证从入口徒步到三处并手动完成全部参数调节**。
- 暗室：实际点击封罐、冲洗完成后点击罐盖取片。
- 最终实际点击悬挂相纸打开图库，Esc 返回暗室，完成状态和纸张保留。

### Play Mode 控制器、射线接收区及持久化

- ISO100 装入；拉片头取消保留已装胶卷，重做可穿入收片轴；保存／读取仍是 LoadedOpen 且 threaded=true。
- 倒片 38% 取消和读取保留进度；继续倒完可开盖取卷。
- 当前版本回归通过：取卷→暗袋→装轴40%取消读取→封罐→三种药液各倒入／摇罐／保存读取／排液→水洗→取片→晾片→纸与底片入框→曝光中断读取→续曝光→四托盘→晾照片→Developed。
- 曝光剩余 1.4 秒读取后仍是 1.4 秒；松手时真实灯关闭。
- 相纸各处理中间保存读取；最终完成记录有三张原始曝光，图库仅在完成后打开。
- 重新加载 HouseScene 和隔离测试存档后，Complete、真实印样图片和物品位置恢复。
- 最终 House 运行检查 Missing Script=0，18 个新视觉实例均无模型碰撞体。末次 Console 读取 0 个 warning/error；未调用 Clear Console。中途发现的问题与测试脚本错误没有冒充通过，详见修复记录。
- 涉及 B/C 贴图的导入检查没有发现需要修改的 NormalMap / 数据贴图 sRGB 设置，修复数为 0；没有批量重导无关贴图。

### 布局保护

`scene-pose-comparison.json` 对比了接入前 2477 个原 Transform：没有删除；除原曝光指示物迁到灯头并朝向印样框外，旧 Transform 的位置、旋转、缩放没有变化。灯仍由原 printHeld 控制。ParkScene 本次未编辑。

## 证据

目录：`QA/BC-Integration/`。

- `controller-regression.txt`：当前版本完整暗室控制器回归。
- `loading-recovery.json`、`rewind-recovery.json`、`reel-recovery.json`、`exposure-recovery.json`。
- `real-exposures.json`：本次三次真实拍摄记录，入口／桥／草地顺序。
- `image-bindings.json`：三帧底片各自图片和纸张 Reveal=1 的绑定。
- `final-runtime.json`、`console-before.json`、`console-after.json`、`asset-audit.json`。
- `park-roll-complete.png`、`darkroom-loaded-tank.png`、`reloaded-print.png`、`native-gallery.png`、`native-gallery-closed.png`。
- `saved-roll/`：倒片后测试检查点；`completed-roll/`：完成存档快照。只用于隔离 Editor 测试，不替换玩家正式存档。

## 未验证与剩余风险

- 原生鼠标长拖拽没有完成全流程验收：Windows 自动化 drag 尝试没有让胶卷进入持续按住的游戏手势，后续改用控制器及真实射线接收判定测试。**不能由此断言玩家鼠标拖动手感已经过关。** 人工复测：胶卷入仓、拉片头、取卷进袋、药瓶／罐／底片／相纸的完整拖动。
- 四档 ISO 在原有 B 接入中有模型／控制器检查，但本轮完整真实拍摄与冲洗使用 ISO100；200／400／800 各自新卷的全过程仍需复测。
- 公园画面可见草木黑色块／点状渲染瑕疵，实际照片也会捕获。未证实与本次接入有关，未擅自修改公园环境材质；需要独立排查原环境 Shader／深度效果。
- 复杂中途场景切换、磁盘写入失败、所有角度的薄片穿插、正式 Player 构建尚未验收。
- 现有第一人称手保持使用，但未新增逐根手指的装轴、夹片、旋瓶盖动画；模型部件动作不等于手部精确接触已完成。
- `docs/design.html` 仍存在旧版三档 ISO 和变焦描述；本次按用户明确要求及当前游戏保留四档 ISO、50 mm 定焦，没有改成旧文档快照。

## 回退

修改前的场景副本：`Backups/BC-Integration/HouseScene.before.unity`。确认此后没有新的用户编辑后，可仅用该副本恢复 HouseScene；不要替换 `.meta`。脚本回退仅撤销本次新增适配和上述增量，保留之前 B 接入及其他工作。不要执行整仓 git reset。原 C 模型／材质／动画未更改，可以保留资产目录。运行测试退出时不保存 Play 中的玩家位置。
