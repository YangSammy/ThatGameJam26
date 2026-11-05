# 卧室加载计时、预加载与人工检查

## 后续计时分析（2026-11-05）

以下为同一组 Editor 日志中的实际数据，不代表独立包性能。之前“未执行 Play”的说明仅指初次实现时的检查范围。

| 阶段 | 总时间 | 主要耗时 |
| --- | ---: | --- |
| Host 初始化 | 1.70 s | 秋季 WritingWorld3D 提前构建约 1.58 s |
| 首次春季 | 5.68 s | 场景加载 3.74 s，绑定 0.95 s，旧计时中的 fade-out 0.80 s |
| 春转夏 | 17.43 s | 加载已预完成；激活 0.203 s，绑定 0.021 s，旧 final-sync 16.45 s |
| 夏转秋 | 14.48 s | 场景加载 14.04 s，其中反序列化 13.54 s |
| 秋转冬 | 20.25 s | 场景加载 19.81 s，其中反序列化 18.09 s |
| 第五天回春季 | 0.57 s | 已缓存场景加载 0.408 s |

春转夏的 final-sync 原先包含 Physics.SyncTransforms 和 yield return null，不能将整段归因于物理。该段出现 448 条资产导入记录、105 次 Asset Pipeline Refresh，后者报告耗时合计约 14.831 s；涉及 RadioTapeWornV1 的贴图、材质以及录音机和源带 Prefab。这证明本次计时被编辑器导入严重干扰，不能据此宣称 Summer 在独立包需要 17 秒。日志未证明由哪个操作者/调用入口触发导入，不修改其他资产制作工作。

夏季原生 Loaded scene 日志约 24.90 s，其中 Thread Wait 约 21.98 s 包含预加载等待激活的驻留时间，也不能当作玩家黑屏时间。夏季后台资源准备约 2.89 s，正式交接剩余激活约 0.203 s。

秋冬区间没有 Asset Pipeline Refresh 记录，反序列化是下一轮真正要处理的加载阶段。冬季场景文本约 34.69 MB（秋季 4.68 MB）；文件大小只是排查线索，不能替代依赖资源、对象数量及内存分析。

### 本轮最小修正

- 原 final-sync 拆成 physics-sync-and-stop 和 ready-frame-wait，后者记录跨越帧数、焦点及帧间隔。
- 完全黑屏时跳过无操作的淡出协程，但保留必要的遮罩显示帧，并单独记录 covered-frame-wait。
- 淡变使用从本次淡变开始计算的实际时间，避免上一个加载长帧的 deltaTime 使淡入瞬间结束。
- 本轮没有运行游戏、改布局、修改存档或覆盖未保存场景；不声称已获得新的优化秒数。

### 下一轮执行优先级

1. 在资源导入停止的条件下复测同一流程，区分 Editor 和 Player；保留日志，不通过禁止全局自动刷新掩盖问题。
2. 将秋季书写视觉的昂贵创建延后到需要时；章节数据和存档恢复保持提前可用，不能简单延迟整个章节对象。
3. 审计秋冬场景依赖、重复网格/材质、大贴图和常驻内存，再把预加载扩展为“当前章节即将完成时准备下一季”。同一时刻最多一个预加载，并沿用激活队列释放机制。
4. 需要更大改造时才考虑常驻共用房间壳体、按季节切换布景和玩法层。必须先确认四季空间差异，不为省加载破坏现有 Hierarchy 编辑能力。
5. 体验上保留简洁真实阶段提示；换季前用现有完成反馈留出预加载窗口，转场期间衔接环境声，在房间和出生点就绪后淡入。主线程阻塞期间 UI 动画也会停，提示不能代替性能优化。

原始证据：output/QA/BedroomLoadingProfile_20261105/timing-before.txt、summer-context.txt；本轮脚本修改前备份在同目录 transition-scripts-before/。

## 已实现

1. 启动、创建各章节、绑定配置、读档、保存、淡出、预加载交接、解绑、卸载、场景加载、道具与镜头绑定、物理同步、淡入及总耗时统一记录为 `[BedroomLoad]` 日志。每阶段记录一次，不逐帧刷屏。
2. 首次进入和黑屏交接期间将 `Application.backgroundLoadingPriority` 临时设为 High，为场景/依赖资源加载提供更高预算；结束、失败或退出后恢复原设置。没有降低画质或删减场景。
3. 春季进入且可操作后，等待 2 秒，再以 Low 优先级预加载夏季。这个延迟不属于黑屏等待。只保留一个待激活场景，预加载期间不会运行夏季的场景激活、交互或章节绑定。
4. 春转夏时，在黑屏内先释放预加载的激活等待，再卸载春季，并复用已加载的夏季场景。这样不会在 `allowSceneActivation=false` 阻挡异步队列时请求卸载而死等。若预加载尚未完成，则等剩余加载正常完成。
5. 开发跳往其他场景时，先释放并丢弃未使用的夏季预加载。停止 Play、应用退出、销毁和编辑器脚本重载均释放等待、恢复加载优先级。
6. 遮罩显示简短英文阶段提示：`Preparing room…`、`Saving progress…`、`Loading bedroom…`、`Preparing objects…`。提示轻微明暗变化，没有伪造百分比。同步初始化阻塞主线程时动画也可能短暂停顿。

布局、模型、定位、玩法条件和存档格式均未修改；没有实际加载测试存档或写入玩家进度。预加载会额外占用一份夏季场景依赖资源的内存；本轮没有更改跨章节数据初始化顺序。

## 文件

- `Assets/Scripts/House/Bedroom/BedroomLoadTrace.cs`：分段耗时日志。
- `Assets/Scripts/Core/WBMain.BedroomPreload.cs`：夏季预加载、异步队列释放、加载优先级恢复。
- `Assets/Scripts/Core/WBMain.BedroomTransition.cs`：交接计时、提示与预加载交接。
- `Assets/Scripts/Core/WBMain.SpringBedroom.cs`：场景读入与绑定计时、复用夏季预加载。
- `Assets/Scripts/Core/WBMain.SharedHouse.cs`：启动各阶段计时。
- `Assets/Scripts/Core/WBMain.cs`、`WBMain.Progress.cs`：销毁和退出清理。
- `Assets/Scripts/House/Bedroom/BedroomTransitionOverlay.cs`：简洁加载提示。

## 检查结果与边界

使用 Unity 2022.3.62f3 自带 Roslyn 和项目 Bee 引用参数完成三项编译检查：运行程序集的 Editor 条件、去除 UNITY_EDITOR 的条件、Editor 工具程序集，退出码均为 0。输出在 `output/QA/BedroomLoading_20261105/`；未替换 Unity Library 中的程序集。

静态检查了预加载队列释放顺序、非夏季跳转、重复场景复用、停止与重载清理。未执行 Play、真实鼠标测试、独立包或优化后耗时测量，不能据此声称已经缩短到某个秒数。

原 Editor 日志参考：最近一次春季场景总加载 23191.357 ms（反序列化 22033.910 ms），夏季 15856.082 ms（反序列化 14923.566 ms）。这些是 Unity 自己的场景加载时间，不包含本次新日志记录的全部启动/绑定阶段，也不能直接与整个交接 TOTAL 混为同一个指标。

## 人工测试清单

### 1. 春季冷启动

- 打开 Console，搜索 `BedroomLoad`，关闭 Collapse 以区分不同次加载。
- 正常 Play 进入第一天。确认先看到简洁加载提示，再进入春季卧室；无旧 HouseScene 闪现。
- 记录 `host-startup` 的 TOTAL、`Bedroom_Spring` 的 `scene-load`、`season-bind-and-camera` 和 TOTAL。
- 若仍慢：`scene-load` 慢代表场景/资源读取；`create-chapter-*` 慢代表启动提前创建其他关卡；`season-bind-and-camera` 慢代表录音机初始化、绑定或碰撞采集。父级总计包含子阶段，不能把所有日志毫秒值重复相加。
- 进入后确认移动、开盒、选带仍正常；连续进出工作视角不应再次启动一份预加载。

### 2. 春转夏：预加载完成

- 在春季正常游玩，等待 Console 出现 `summer-preload ... assets ready; activation deferred`。
- 用原流程完成录音；也可按 Editor 的 1 快速完成，再关闭成功弹窗、按原 Esc 流程退出工作视角。
- 预期出现加载提示后进入夏季。记录 `preload-remaining-and-activation`、`unload-old-scene`、`scene-load`、`season-bind-and-camera`、TOTAL。
- 已预加载时 `scene-load` 应接近零，因为复用已加载场景；总交接仍包含激活、春季卸载、暗室绑定和淡变。
- 确认只有一份夏季卧室，照片墙、相机和暗室引用正常，录音完成状态保留。

### 3. 春转夏：立即跳转

- 新一次 Play，在预加载尚未准备好时尽快完成第一天或用 Shift+2 进入夏季测试入口。
- 允许等待剩余加载，但必须最终完成，不能永久黑屏。检查没有重复加载夏季、重复物品 ID 或丢失引用异常。
- 此情形不能用于宣称预加载的完整加速效果，因为没有足够的提前加载时间。

### 4. 预加载中取消或跳往其他季节

- 春季预加载开始后，按 Shift+3 或 Shift+4：应释放夏季等待、丢弃未使用预加载，再进入目标季节，不死等。
- 分别在预加载进行中、预加载准备好后点击 Unity Stop：应正常退出 Play。再次 Play 应能重新加载，不遗留阻塞队列或持续 High/Low 优先级。
- 若在 Editor 重新编译，沿用项目现有热重载保护；本次只负责释放预加载，不能把热重载保护退出 Play 误认为加载失败。

### 5. 公园返回与恢复

- 夏季装片进入公园后，分别使用 Leave memory 和拍满后的 Return to darkroom。
- 返回应进入夏季、相机/胶卷进度仍在，不应因未经过春季而依赖预加载。
- 按现有方式检查已保存的季节进度恢复；Editor 默认每次 Play 重置测试档时，须区分新档与真正读档。

若仍然慢，请保留同一次运行的 `[BedroomLoad]` 完整日志。下一轮按最慢阶段继续优化，不直接把所有关卡改成延迟初始化。
