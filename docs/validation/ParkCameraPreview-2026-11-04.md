# 公园相机曝光、景深与调焦

2026-11-04；Unity 2022.3.62f3，Built-in，Gamma，D3D11。按用户确认的“取景器实时预览曝光和景深”实现。本轮不制作动态测光、快门拖影、颗粒或新的摄影流程。

## 实现

- 光圈、快门和胶卷 ISO 共同计算曝光偏差；预览和实际照片均应用曝光。在线性颜色中调整，正常曝光保持原图，过曝带柔和高光压缩，不是 UI 黑白蒙层。
- 光圈影响清晰范围和虚焦强度。f/8 保留原判定宽容度，f/2.8 缩窄，f/16 扩大；近处留最低清晰宽度。主体清晰验收与渲染读取同一套范围。
- 滚轮对焦改为非线性距离推进：近处更细、远处更快。现有 focusStep 仍表示约 4 米处的名义调整速度；不改变焦距/FOV，不自动吸附主体。
- 虚焦半径按图像高度统一，减少取景器与落盘分辨率不同带来的效果差异。近景虚焦强度乘 0.65，保留失焦效果同时改善牵手可读性；不是把手单独强制清晰。
- 快门前同步当前参数，避免这一帧轮盘已变而照片沿用旧参数。模式切换立即同步效果，修复放下相机后晚一帧关闭预览的问题。
- 普通行走、机身操作及参考图保持原有自然画面。旧照片字节与旧参考图快照不重算。

## 文件

| 文件 | 用途 |
|---|---|
| `Assets/Scripts/Park/ParkPhotoFocus.cs` | 原有后处理入口；加入共用 `ParkCameraResponse` 数学规则和运行时曝光/光圈参数 |
| `Assets/Resources/ParkPhoto/ManualFocus.shader` | 光圈景深、分辨率一致的模糊、线性曝光及柔和高光 |
| `Assets/Scripts/Park/ParkPhotoPrototype.Controls.cs` | 共用曝光计算；切换模式立即关闭/恢复预览 |
| `Assets/Scripts/Park/ParkPhotoPrototype.cs` | 非线性滚轮、参数同步、快门同步、照片清晰判定 |
| `Assets/Scripts/Park/ParkPhotoPrototype.Bridge.cs` | 任务条件和提示共用新的光圈清晰范围 |
| `Assets/Editor/Validation/ParkCameraPreviewChecks.cs` 与 Unity 生成的 `.meta` | 可重复运行的数学及 GPU 曝光检查 |

无场景、Prefab、定位物体、物品 ID 或存档结构修改。增加的表现参数为非序列化运行状态，从现有摄影设置恢复。没有额外资产安装或渲染管线变更。

## 验证

证据：`output/QA/2026-11-04-Camera-Preview/`。

| 项目 | 结果 |
|---|---|
| 曝光 RED → GREEN | 修改前 .5/1/2 倍曝光 GPU 输出均 0.349；修改后分别 0.2471、0.3490、0.4627；正常曝光保持源亮度 |
| 数学规则 | 18 项 PASS：等效组合、ISO 补偿、曝光方向、四个距离的光圈/判定关系、近细远快、反向滚轮及边界 |
| 真实公园 GPU | 正常/欠曝/过曝平均 RGB 约 0.2661/0.1254/0.4569；f/8 1/1000 与 f/16 1/250 相差约 0.000057 |
| 真实景深图像 | 相同偏焦与等效曝光下 f/16 比 f/8 人物区域边缘细节指标高约 16.5%；已检查 `miss-f8.png` 与 `clear-f16.png` |
| 原生滚轮调焦 | 2.85797 → 2.68482 米，反向恢复；光圈、快门和 FOV=48 不变 |
| 原生光圈/快门滚轮 | f/8 → f/11，焦点和快门不变；再将快门 1/1000 → 1/500，焦点/光圈不变 |
| 原生快门 | 欠曝时点击保持 0 张；补偿曝光后点击成功保存第 1 张 |
| 原生 Esc 与退出 | 放下、暂停、Leave memory 返回书房；再经正式章节接口进入 Park |
| 读档恢复 | 1 张照片、2.6 米、f/16、1/250 全部恢复；无需新存档字段 |
| 草地 | 原生点击伸手完成牵手；正式 Capture 接口拍下第 3 张，检查牵手 PNG |
| 预览/照片一致性 | 最终同帧、同参数、相同摄影遮罩重渲染与保存 PNG 的平均 RGB 绝对误差 6.82e-8；刻意注入旧曝光参数后，快门仍正确同步新参数 |
| 即时效果释放 | 修复前切换探索态仍 active；修复后立即关闭、重进立即开启且参数匹配 |
| 既有输入回归 | InstrumentPointerContract 6 项 PASS |
| 编译/Console | Unity 编译完成；基线及最终 warning/error 查询均 0，未清空 Console |

测试边界：站位、胶卷装好状态及部分操作模式切换由 MCP 夹具准备，实际滚轮、第一张快门、牵手和退出使用原生输入。后两张快门由正式 Capture 接口调用。不是从装片到暗室全流程的纯手工验收，也未构建独立包。

照片比对曾两次因测试夹具假设失败：第一次固定 960×540，但当前 Game 比例实际保存 960×539；第二次满卷已切回探索态，直接重渲染时预览效果已关闭。改为实际尺寸并使用拍摄记录/相同效果状态比较后通过。原失败记录保留，不把它们解释成游戏画面损坏。

## 证据文件

- `red-exposure.json` / `green-checks.json` / `final-compile.json`
- `scene-render-probe.json` 与 `neutral-f8.png`、`under-f16.png`、`over-f4.png`、`equivalent-f16.png`
- `native-wheel-before.json`、`native-wheel-after.json`、`aperture-wheel-after.json`、`shutter-wheel-after.json`
- `native-underexposed.png`、`native-compensated.png`：Unity 实际窗口截图
- `pause-persisted.json`、`recovered-settings.json`
- `captured-meadow.png`：开发验收复制的真实负片，游戏内仍需显影才能看
- `final-pixel-parity.json`、`captured-final.png`、`capture-rerender.png`
- `red-mode-release.json`、`mode-release-green.json`

## 保留与限制

本轮前的 29 个原存档均恢复，SHA256 全匹配，见 `save-restoration.json`。测试数据归档到 `session-one/`、`session-two/`。HouseScene 与 ParkScene 对本轮基线逐字节相同；Unity 最后停在 HouseScene Edit、clean，reset-on-Play 保持原值 true。没有提交或推送。

本轮仍采用游戏化景深，没有真实散景光学、快门运动积分、胶片颗粒或自动测光。明暗以已有配置 EV 为基准，原场景中少女位于树荫仍会较暗，未通过更改光照或定位掩盖。已保存照片不会因新效果重新生成。

回退参考 `source-before.zip` 和 `turn-difference.patch`，只撤销上述文件的本轮增量。不要对脏工作区执行整体 Git reset。
