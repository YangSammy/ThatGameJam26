# 公园参考图与人物成片表现

日期：2026-11-04。Unity 2022.3.62f3 / Built-in。使用当前工作区，未提交、推送或覆盖其他 agent 的修改。

## 本轮修改

| 文件 | 修改 |
|---|---|
| `Assets/Art/Photography/References/EntranceReference.png` | 用当前正式少女及 ParkScene 原参考定位重新拍摄入口参考 |
| `Assets/Art/Photography/References/BridgeReference.png` | 当前少女的侧背参考，与回眸成片区分 |
| `Assets/Art/Photography/References/MeadowReference.png` | 当前少女的未牵手参考，不含玩家前景手 |
| 上述三个 `.meta` | 关闭 NPOT 自动缩放，避免 960×540 被导入成 1024×512；保留 GUID |
| `Assets/Scripts/Park/ParkPhotoPrototype.Bridge.cs` | 保存参考图时保留原图比例，不再强制拉成 960×600 |
| `Assets/Art/Characters/ParkGirlTripoRigged/Scripts/ParkGirlPresentation.cs` | 区分中立侧背姿态与回眸/伸手回应姿态，防止参考状态沿用回应侧头而露出脸 |
| `Assets/Art/Characters/ParkGirlTripoRigged/Prefabs/PF_ParkGirlTripo_Locomotion.prefab` | 启用现有自然遮脸表现，低头 36、侧头 50；已有 neckBone、过渡时间字段随 Unity 序列化保存 |

复用已有回头、肩部跟随、发丝滞后、伸手动画、掌心 IK、握手及快门表现。未新建人体、骨架、Animator 或玩法状态机。姿态调整分布在原有上胸/颈/头骨上；逻辑根与交互定位不动。

## 与 v3 的关系

依据 v3 第 6–7 页：入口参考站姿稳定、快门有头发/肩线变化；桥边侧背参考变为回眸；草地参考没有牵手前景，先点伸出的手、再拍照；成片不靠清晰正脸表达。

本轮使正式少女进入上述参考/回应链路。入口保留背影，桥边用低头、偏头与发丝遮住主要眼部特征，草地以牵手和头发阴影表达回应。当前桥边仍可能看到鼻口局部；这不是所有机位完全无面部可见的最终验收。v3 指定的背光轮廓和草地恰好的光遮脸，也未通过移动现有灯光或定位来强行实现。全可达机位遮脸与最终电影化光影仍需专项美术验收。

## Unity 实际验证

证据目录：`output/QA/2026-11-04-Park-Presentation/`。

测试中通过 MCP 准备 ISO800 已装片胶卷，并将玩家放到现有参考定位、设置曝光/焦点。随后由正常 Update、正式动画和原生鼠标操作执行回应、牵手与快门。**这不是从书房装片、全程徒步找机位和手动调参的无辅助验收。**

| 检查 | 结果与证据 |
|---|---|
| 入口真实快门 | 原生左键，1/3；`entrance-native-capture.json`、`Entrance-exposure.png` |
| 桥边真实快门 | 原生左键，2/3；`bridge-native-capture.json`、`Bridge-exposure.png`、`bridge-game.png` |
| 草地先回应再拍 | 原生点击少女手，保持 2/3，handJoined=true；`meadow-native-joined.json` |
| 掌心接触及重复表现调用 | 接触距离约 1.1 cm；同帧重复 ApplyPhotoPose 无累积漂移；`meadow-offer-check.json` |
| Esc 暂停 | 原生 Esc；暂停时伸手进度、掌心位置不漂移；`meadow-pause-check.json` |
| 中途离开/重进 | 原生 Leave memory 返回 House，再经正式进入接口回 Park；恢复 2 张照片和已牵手状态；站位仍由测试夹具准备。`restore-meadow-entry.json`、`restored-meadow-check.json` |
| 恢复后第三张 | 原生左键，3/3；joinedHands=true；`meadow-native-capture.json`、`Meadow-exposure.png`、`meadow-final-game.png` |
| 防提前回看 | 正式 TryReadDevelopedFrame 返回 false / Develop film first；并未改开发完成状态 |
| 拍满返回 | 原生 Return to darkroom 返回 HouseScene；记录保留三帧 |
| 参考比例 | 新建任务保存的参考为 960×540；`final-reference-ratio.json` |
| Console | 基线及结束 warning/error 查询均为 0，未清空 Console 掩盖错误 |

三个 exposure PNG 来自实际快门写出的负片字节，仅为开发验收复制到输出目录，不向游戏内提前开放。`privacy-range` 图片是关闭虚焦、临时偏移相机的遮脸诊断，不是正式照片或移动中牵手验收。

## 虚焦应怎样设计

v3 明确要求定焦镜头的滚轮改变**对焦距离**，不改变焦距；光圈、快门独立操作；等效曝光组合可通过；玩家再次改变焦点后重新检查对应条件。文档没有要求薄透镜、弥散圈及全物理光学模拟。第 10 页“默认虚焦、正确实物清晰”描述的是冬季观察，不能当作公园少女必须始终模糊的要求。

推荐保留真实距离与曝光参数，用“焦点附近清晰范围 + 范围外连续渐变虚焦”服务操作。人物处于清晰范围时应看清衣服、头发与动作；不露脸由构图、发丝、姿态和光影完成。清晰判定与画面应一致。

当前 shader 已有上一轮修复的清晰范围：`max(0.2m, focusDistance*0.065)`，范围外按深度差平滑增加模糊。本轮未改 shader。光圈当前影响曝光和机械环，但**尚不影响该清晰范围**，因此不是全物理景深。后续可让光圈温和影响范围并保留可操作的最低宽度，不应增加焦距变化或偷偷自动对焦。

## 仍需打磨/未验证

- 草地前景手距离镜头很近，当前深度虚焦偏重，削弱牵手细节；建议后续限定前景最大模糊或将牵手接触区纳入宽容清晰范围，并同时核对录入照片效果。
- 桥边完整正脸已被姿态和发丝遮挡，但局部鼻口可见；全角度、光照变化、全过程遮脸未获认证。
- 现有草地任务允许近景，但通用 Assess 记录会写 framed=false/Keep head and body in frame，任务门槛随后 accepted=true。实际任务成功，不阻塞快门；属于旧通用评价与任务评价的语义不一致，本轮未修改玩法判定或存档。
- 未重新执行完整装片/暗室、全部原生长按滚轮、独立包及全五日测试。

## 数据保护与回退

- HouseScene、ParkScene 相对本轮备份逐字节相同；无布局、相机、出生点或接收定位修改。
- 原 29 个存档文件已恢复且 SHA256 全匹配：`save-restoration.json`。隔离测试卷归档至 `finished-test-session/`。
- 存档格式、物品 ID、胶卷/照片身份及判定不变。旧存档已保存的参考 PNG 继续保留，未覆盖历史记录；新版图片用于尚未捕获参考的任务。
- Unity 停在 HouseScene Edit 模式，场景 clean；未构建或提交。
- `source-before.zip`、`references-before.zip` 为本轮前备份，`turn-difference.json/.patch` 记录增量。回退只还原上述文件的本轮增量，不对整个工作区执行 reset，以免覆盖其他工作。
