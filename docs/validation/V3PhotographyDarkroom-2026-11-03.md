# V3 摄影与暗室：首轮执行记录

日期：2026-11-03。Unity 2022.3.62f3，Built-in。按已批准的 v3 对齐计划先执行摄影与暗室；保留当前场景、模型、物品身份、定位与存档格式。此报告不是完整原生输入或最终发行验收。

## 本轮落地

1. **先进入公园回忆，再准备相机。** 现有书房相机入口直接进入 ParkScene；拍满返回后继续原暗室流程。书房不再提供另一套会改写胶卷的简化装片操作。照片墙的正式空间入口留在房间布置任务中。
2. **一个装片状态来源。** 公园保留倒片钮拉起开盖、四档胶卷入仓、拉片头接轴、过片杆收紧、合盖、首次过片。未准备完成不能进入取景器或消耗照片。书房自动保存不会覆盖公园的细分步骤和 ISO。
3. **修复真实命中遮挡。** 旧后盖命中框覆盖倒片钮和过片杆。现在只有对应装片步骤的部件参与射线命中；模型视觉、定位和碰撞布局没有移动。原生输入和测试共用 Begin/Move/EndBodyPointer 的射线路由。
4. **中途离开与续玩。** 公园按 Esc 放下仪器，再按 Esc 暂停，点击 `Leave memory` 返回书房。再次进入，从尚未完成的拍摄地点附近继续；使用独立 resumePoint，不把玩家自动摆到构图答案上。胶卷、曝光记录、对焦距离/光圈/快门设置与稳定装片步骤保留。无法保存或读取已提交胶卷时留在当前场景。
5. **暗室实体辨认。** 三种药瓶及对应显影、停显、定影托盘增加同色的一、二、三道实体标记；水洗托盘使用波纹标记。它们是保存进 Hierarchy 的视觉子物体，没有 Collider，没有新增输入或化学判定。现有悬停提示和高亮仍保留。

`ParkCameraLoading` 新增的是计算属性和构造/转换规则；没有新增序列化存档字段或版本。旧的 Ready/Closed/Threaded 记录仍能解析。ParkPhotoWaypoint.resumePoint 是场景引用，不是存档字段。

## 修改位置

| 文件/对象 | 用途 |
|---|---|
| `Assets/Scripts/Core/WBMain.SharedHouse.cs` | 摄影入口、返回自由视角记录，保持暗室路由 |
| `Assets/Scripts/Chapters/Photography/PhotoWorld3D.cs`、`.ParkFilm.cs`、`.Recovery.cs` | 关闭书房重复装片输入，装片状态只读，入口提示一致 |
| `Assets/Scripts/Park/ParkFilmStore.cs` | 解析机械稳定步骤与真实续玩证据，保留原序列化字段 |
| `ParkPhotoPrototype.Controls.cs`、`.Assets.cs` | 共用射线手势路由、步骤提示、有效命中区域 |
| `ParkPhotoPrototype.cs`、`.Bridge.cs` | 离开回忆、未完成地点恢复、设置保存与退出失败保护 |
| `ParkCompanion.cs`、`ParkPhotoWaypoint.cs` | Start 前后均可恢复同伴路线节点，独立续玩定位引用 |
| `Assets/Scenes/ParkScene.unity` | 新增 Resume_Entrance、Resume_Bridge、Resume_Meadow；旧定位不动 |
| `Assets/Scenes/HouseScene.unity` | 七个模型下的 ProcessCue 实体标记 |
| `Assets/Art/Interaction/DarkroomCues/` | 实体标记材质；原模型材质未覆盖 |
| `Assets/Editor/Validation/V3PhotographyChecks.cs` | 装片、命中、取消、状态恢复与真实相机拍摄回归检查 |
| `Assets/Editor/Validation/V3PhotographyAuthoring.cs` | 已执行的一次性场景补充；不在运行时生成 |

## 实际验证

证据根目录：`output/QA/2026-11-03-V3-Photography-Darkroom/`。

| 检查 | 结果与边界 |
|---|---|
| 书房覆盖机械装片步骤 | 修复前 5 项失败；修复后 6/6 保存检查通过。见 loading-red/green.json |
| 装片状态序列化、旧记录兼容、初次进入判定、同伴 Start 顺序 | 37 项通过，见 contracts-final.json |
| ISO100/200/400/800 命中与装片 | 40/40 通过：空机禁拍、倒片钮、取消拿卷、入仓、片头、收紧、合盖、过片和不耗片。见 pointer-tests-final.json。控制器用真实 Collider 射线；未替代全套原生鼠标手势验收 |
| 首次公园入口 | 空机可进入；仍位于原出生点 `(-15, 0.02, -12)`，不能曝光。修正了 JsonUtility 默认对象被误当作已有进度的回归 |
| 三处实际相机成像 | 入口、桥、草地共 16 项检查通过；使用游戏 Camera.Render 保存三张不同画面，不是调试占位图。失焦不耗片，草地响应与快门分开，不提前允许回看。站位和参数由测试夹具准备，不算玩家手动完成构图 |
| 原生电脑操作 | 实际 Esc 放下相机、再次 Esc 暂停，点击 Leave memory 返回书房；拍满后实际点击 Return to darkroom 返回。未用接口调用冒充这几个按钮的原生点击 |
| 一张后离开重进 | 实际切换 House→Park，恢复桥边 resumePoint、1 张照片、ISO800、原对焦距离/光圈/快门及装片步骤7，见 resume-after-one.json |
| 倒片、取卷 | 倒片前不能开后盖；50% 倒片取消保存/加载仍为50%；继续倒完再开盖，见 rewind-recovery.json |
| 暗室整链 | 取卷进暗袋、40%装轴恢复、封罐，三轮倒药/搅动/等待/排液，水洗、取片晾干、相纸与底片入框、关框、分次曝光、开框、相纸显影/停显/定影/水洗/晾晒均通过现有控制器/接收区检查。见 darkroom-chain.json；不是全套原生长按拖动验收 |
| 已完成场景重载 | HouseScene 真正重载后仍为 Developed / Complete，3张照片保留，底片在印样框、相纸在晾晒夹；见 completed-scene-reload.json |
| 存储故障退出保护 | 在独立测试卷临时注入无效 manifest，LeaveParkMemory 返回 false，留在暂停的 ParkScene；随后恢复原字节。见 storage-failure.json |
| Console | 初始0 warning/error；完整链结束仍0。最后的故障注入保留1条预期 JSON parse warning，未清 Console 冒充无错误。没有新增编译错误 |
| 场景与存档保护 | House 3658个、Park 1799个既有 Transform/RectTransform 的位置、旋转和缩放均未改变，无既有 Transform 删除；新增29和3个视觉/定位 Transform。29个原存档文件已逐一恢复并核对 SHA256 |

## 图像证据

- `park-roll-complete.png`：测试夹具完成实际成像后的 Game view。
- `darkroom-finished.png`：冲洗、印样和晾晒完成后的实际 Game view。
- `developed-1.png`～`developed-3.png`：显影完成后通过正式读取接口导出的照片。
- `contact-sheet.png`：由上述三张实际照片生成的接触印样。
- `completed-test-session.zip`：本轮测试卷和完成检查点的隔离副本，未留在玩家活动存档里。

## 未完成与下一优先级

1. **摄影人物表现仍需验收和调整。** 测试成片能看到人物正脸；第三张中出现玩家双手，但没有证明自然的双方牵手接触已成立。动作字段/判定通过不能代替画面符合 v3。保留了现有人体与动作资产，本轮没有改写人物模型、Animator 或骨架。
2. 全套原生按住、滚轮、片头拖动及取消；暗室原生携卷走动、药瓶倾倒、排液和曝光长按仍未全覆盖。控制器与射线测试已通过，不标成鼠标全流程验收。
3. 三个续玩点已绑定；真实场景往返验证了第二处，第一、第三处仍需各自的原生续玩验收。没有声称测试了任意故障下的所有存档恢复。
4. 药瓶/托盘实体标记已补；龙头、晾片与晾照片架、印样框仍需更完整的非文字辨认。现阶段继续保留简短提示，避免直接删文字造成不可玩。
5. 照片墙三框的正式入口、季节空间及全局去文字化按后续房间/表现任务执行。
6. 本轮没有制作新的独立包，也没有把 Editor 测试报告当作独立包验收。旧的“必须书房装片后才能进公园”测试文档已被 v3 路由取代，执行时以本报告为准。

## 回退

`source-before.zip` 是本次任务开始前的源文件快照，包含前一轮磁带与其他工作的已有改动。只按 `changed-from-task-baseline.json` 选择性还原本轮文件；不要整体 reset 或覆盖其他 agent 的成果。新增 cue 材质、检查工具及其 meta 应随对应引用一起回退。`saves-before.zip` 仅用于恢复测试前存档；本轮已恢复29/29，不需要再次导入覆盖正常游玩。

交付时 Unity 停在 HouseScene 编辑模式，场景 clean，未提交或推送。
