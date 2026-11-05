# 秋季持物、告别与冬季实时观察接入

日期：2026-11-05；Unity 2022.3.62f3，Built-in；工作区已有他人改动，未重置、提交或推送。

## 本次结果

- 停笔按 E：回忆中的右臂朝邻近物品伸出，只预备，不绕过 `InkFresh + E`。松开、取消、超时换稿后不保留拾取意图。
- 有效拾取：用原有物品身份，视觉从原位置移向各自的携带位置。吊坠提至领口后收入衣领；花固定身侧；信封在前侧收纳；行李保持地面高度、拖随和转轮，手对齐原模型的 `CarryGripAnchor`。移除统一 45% 缩小并排悬挂。
- 告别：复用项目的 ParkGirlTripo_Locomotion 身体、头肩、发丝骨骼及 `AN_wave_goodbye_01`。不再显示 NPC 的球头/球体头发/胶囊手臂。使用源动画 2.35–4.65 秒片段，适配原有两秒告别；下身采用站立姿态，防止源片段的抬腿位移。头肩转动配合发梢延迟，门边加局部轮廓光。
- 冬季：两幕使用正式 ParkScene 的 Environment 和 Bedroom_Winter_Editable 的环境几何副本，保留原机位与线索位置。副本在冬季场景 Hierarchy 内 `Winter_Album` 下对应观察区的 `Realtime_Observation`，没有玩家、同伴或章节控制器。桥直接使用 WoodenBridge_Reused 的实际网格和 MeshCollider；旧桥 BoxCollider 关闭，白边描到真实桥。
- 旧六面 `Scene_Observation` 保留为禁用的回退内容，不参与当前显示。旧菜单替换为 `Tools > Album > Update real-time observation geometry`，不会重新覆盖为六面截图。背景实时渲染，原植被面片与资源材质仍沿用；没有重新制作环境资产。

## 状态映射与修改文件

| 文件 | 作用 |
|---|---|
| Assets/Scripts/Chapters/Writing/WritingWorld3D.Carry.cs | 预备伸手、拾取位移、四种持有表现与取消 |
| Assets/Scripts/Chapters/Writing/AutumnFarewellVisual.cs | 按章节时间采样原有骨架动作、站立下身和发丝延迟，无独立输入或玩法状态 |
| Assets/Scripts/Chapters/Writing/WritingWorld3D.Autumn.cs | 原人物根继续移动，接新少女表现，停用旧告别几何显示 |
| Assets/Scripts/Chapters/Writing/WritingWorld3D.Visuals.cs | 原道具统一尺寸、恢复携带姿态 |
| Assets/Scripts/Chapters/Writing/WritingWorld3D.cs | 传递 E 意图，取消时清表现；保留此前左写字/右回忆改动 |
| Assets/Scripts/Chapters/Writing/WritingVisualAssets.cs、Assets/Resources/WritingVisualAssets.asset | 绑定告别 Prefab |
| Assets/Art/Characters/ParkGirlTripoRigged/Prefabs/PF_AutumnFarewell.prefab | 独立表现包装，复用既有模型/材质/动画 GUID，不修改 Park 人物 Prefab |
| Assets/Scripts/House/Bedroom/WinterAlbumWorkspace.cs | 场景配置标记 realtimeObservation，非存档字段 |
| Assets/Scripts/Chapters/Painting/PaintingWorld3D.Visuals.cs、PaintingWorld3D.cs | 真实桥命中、悬停及渐白缓存 |
| Assets/Scenes/Bedroom/V17/Bedroom_Winter.unity | 保存实时环境几何、灯光、桥网格命中及禁用旧截图 |
| Assets/Editor/Build/AutumnWinterPresentationAuthoring.cs、AlbumObservationBaker.cs | 复用资源的编辑器接入入口，拒绝覆盖未保存冬季场景 |
| Assets/Editor/Validation/AutumnWinterPresentationChecks.cs | 定向运行检查 |

权威状态仍为 WritingChapter.HasItem / Phase / InkFresh，以及 PaintingChapter 的取形、章节阶段和检查点。新表现参数不写入存档，不调整收集计时、双输入规则、移动、机关或过关判定。

## 验证证据

Unity 实际 Play Mode，进入现有秋冬章节后执行定向输入/状态检查；使用测试摆位与检查点，不是从新档原生键鼠完整通关：

1. 停笔 E 可见伸手，不收物；取消清预备状态。
2. 有效书写 + E 按 0、2、1、3 顺序收齐；吊坠收入、其他三物显示；行李跟随移动。
3. 使用现有检查点恢复四件携带外观，不重放拾取；无新存档字段。
4. 原少女骨架和头发实际显示，挥手关节发生变化；21 个采样时刻下身脚部与站姿最大差约 0.000002 m。
5. 冬季两幕实时几何显示，无启用的六面背景，无复制摄影/同伴控制器；41 条桥网格方向射线命中原关卡目标。
6. 真实桥观察输入：0.5 秒后取消不取形；持续 1.3 秒成功取形；取消、退出、恢复、重进后保留。
7. 第二幕退出重进，机位与阶段保留，实时房间仍显示。
8. 对比任务开始的冬季场景：1662 个既有 Transform，位置/旋转/缩放无变化（容差 1e-5）。只新增视觉子层级及相关绑定。
9. Unity 编译完成，最终 Console 查询无 error/warning。过程中修复了验证脚本的内部属性访问编译错误；一次外部磁带脚本编译触发保护退出，重新进入后完成检查。
10. 测试前备份 EditorPlaySession，测试后恢复，文件 SHA256 比对差异 0；HouseScene 退出 Play，未保存无关场景修改。

证据：`output/QA/AutumnWinterPresentation_20261105/` 下的 autumn-checks.txt、winter-checks.txt、scene-pose-comparison.json、save-restore.txt，以及以下 Unity 运行截图：

- autumn-prepare-reach.png：预备伸手，Collected=0。
- autumn-carry.png：四物持有（此截图早于门口补光，持物实现相同）。
- autumn-farewell.png：修正下身后的告别。
- winter-park-focused.png、winter-room-focused.png：两幕真实环境。

## 尚未覆盖的验收与后续项

- 未做本次正式新档、真实键鼠的秋冬整章通关，未构建独立包。实际验证为编辑器 Play Mode 定向输入、状态恢复和视觉采样，不能等同完整玩家验收。
- 告别人物已复用已有正式项目模型，但本次没有新建高精度发丝资产或重新制作动作；全分辨率情绪表现、所有时刻遮脸和衣物/手部接触还需艺术验收。回忆里的玩家本体仍是原占位人物，只有右臂增加拾取表现，没有虚称玩家正式人体完成。
- v3 中的取花后空瓶、门前缺物指向、减少金色圈以及独立行李拖动声音不在本次三项修改内，仍需后续补齐。
- 实时几何保留源场景的 LOD 配置与静态合批标记。单次编辑器观察约 16.7 ms，仅是运行取样，不代表独立包性能达标；低配与转场峰值需构建后测。与六面捕获相比，实时灯光和抗锯齿观感会有差异，需要正式画质验收。

## 回退

不要使用 git reset 丢掉其他任务。任务前的冬季场景和 WritingVisualAssets 备份在上述 QA 目录；相关源码快照在 CodeBefore。仅回退本报告列出的改动，并移除本轮新增 Prefab/脚本及其 meta；不覆盖共享 ParkGirl、摄影、磁带或存档文件。若任务后有人继续编辑冬季场景，请逐项回退观察区而不是整场替换。
