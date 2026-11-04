# v3 摄影与暗室：实际输入打磨记录

日期：2026-11-03。对应 `docs/superpowers/plans/2026-11-03-v3-alignment.md` Task 3。

本轮完成三个输入缺陷修复及针对性验收；不是完整摄影章节、五日流程或独立包验收。

## 修改与原因

| 文件 | 本轮修改 |
|---|---|
| `Assets/Scripts/Park/ParkPhotoPrototype.Controls.cs` | Body/Meter 使用实际屏幕鼠标位置；Body 复用 PointerGestureBuffer，保留按下和松开各自的射线，避免快速拖动在下一次 Update 时把终点当起点；取消时清空待处理手势。 |
| `Assets/Scripts/Park/ParkPhotoPrototype.Input.cs` | Body/Meter 使用可见、解锁的指针。Exploration/Viewfinder 保留原锁定视角与屏幕边缘切换。 |
| `Assets/Scripts/Park/ParkPhotoPrototype.cs` | OnGUI 捕获 Body 左键按下/释放事件；仍由现有输入上下文和机械操作方法执行。 |
| `Assets/Scripts/Chapters/Photography/PhotoWorld3D.DarkroomAssets.cs` | 三种药瓶的原有 BoxCollider 贴合正式模型 Body_Mesh，消除前方旧白模命中盒拦截后方可见瓶肩的问题。保留交互对象、位置与接收区。 |
| `Assets/Editor/Validation/V3PhotographyChecks.cs` | 新增精确指针、快速拖动/取消、药瓶肩部射线回归检查。 |

根因均先复现：指针合同检查 4 项失败；缺少事件射线缓冲；后两瓶命中前一瓶。修复后分别通过。未修改录音控制、章节规则、存档格式、资产、相机定位物体或场景布局。

## Unity 验证

环境：Unity 2022.3.62f3，Built-in，HouseScene/ParkScene Play Mode。使用独立 EditorPlaySession；进入模式、站位和部分前置状态由测试接口准备。以下“鼠标”指 Windows 原生输入，不包含这些前置设置。

| 范围 | 结果及证据边界 |
|---|---|
| 装片 | 鼠标拖倒片钮开盖 → ISO400 胶卷入仓 → 片头入轴 → 过片杆收紧 → 合盖 → 首帧过片通过。实际保存 step 7、ISO400、0 张、Ready。 |
| 四档 ISO / 状态恢复 | 程序检查 37 项加载/恢复合同、40 项四 ISO 机械命中流程、6 项指针合同通过（`regressions.json`，83 PASS）；额外快速手势检查 5 PASS。实际 Park 场景重载保留准备完毕状态。 |
| 三处拍摄 | 测试接口站位、配置并调用正式游戏相机拍摄，三帧均成功；不是人工构图、调焦和人物动作的完整验收。 |
| 倒片、取卷、暗袋 | 通过正式控制器的按下/滚轮/释放与接收区准备，包含部分暗袋操作取消、保存恢复；本轮未完成原生“按住同时滚轮”和携物行走验收。 |
| 底片冲洗 | 鼠标合罐、分别倒入显影/停显/定影液、晃罐、拖罐到排液盆、点击水洗、开盖取底片通过。药瓶修复后后两瓶不再选到前瓶；固定后的射线检查 3 PASS。 |
| 晾片与入框 | 鼠标底片到夹子，相纸入框、干底片入框、合压板通过。 |
| 曝光与中断 | 原生按下/拖动/松开可开始和中止曝光；松手立即灭灯。Esc 暂停后调用现有保存/恢复，剩余 1.975987 秒保持、灯熄灭。剩余曝光使用正式控制器推进完成；不能记为原生全程长按通过。 |
| 相纸处理 | 鼠标开框、取纸到显影盘 → 停显盘 → 定影盘 → 水洗盘 → 点击水洗阀 → 拖到照片夹连续通过；按实际运行时间完成处理，最终 Complete / Developed，无活动手势。 |
| 恢复 | 待排液阶段保存/恢复通过；停显处理中实际停止 Play、重新加载保存的隔离会话后继续通过；最终完成后实际重载 HouseScene 仍是 Complete / Developed。曝光恢复是同一实例调用生产保存/加载，不冒充重启验收。 |
| Console / 编译 | 基线与最终 warning/error 查询均为 0，最终不在编译中。未清空 Console。 |

三处拍摄辅助检查存在时间敏感性：同步调用同伴 Update 的夹具在桥与草地初次返回 `bridgeReady=false`；等待实际游戏帧稳定后，同一正式捕获路径分别从 1→2、2→3 张成功。保留原失败记录，不能把同步夹具本身标为全绿。

## 证据与保护

证据目录：`output/QA/2026-11-03-V3-Native-Iteration/`。

- 截图：`native-loading-ready.png`、`native-drained.png`、`native-print-complete.png`。
- 原生操作结果：`native-*.json`、`paper-native-*.json`、`print-native-complete.json`。
- 恢复：`drain-recovery.json`、`darkroom-state-restored.json`、`exposure-recovery.json`、`completed-restored.json`。
- 失败与修复：`pointer-red.json` / `pointer-green.json`，`buffer-red.json` / `buffer-green.json`，`bottle-red.json` / `bottle-green-persistent.json`。
- 比较的是本轮开始的 `source-before.zip`，不是包含其他工作的 git HEAD。`turn-diff-summary.json` 确认仅上述五个代码文件变化，另外 101 个备份文件不变，HouseScene/ParkScene 字节一致。
- 全局 `git diff --check` 仍报告原有 HouseScene Unity 序列化空值行尾空格；场景与本轮基线一致，未为消除该报告重写场景。
- 测试结束退出 Play，HouseScene 为 clean；原 29 个持久化文件恢复，SHA256 全部一致，活动目录文件数仍为 29。测试会话单独归档到 `finished-test-session/`。重置 Editor Play 存档的原偏好仍为 true。
- 独立只读代码审查无可行动问题；审查不替代上述运行验收。没有构建、提交或推送。

## 尚未完成与后续优先级

1. 原生持续按住＋滚轮倒片、暗袋转轴以及携物行走；现有桌面自动化 API 不提供跨调用保持按键/鼠标状态，需人工或支持持续输入的受控验收接口。
2. 公园三处全程人工站位、调焦、动作差异和角色遮脸验收；本轮未修改人物资产。
3. 物品辨认性：水洗阀与晾晒架轮廓仍接近，照片夹需清晰视线；不应只靠 tooltip 才能区分。部分视角下大罐/开盖仍会遮住后方物品，需要后续在保留定位约束下处理视线与表现。
4. 曝光全时长原生长按、完整章节与最终独立包验收。当前 Task 3 的复合验收项保持未勾选。

回退：依据 `turn-diff.patch` 撤销本轮五个文件的增量；不要覆盖同文件内已有修改。`source-before.zip` 是逐文件参考，不能整包覆盖当前工作区。存档无需迁移，已恢复原件。
