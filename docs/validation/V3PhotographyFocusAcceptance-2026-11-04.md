# 摄影、倒片与曝光验收续测

日期：2026-11-04。Unity 2022.3.62f3 / Built-in / 实际 HouseScene、ParkScene Play Mode。对应 v3 实施计划 Task 3；不是全章节原生操作或最终独立包认证。

## 本次修复

`Assets/Resources/ParkPhoto/ManualFocus.shader`：原景深只有无限薄的清晰焦平面。人物胸口与脸部存在深度差，玩法允许的合焦区间内仍有明显模糊。现增加 `max(0.2 m, focusDistance * 0.065)` 清晰范围，超出后平滑增加模糊，与既有摄影合焦容差保持同一尺度。没有自动对焦、修改焦距/FOV、改变拍照通过条件或修改模型。

GPU 取样确认草地胸口约 1.528 m，脸部取样约 1.369 m，对焦设定 1.532 m。对真实渲染画面做开关效果对照，选取距焦平面不超过 0.18 m 的人物区域：修复前平均 RGB 差约 0.0211；修复后约 1.13e-8；故意失焦至 5 m 时仍约 0.0643。回归先失败再通过。临时深度探针材质/Shader/RT 均释放，未保存到场景。

同时完成上一轮草地指针修复的原生验收：伸手时使用可见实际鼠标，接触前禁止误切边缘模式；牵手后恢复取景控制。对应上一轮 `ParkPhotoPrototype.Controls.cs` 与 `V3PhotographyChecks.MeadowPointerContract`，本轮没有再重写这些代码。

## 验证结果与边界

测试使用隔离 EditorPlaySession。站位、曝光设置和部分前置流程由 MCP 测试夹具准备；这些不能算作人工行走、构图或参数选择通过。

| 项目 | 实际结果 | 验证方式 |
|---|---|---|
| 草地牵手 | 点击伸出的手成功，仍为 2/3 张；未因牵手提前曝光 | 原生鼠标点击 + 运行状态读取 |
| 手动调焦 | 实际滚轮 1.532→1.782→1.532 m；人物由清晰变模糊，再恢复清晰 | 原生滚轮、Game 画面、GPU 对照 |
| 牵手后返回 | Esc 放下相机、Esc 暂停、点击 Leave memory；书房记录仍为 2 张、handJoined=true、focus=1.532 | 原生退出 + 读取生产存档 |
| 重进草地 | 经现有 TryStartParkPhotography 返回，重新取景后保留牵手与对焦，不重复消耗照片 | 生产入口调用；站位/取景夹具；场景重建 |
| 草地快门 | 实际左键拍下第 3 张，AwaitingDevelopment；按钮返回书房成功 | 原生快门与 Return to darkroom 按钮 |
| 倒片 | 未按住时滚轮不生效；按住后到 25%；松手后滚轮不推进；取消无活动手势；重载 HouseScene 仍为 25% | 正式 Press/Scroll/Release/Cancel 接口 + 实际场景重载 |
| 倒片取消/完成 | 夹具启动持有后实际 Esc 取消，25% 保留；继续至 100% 才能开盖，未造成曝光损坏 | 混合验证：程序启动、原生 Esc；剩余由控制器推进 |
| 暗袋到印样准备 | 使用同一真实三帧胶卷，经暗袋、封罐、三次倒药/搅动/排液、水洗、晾片、相纸与底片入框、合框到 ContactReady | 正式控制器/拖放接收接口准备，非本轮原生整段拖动 |
| 曝光短按 | 点击开关并松开后仍为 ContactReady，剩余 1.981429 s，灯灭、无 held 残留；未自动完成 | 原生鼠标短按 |
| 曝光持续计时 | 继续按住推进 0.6 s；松开后再 Tick 不推进；取消和 LoadProgress 保留 1.381429 s | 正式 Press/Tick/Release/Cancel 接口 |
| 曝光跨场景恢复 | 重新加载 HouseScene 仍为 ContactReady、剩余 1.381429 s、灯灭、无活动手势、3 张照片 | 实际场景重建及生产读档 |
| 暂停/继续曝光 | 实际 Esc 暂停与继续，剩余时间不变；恢复按住后仅补足剩余量，完成后灯灭 | 原生 Esc；最终持续按住由控制器验证 |
| 相纸后处理 | 显影、停显、定影、水洗、晾照片，各阶段部分处理后保存/加载可继续；最终 Developed/Complete，重载场景仍保留 | 正式拖放/处理接口与生产存档 |
| 三张成片 | 实际点击晾干相纸打开相册，Next 逐张查看 1/2/3，第三张含实际牵手前景，使用真实拍摄图像 | 原生点击；第一、二张为上一轮修复前的实拍，未改写旧照片 |
| Console/Shader | 基线、Play 最终 warning/error 查询均 0；ShaderHasError=false，未清空 Console | Unity MCP |

当前 Windows 桌面工具不支持可靠的跨调用持续鼠标按住及“按住+滚轮”，不使用绕过工具的系统输入注入。因此**整段原生倒片长按、完整曝光长按仍未验收**；控制器结果不能代替这两项手感验收。三处连续自由行走/自主构图也仍未完成，本轮原生快门补齐了第三处。

## 摄影是否符合 v3

依据 v3 第 6–7 页（`output/park-blockout/v10/v3-design-extracted.txt`），旧 `docs/design.html` 快照不作为冲突时的最终标准。

| v3 要求 | 当前情况 | 下一步 |
|---|---|---|
| 定焦镜头；取景中央滚轮调对焦距离 | 已有；本次修复“判定清楚但图像仍糊”；左右边缘是光圈/快门模式，滚轮不会在这些模式中调焦 | 保留；补三处自主站位与调焦手感验收 |
| 四 ISO、完整机械装片、同卷不改 ISO、等效曝光 | 代码与上一轮四 ISO 机械检查覆盖；本轮沿用 ISO800 已装好的胶卷，没有重做四档人工装片 | 保留既有实现，最终包补回归 |
| 公园入口轻偏头、桥边回眸、草地先牵手再拍 | 有独立任务和快门动作路径；牵手阻断已解决；三张真实图像可到暗室查看 | 仍需正式动作表现验收，不能仅凭记录的 moment 字段认定动画达标 |
| 静态对齐后人物动作不撤销对齐，主动改焦点/曝光重新检查 | 当前保存 aligned，并以固定 subjectPoint、相机位置/方向、焦点/曝光重验；本次改焦点未清除牵手进度 | 保留 |
| 参考图可指导实际站位与构图 | **未达标**：三张参考仍为旧几何人物，正式角色的体型与轮廓不同 | **P0：重新制作与当前角色一致的参考图**，保持现有 authored anchors；不要只放宽判定 |
| 三张成片不露脸，草地靠光线遮住脸 | **未达标**：当前草地及桥边可见脸部；不能用永久失焦代替遮脸 | **P1：正式构图、背光/头发与动作遮脸验收**，需在资产/定位保护约束下单独实施 |
| 床旁三个空白相框作为摄影入口 | **未达标**：仍为书房相机入口 | **P1：随季节房间入口工作统一处理**；本轮保留布局，未改入口 |
| 左上参考图无任务文字；物理仪表/边缘操作可辨认 | **部分符合**：有分模式边缘操作，但仍有文字任务、控制提示、数值 HUD；仪表特写表现还需验收 | **P1：先完善物理指示再移除文字**，不可先删掉唯一操作说明 |
| 拍照时不回看；洗完才看参考/成片对照 | 本次实测符合：拍满只显示返回入口，印样完成后才能逐张查看 | 保留 |
| 整卷倒片、遮光装轴、显停定逐步排液、水洗、接触印样 | 生产流程和恢复检查通过；本轮所列持续输入仍有原生验收限制 | **P0：人工补测持有+滚轮、连续曝光长按**；随后做最终独立包回归 |

操作提醒：在取景器中央滚轮调焦；光圈或快门边缘界面内滚轮只调当前参数。回中央再滚轮对焦。本次没有把镜头变焦作为对焦替代，也没有改变旧照片文件。

## 证据与数据保护

证据目录：`output/QA/2026-11-04-Photo-Acceptance/`。

- `focus-gpu-before.json` / `focus-gpu-after.json` 与 `focus-gpu-check.cs.txt`：可重放的 GPU 回归；需要草地已取景且正确焦距的 Play 测试夹具。
- `meadow-focus-fixed.png`、`meadow-native-captured.png`、`meadow-developed-gallery.png`：实际 Unity 窗口截图。
- `hand-native.json`、`meadow-saved.json`、`meadow-resumed.json`、`meadow-captured.json`：牵手、恢复与快门结果。
- `rewind-partial.json`、`rewind-scene-restored.json`、`rewind-escape-state.json`、`rewind-finish.json`：倒片验证。
- `exposure-native-short-click.json`、`exposure-partial.json`、`exposure-scene-restored.json`、`exposure-native-paused.json`、`exposure-finish.json`：曝光验证。
- `paper-finish.json`、`final-print-restored.json`：相纸处理与完成后恢复。
- `rewind-escape.json` 记录一次诊断调用使用不存在的 `InputContext.Id` 属性所致的 MCP 临时代码编译失败；随后改用实际 API/字段重查。不是项目脚本编译错误，不删除失败证据。
- 本轮源文件备份 66 项中 65 项未变，仅 ManualFocus.shader 改变。HouseScene、ParkScene 文件与本轮基线字节一致；未动模型、材质资源、定位物体、存档结构。此前指针修复及其他 agent 工作保留。
- 测试结束退出 Play、打开无未保存修改的 HouseScene。原始存档取自上轮开始前 `2026-11-03-V3-Held-Resume/saves-before.zip`，29 文件 SHA256 全部一致；活动目录仍为 29 文件。`save-restore-result.json` 为核对结果。
- 本轮开始时的会话另留 `session-on-resume.zip`，本轮成片/进度留在 `finished-test-session/`；不会用测试进度覆盖原存档。ResetOnEditorPlay 原偏好 true 未改。
- 没有构建独立包、提交或推送。回退只撤销 `turn-diff.patch` 中 Shader 增量，勿整包覆盖其他未提交工作。
