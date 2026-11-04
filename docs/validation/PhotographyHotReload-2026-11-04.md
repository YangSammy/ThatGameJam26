# 摄影仪器运行中重编译修复 — 2026-11-04

## 结论与范围

在 Unity 2022.3.62f3 的 ParkScene 实际 Play 中触发脚本重新编译、Domain Reload，修复摄影仪器引用丢失。相机与测光表继续使用原实例；已完成的装片状态、胶卷身份、ISO、光圈、快门和对焦距离保留。未完成拖动取消，恢复后需释放按键再开始操作。

不需要为了测光表的 f/8 建议调整场景。f/8 只是固定展示的一组参考组合，其他等效曝光仍然有效。三处预设 EV13、光照、布局、定位及曝光判定都未改。本次不是全游戏所有控制器的热重载审计；HouseScene 仅恢复到 Edit 状态，未验收 House 全流程运行中重编译。

## 复现与根因

修复前：进入 Park，已装入 ISO400 胶卷，使用测光表时调用 Unity `CompilationPipeline.RequestScriptCompilation()`。实际重新加载后：

- `instrumentParts`、`partHomes` 从 15 项变为 0 项。
- 相机、测光表的 Unity 对象实例仍在，胶卷与相机参数仍在。
- 同步模型抛出 `The given key 'rewind' was not present in the dictionary`。
- 空拖动目标字符串在热重载后不再满足 `dragPart == null`，可阻止新动作。

原因是 Unity 不序列化字典，原初始化只在 Start 中执行。重新编译恢复组件时不会重跑 Start，直接继续使用字典。

同一复现还暴露了 `GameAmbience.Update` 的空引用：readonly 的季节音源数组重建为空引用，而原 AudioSource 子物体仍在。Awake 中的 sceneLoaded 订阅也不适合重载后的恢复。

## 修改

| 文件 | 用途 |
|---|---|
| `Assets/Scripts/Park/ParkPhotoPrototype.Reload.cs`、`.meta` | 通过 Unity 组件序列化保存并恢复交互引用及拖动原位缓存；OnEnable 清除旧手势、要求释放输入并同步原模型。序列化回调只处理托管数据，不查询 Transform、创建物体或读写存档。 |
| `Assets/Scripts/Core/GameAmbience.cs` | 季节音源数组改为可序列化引用；订阅在 OnEnable/OnDisable 成对处理；暂停/恢复原音源，避免重载时丢失引用或重建音源。 |
| `Assets/Editor/Validation/PhotographyHotReloadChecks.cs`、`.meta` | 隔离 Play 会话的重载前快照与重载后检查，验证原实例、状态、字典、取消及真实碰撞体射线操作。 |

没有重跑仪器生成方法，没有添加另一套玩法状态或胶卷存档字段。原有 Park、摄影脚本与场景快照内的 71 个文件逐字节保持不变；新分部文件增加恢复能力。环境音修改单独保留了修改前副本和补丁。

## Unity 验证结果

最终修复版本连续完成三种运行中重新编译：

| 编译时状态 | 结果 |
|---|---|
| 测光表，装片步骤 2 | 15/15 引用及原位恢复，实例 ID 不变；胶卷/ISO/参数/模式/玩家位置保持，仪器同步成功。 |
| 相机机身，正在抓取片头 | 旧手势取消，仍停在已提交步骤 2；重新抓取可继续装片。 |
| 取景器，装片步骤 7 | 模式与参数保持，输入恢复，继续调焦与拍摄。 |

每次重载后的 9 项快照检查均通过。测试是实际脚本编译触发的重载，不是人工清空字典再调用恢复方法。

**真实桌面输入验收：**

- 测光表滚轮：ISO400 → ISO800，实际读数 `F8 1/500` → `F8 1/1000`，胶卷 ISO400 不受测光表档位调整影响。
- 片头：重载中断后重新按住并拖到收片轴，松手，运行和磁盘状态均从步骤 2 到 4。
- 点击过片杆 → 点击后盖 → 再点击过片杆 → 点击目镜，步骤到 7 并进入取景器。
- 取景器滚轮：对焦从 3.7 m 到 3.93863 m，渲染参数同步。
- 光圈滚轮：f/11 → f/16；快门滚轮：1/500 → 1/1000。

光圈、快门测试用 MCP 进入既有操作模式，并临时延长该测试实例的回中等待，随后使用真实滚轮输入；这不是屏幕边缘手势的完整重测。初始胶卷和步骤也是隔离测试夹具设定，没有声称本轮从空机手动装完所有四档胶卷。

**拍摄回归：**

在上述多次重载后，复用既有 Play 测试，以 ISO400、f/8、1/500 在三个实际任务点执行生产 Capture / Camera.Render / SaveExposure。三张均成功保存；错误曝光不消耗胶卷；未显影图片仍不可读取。站位与设置由测试夹具安排，非完整手动游园。

**环境音及 Console：**

- 重载后只存在一个 GameAmbience、两个季节音源和一个全局音源；全局及当前季节音源恢复播放。
- 最终 Console error 查询为 0；仍有 9 条 AnimatorController 相关警告，在修复前重载复现中已出现，未屏蔽或修改人体/模型 Animator 来消除警告。
- 未通过工具清空 Console；重启 Play 使用编辑器原有的清理设置。修复前、后的结果分别存档。

## 数据保护及证据

已停止 Play，返回原 HouseScene，场景未脏、编辑器未编译。原存档共 29 个文件恢复后 SHA-256 全部匹配，文件数仍为 29。隔离测试会话已移至 QA 目录。没有提交、推送、重置仓库或保存场景改动。

证据：`output/QA/2026-11-04-Photography-HotReload/`

- `red-before.json`、`red-after.json`：失败复现。
- `final-meter-*`、`final-body-*`、`final-finder-*`：实际重编译前后快照。
- `native-*.json`：桌面滚轮、拖放、按钮操作后的状态读回。
- `meter-after-native-wheel.png`、`finder-after-native-wheel.png`：实际 Game 画面。
- `Entrance-after-reload.png`、`Bridge-after-reload.png`、`Meadow-after-reload.png`：实际捕获的测试底片。
- `post-reload-capture.json`、`ambient-after.json`、`final-console.json`、`final-errors.json`。
- `source-before.zip`、`GameAmbience-before.cs.txt`、`ambience.patch`、`baseline-audit.json`。
- `saves-before.zip`、`saves-before-hashes.json`、`save-restore.json`、`restore-editor.json`。

测试夹具的 `Prepare(..., true)` 应在机身模型已显示并完成当帧同步后调用。刚从测光表切换的同一工具调用中立即发射片头射线曾未命中；稳定后及真实输入均通过，未为测试调整正式交互判定。

回退本轮：删除新增 Reload 分部和测试文件（连同 meta），按 ambience.patch 回退 GameAmbience 的本轮变更，保留其余工作。不需要场景或存档迁移。
