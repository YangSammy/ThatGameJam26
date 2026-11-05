# 四季卧室交接：隐藏旧 HouseScene

## 实现范围

采用用户确认的快速方案：HouseScene 继续承载玩家、章节与存档系统，公园仍通过它启动返回流程；它的旧房间不再作为可见的中转空间。本次没有删除 HouseScene，没有修改任何场景资产、定位物体、存档文件或存档格式。

- 启动即创建全屏遮罩，覆盖旧房间初始化和卧室加载阶段。
- 换季统一先淡出（0.25 秒），保存稳定进度，解绑并卸载上一卧室，再加载目标卧室。目标绑定、相机落位和物理同步完成后淡入（0.25 秒）。加载和淡变期间不接受章节输入；结束后使用现有按键释放门控。
- 四种卧室退出清理不再重新激活旧房间、不再恢复旧 HouseScene 出生点或视角。系统场景在卸载期间暂时成为 Active Scene 只用于管理，不会透出画面。
- 冬季操作保留画册观察层、画页层、路线层和原人物；继续隐藏卧室背景，但不再显示旧 HouseScene 布景。保留原章节灯光，Active Scene 保持冬季场景。
- 病房记忆中断时立即遮住画面，避免返回第五天之前短暂露出冬季卧室。
- 加载/绑定失败时保持全屏遮罩，显示 Retry。重试会从现有稳定存档重新建立系统和场景绑定；绑定失败后的临时状态不会通过 WBMain 保存覆盖原检查点。
- 开发入口也通过统一的卧室交接执行，避免快捷入口恢复旧房间。

## 修改文件

- `Assets/Scripts/Core/WBMain.BedroomTransition.cs`：统一交接、遮罩、错误恢复。
- `Assets/Scripts/House/Bedroom/BedroomTransitionOverlay.cs`：全屏遮罩和重试 UI，仅运行时表现。
- `Assets/Scripts/Core/WBMain.SpringBedroom.cs`：四季绑定与交接入口。
- `Assets/Scripts/Core/WBMain.SharedHouse.cs`、`WBMain.cs`、`WBMain.Input.cs`：初始化遮罩、过渡状态和输入阻挡。
- `Assets/Scripts/Core/WBMain.DebugChapters.cs`：快捷入口复用同一交接。
- `Assets/Scripts/Core/WBMain.Progress.cs`：绑定失败时保留稳定存档。
- `Assets/Scripts/Core/WBMain.Narrative.cs`：病房中断瞬间遮挡。
- `Assets/Scripts/House/Bedroom/SpringTapeRoom.cs`、`SummerPhotoRoom.cs`、`NarrativeBedroom.cs`、`DepartureBedroom.cs`：只解绑当前卧室，不恢复旧房间与旧出生点。

## 已完成检查

使用 Unity 2022.3.62f3 自带 Roslyn 编译器及项目现有 Bee 引用参数，分别编译运行程序集与 Editor 程序集，退出码均为 0。输出单独保存在 QA 目录，没有覆盖 Unity 的 Library 程序集。

静态核对：正常换季和开发跨季入口的解绑都位于统一遮罩流程内；卧室 Leave 不再恢复旧出生点或启用旧房间；加载标志在目标完成落位并淡入后才清除；错误状态保留鼠标以便重试。

证据：`output/QA/SeasonTransitions_20261105/compile-runtime.txt`、`compile-editor.txt`，以及修改前脚本备份和 git 状态记录。

本轮 MCP 返回 HTTP 502，没有进行 Unity Editor Play 验收、真实鼠标操作或独立包测试。下面流程交由人工确认；编译通过不代表视觉交接已实际验收。

## 人工测试顺序

1. **新档启动**：从正常入口 Play，应在遮罩后直接看到春季卧室；不应闪现旧桌子、旧房间或旧出生点。淡入后松开所有按键，再移动和点击。
2. **春 → 夏**：完成磁带，关闭成功弹窗，按现有 Esc 流程离开工作视角。应淡出后进入夏季卧室，能点击照片墙。可以用 Editor 的 1 快速完成磁带来检查此交接。
3. **公园返回**：从夏季完成装片进入公园；分别测试未拍满时 Leave memory、拍满后的 Return to darkroom。应遮住后台加载，直接看到夏季卧室；原返回位置合法时保留，胶卷和相机进度不变。
4. **夏 → 秋**：完成冲洗并逐张看完照片，关闭成片浏览。应进入秋季卧室，点击纸笔仍可开始书写。没有旧 HouseScene 的桌子或相机残留。
5. **秋 → 冬**：完成告别后返回，等待进入冬季卧室。点击画架，确认画册公园/空房间观察画面、画页及人物正常；按 Esc 返回冬季卧室，不应出现旧房间。
6. **冬 → 第五天**：完成病房中断，或用既有 Editor 5 触发完成。应直接从遮挡进入春季卧室；最终画页、成品和开门离家仍正常。
7. **退出重进**：各季退出当前操作再进入，确认相机返回季节卧室内的原位置。跨 Play 测试读档时，注意编辑器“每次 Play 重置测试存档”设置；不要用新档模式判定恢复失败。
8. **连续快捷入口**：Shift+1、Shift+2、Shift+3、Shift+4、Shift+5、Shift+6 各等待淡入和释放按键后再切下一个，检查没有重叠卧室或旧房间闪现。

可选错误路径：仅在测试副本中暂时禁用目标卧室 Build Settings 条目，触发切换后应保持黑底并出现 Retry，而非回到旧 HouseScene。恢复条目后点击 Retry，应从稳定存档重新进入对应季节。

## 后续边界

Hierarchy 中仍能看到加载中的 HouseScene 系统场景，这是本轮方案的预期结果。若要连这个后台场景也移除，仍需另行迁出系统组件和跨场景引用，不能只替换场景路径字符串。
