# Editor completion shortcuts

仅 Unity Editor Play Mode 使用，先让 Game 窗口获得焦点。主键盘与小键盘均支持，按一下即可。

| 键 | 效果 | 完成后的表现 |
|---|---|---|
| 1 | 为尚未提交的录音委托准备通过验收的真实 PCM 和映射，逐份调用原提交路径 | 三份成品、最终成功弹窗；关闭后沿原流程回听或继续 |
| 2 | 补满当前胶卷，保留已有照片，缺帧生成明确标记的开发占位图 | 公园显示返回按钮；卧室进入可继续冲洗的暗室状态；不会代替显影 |
| 3 | 准备最终晾照片状态，由正常晾干更新完成冲洗和章节提交 | 可点击成片查看，关闭查看后按原流程进入秋季 |
| 4 | 完成书写和收集条件，进入既有告别阶段 | 播放告别，再正常结束秋季并返回冬季 |
| 5 | 进入病房并通过原行走、显现及中断判定推进 | 观看病房显现和记忆中断，回第五天春季；不会替玩家开门离家 |

在卧室可以直接使用上述入口，不必先手动满足章节前置。公园使用既有的 2 补满胶卷入口，其余快捷完成入口由卧室管理器处理。加载期间重复输入被忽略。已经完成的章节不重复提交；以存档完成标记和当前章节状态共同判断。

保留旧测试入口：Shift+1 录音、Shift+2 暗室、Shift+3 秋季、Shift+4 冬季、Shift+5 病房、Shift+6 结局。它们仍是旧的进入/重开测试功能，与新完成快捷键语义不同。6 也保留原入口。

这会修改当前开发测试存档。已有拍摄图保留；不合格的现有录音在生成通过版本前保留独立 PCM 备份。没有修改存档格式。新增完成入口用 UNITY_EDITOR 隔离，不进入独立包；既有开发包入口保持原行为。

## 实现文件

- Core/WBMain.DebugChapters.cs：按下触发与 Shift 旧入口分流。
- Core/WBMain.EditorCompletion.cs：进入对应场景、取消当前手势、稳定保存、调度完成和防重复。
- Chapters/Tape/HouseRadioDeck.EditorCompletion.cs、TapeSampleTransport.cs：真实结果生成与正常逐委托提交。
- Chapters/Photography/PhotoWorld3D.DebugDarkroom.cs：最后晾晒稳定节点和正常完成链。
- Park/ParkPhotoPrototype.DebugFilm.cs：补片时防止覆盖尚未保存完的实际拍摄。
- Chapters/Writing/WritingChapter.cs、WritingWorld3D.EditorCompletion.cs：告别前置和既有表现。
- Chapters/Painting/PaintingWorld3D.cs、PaintingWorld3D.EditorCompletion.cs：临时自动行走驱动既有病房显现；Esc 可取消，重新按 5 继续。

以上脚本路径相对 Assets/Scripts。本次不修改场景布局、模型或定位物体。

## 验证范围

实际 Unity Play Mode 通过 MCP 调用与数字键相同的 EditorCompleteStage 入口，由游戏 Update 继续运行后续表现。未用物理键盘逐键验收；不是正式新档完整通关测试。公园 2 保留原入口，本轮只静态核对其按键与补片调用，没有重新走一遍公园实拍。

- 1：三份结果提交，最终弹窗显示，剧情进入 SummerPhoto。
- 2：三张照片记录，胶卷 AwaitingDevelopment，摄影/暗室总章节仍未完成。
- 3：晾晒结束后 Developed、印样 Complete、章节完成；成片数据可读且查看界面能打开。
- 4：既有告别结束，秋季 Completed，进入 WinterPainting。
- 5：病房完成，退出操作视角，进入 SpringDeparture。
- 首次暗室测试暴露错误的归一化 BagTurns 和倒片状态；已修复并在新 Play 会话复测成功。原失败记录保留。
- 跨季后重复快捷键发现内存章节未恢复导致返回旧章节；补充存档完成标记防重复检查。

截图和原始状态记录：output/QA/EditorCompletion_20261105/。测试使用独立 EditorPlaySession 备份，结束后恢复原目录，保存哈希核对结果。
