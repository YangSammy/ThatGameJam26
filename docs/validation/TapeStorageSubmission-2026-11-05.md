# 收纳盒成品格验收与两仓卷带

## 本轮实现

- 春季 Bedroom_Spring 移除外部 Finished Tape Rack 及其独立提交面；收纳盒中间增加 Finished compartment，三个 finishedTapeAnchors 均在盒内。现有磁带身份、桌面落位和录音机定位保留。
- 弹出当前录制 B 带后，拖入打开的盒内成品格、松手触发原录音质量检查与提交事务。点击空格不提交。
- 拒绝或取消时回到拖动前的桌面位置；录音 PCM、样本映射和物品身份保留。可以重新装入 B 回听、续录，或使用既有重试规则。续录仍追加到末尾，不覆写先前录音。
- 成功后实际工作带进入对应成品定位，不生成另一盘替身。沿用原存档格式和章节完成规则。
- 第三/第四机械键及 J/L 控制已装入且关仓的 A/B 快退、快进，再按同键停止。各自带位独立，B 不超出已录制部分；一仓到头不强制停止另一仓。B 录制期间禁止卷带，先按 S 停录。
- 旧提交入口仅提示将磁带放回盒内，不再直接提交。编辑器完成快捷入口改用同一验收保存逻辑。

## 主要文件

- Assets/Scenes/Bedroom/V17/Bedroom_Spring.unity：盒内格、隔板、源带/工作带位置和三个成品定位；删除外架。
- HouseRadioDeck.StorageSubmission.cs：拖放接收、条件检查、失败回退。
- HouseRadioDeck.Workbench.cs / WorkbenchAssets.cs / DropPreview.cs：手势、提示、白色落位描边。
- HouseRadioDeck.Recording.cs / RadioTapeChapter.cs：按弹出的实际物品身份验收，沿用原保存事务。
- HouseRadioDeck.Source.cs / DualDeck.cs / DualDeckVisual.cs / TapeSampleTransport.cs：两仓卷带和机械键反馈。
- HouseRadioDeck.EditorCompletion.cs：保留编辑器完成入口。
- Assets/Editor/Validation/TapeStorageSubmissionChecks.cs：本次针对性运行检查；相关旧验证脚本改用盒内拖放入口。

## 验证边界与证据

编译通过。在 Unity Play Mode 调用实际指针处理器、射线和拖放接收逻辑，通过 38 项针对性检查，包括三份委托提交、失败 PCM/映射保留、取消、重新装仓、磁带音频文件重载、完成检查点恢复、双仓卷带及单独 B 卷带。

这些测试使用预先准备的合格磁带和录音数据，不代表真实鼠标从选带到全部录制的完整通关。测试夹具关闭音频时钟以确定性推进样本；出现三条 `Can not play a disabled audio source` 夹具警告，不作为正常运行结果。未新增编译错误。

- output/QA/TapeStorageSubmission_20261105/checks.txt：38 项检查结果。
- output/QA/TapeStorageSubmission_20261105/finished-compartment-play.png：Play 中实际相机渲染，展示三盘成品带归盒。
- output/QA/TapeStorageSubmission_20261105/save-restore.txt：原测试存档恢复及 SHA256 核对。
- output/QA/TapeStorageSubmission_20261105/Bedroom_Spring.before.unity：迁移前场景备份。

未完成额外的重启 Play 全文件读档验收：最后 MCP 返回 HTTP 502。此前停止 Play 成功；已恢复原测试存档。正式存档结构未改。

## 快速人工测试

1. 打开春季收纳盒：源带在一侧、工作带在另一侧，中间成品格为空；桌上不再有外部提交架。
2. 装 A、准备并装 B；F 播放 A，S 开始录制，再按 S 停录。保持已有切噪和续录规则。
3. 停录后按 J：两仓快退，再按 J 停止；按 L：两仓快进，再按 L 停止。也点击机械第三/第四键比较。B 到已录末尾时停止，A 可继续；A 空仓时 B 仍可卷带。
4. 故意做一份明显不完整录音，点击 B 仓弹出，拖到打开的盒内成品格后松手：出现拒绝原因，磁带留在桌上，重新装 B 仍能回听原录音。
5. 拖到成品格前按 Esc：取消本次拖动，不验收、不丢磁带。点击空成品格不应提交。
6. 用合格录音重复归盒：弹出成功反馈，实际 B 带移入中间成品格，下一份委托可继续；三份完成后共三盘成品。
7. 中途退出操作再返回，检查已录内容和已完成成品仍在。跨 Play 读档须注意当前编辑器默认每次 Play 重置测试档，不能以新档模式判断恢复失败。

后续默认快速迭代：实现、编译和必要代码检查，交付人工测试清单；不再主动安排真实鼠标操作验收。
