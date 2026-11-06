# 暗室 Scene 摆放

打开 `Assets/Scenes/Bedroom/V17/Bedroom_Summer.unity`，展开场景根节点
`Darkroom - 暗室摆放`。

- `00 Worktop - 工作台与碰撞`：桌面、拖放操作区域和角色碰撞边界。
- `01 Items - 移动物品父节点`：18 个带编号的物品父节点。
- `02 View - 入口视角`：数字键 2 使用的 `View_Darkroom_Worktop`。

## 如何摆放

在 **Edit Mode** 选择带编号的物品父节点，用 W 平移、E 旋转，保存 Scene。
模型、BoxCollider 点击范围、物品落点、盖板/龙头和相关灯光、水流会整体跟随。
启用 Scene 的 Gizmos，选中物品父节点可见橙色 BoxCollider 线框。

不要只移动 `PF_…` 模型子节点，也不要单独把点击范围、Placement 或 Home
锚点拖出父物品。父节点用于摆放，内部原有 Transform 的缩放用于模型和交互，
建议保持缩放不变。Inspector 中的 `itemId`、`primaryTarget` 和原有 binding
引用是程序绑定，不要当作显示名称修改。

相机选择 `01 Camera - 相机摆放`：Scene 中提供外观预览；运行时预览自动隐藏，
原有机械相机及回卷、后盖、取片控件按这个锚点定位。不要单独移动预览模型。

物品归组：相机、暗袋、显影罐、三瓶药液、废液盆/龙头、负片起点、负片晾夹、
接触印相框/盖板、曝光灯/开关/灯光、相纸起点、三只药盘、水洗盘/龙头/水流、
照片晾架、计时器。可在 Hierarchy 中重命名父节点的显示名称。

摆放时保持物品在工作台拖放区域内，避免相互穿插，给搬罐和开盖留空间。
若移动桌面或改变其尺寸，需要同时检查 `OperationArea` 的范围、角色碰撞边界
和入口视角。重摆后从新测试进度验收，旧存档可能会恢复之前保存的可移动物品位置。

## 本次验证（2026-11-06）

- 32 个流程 prop 引用及其 Placement 均归属各自物品父节点。
- 所有暗室模型、Renderer 和 Collider 都有物品父节点。
- 对每个物品父节点临时平移、旋转并还原，子节点跟随检查通过。
- 运行时 `water`、`print-tap`、`tank-lid`、`print-frame-lid` 的点击 ID 保持正确。
- 相机预览在 Play Mode 自动隐藏。
- 数字键 2 同函数入口与完整暗室流程回归：34 项通过。
- 最终 Console 查询无错误/警告。未做独立构建测试。

工具：`Memory/Photography/Organize darkroom hierarchy`，重复执行检查现有结构，
不会重新布局。实现：`Assets/Editor/Photography/DarkroomHierarchyAuthoring.cs`。
流程回归：`MemoryWhitebox.Editor.DarkroomCameraPlacementChecks.Run(WBMain.Instance)`，
需在隔离的新测试存档中，通过 2 入口加载完成后执行。
