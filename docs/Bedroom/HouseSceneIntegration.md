# HouseScene 卧室布置

## 第一日磁带工作区

从 `Assets/Scenes/HouseScene.unity` 进入 Play。新进度的第一日会加法加载分支的 `Assets/Scenes/Bedroom/V17/Bedroom_Spring.unity`。后者提供原始床、书桌、椅子、窗外花园、光照探针、反射和烘焙光照。玩家、输入、章节进度和存档由 HouseScene 管理。双仓录音机、运输按钮面板、候选工作带、源带、准备垫、胶带座和操作镜头都保存在春季场景。已有夏季摄影进度时，游戏加载夏季卧室并恢复当前胶卷。

在春季 Hierarchy 展开 `Tape_Workspace_Mount / Day01_Tape_Workspace`。`Tape_Items` 放录音机、磁带和工作台道具；`Operation_Views` 下的三支定位物体分别控制桌面、录音机近看和准备区镜头。整组可移动，单件也可以在 Inspector 里调整。源带归位读取其对应的 Home 定位点，已装入的带随原存档恢复。在 `Spawn_BedroomEntrance` 调整进门视角。原书桌的交互接收面按真实桌面尺寸适配。椅子和桌子的通行范围由现有角色控制器读取，与磁带部件的点击射线分开。

入口面向窗，沿中央茶几旁边走到右手书桌，瞄准桌面或录音机点击。源带拖进 A 仓后试听，候选带拖上准备垫，准备好再按住拖进 B 仓。Esc 依次从局部近看返回桌面，再回到进桌前的位置。提交最后一份委托后仍可留在录音机前回听，离开工作区后接回下一天。第二天切换到夏季卧室。

本次层级迁移的录制、回听、三份提交及读档记录位于 `output/QA/SpringTapeMove`。之前的 Unity 验证记录位于 `output/QA/SpringBedroomTape`，包括 Game View 的 A/B 装仓、准备垫和近看截图，控制器回归记录，以及读档与回到夏季的检查结果。

## 第二日摄影与暗室

夏季使用分支的 `Assets/Scenes/Bedroom/V17/Bedroom_Summer.unity`。从 HouseScene 正常进入第二天，靠近床边照片墙，点击上排左侧三个空白相框中的任意一个，进入公园摄影记忆。装片与三处拍摄使用公园现有流程。拍满后点击返回按钮，玩家回到进入照片墙前的位置与朝向。底片在冲洗完成以前不对外显示。

返回后在胡桃木桌的相机上倒片、开背、取卷。南侧旧门洞后是小暗室，点击布帘打开，持卷靠近门洞时帘子也会让行。带卷走到暗袋再松手。暗袋、冲洗罐、三瓶药液、排液盆、印样框、相纸托盘和晾晒夹现在由夏季场景持有。晾干后，可以点成片，或回床边点击任一实拍相框打开对应帧，使用 Previous 和 Next 逐张查看。从照片墙打开的相册关闭后接续已解锁的秋季卧室。

在 `Assets/Art/Environments/House/Bedroom/V17/GameLayout/SummerPhoto/Summer_Photography.prefab` 中调整三处相框命中面、进门位置、返回相机位置与暗室台面整体位置。在 Unity 中直接打开 `Assets/Scenes/Bedroom/V17/Bedroom_Summer.unity`，在 Hierarchy 搜索 `Darkroom worktop`。展开 `DarkroomPrintStation` 即可选中具体道具；工作台、柜体和通行阻挡均在夏季的工作台定位点下。移动定位点可移动整组，单件物品的初始位置、旋转和缩放直接在场景实例 Inspector 调整。进入夏季后摄影控制器绑定场景物件和接收区，离开时释放引用。冲洗中产生的湿片、已摆在印样框里的材料按原存档恢复。

Unity 2022.3.62f3 Play Mode 记录位于 `output/QA/SummerBedroomPhoto`。照片墙进公园、返回、倒片、携卷松手投袋、翻阅成片和关闭相册使用 Game View 输入；三处公园摄影与冲洗步骤使用游戏控制器辅助验证。三次排液各验证了保存、重新读取和继续操作。三张原底片在冲洗前后的文件哈希相同。

## 第三日书写与第四日画册

关闭夏季照片墙的成片相册后，HouseScene 加载 `Bedroom_Autumn`。秋季存档也从同一卧室恢复。走近中央茶几，点击信纸、笔附近或面窗坐凳进入书写。左半屏实际使用秋季茶几，运笔、纸团和纸篓的命中随桌面一起适配。右半屏继续由原回忆相机独立拍摄。Esc 退回进入前的位置。送别完成并保存后加载 `Bedroom_Winter`。

冬季走近画架并点击画布，经白场切入原有固定视点观察、两幕画册与病房尾声。未完成时退出回画架，已经观察到的线索从章节检查点恢复。冬季完成后接原第五天出门流程。

在冬季场景的 `Winter_Album / Album_Workspace` 下调整画册。各分组对应不同的画面。

- `Foreground_Page / Page_Placement` 控制眼前画页的位置、朝向和比例。编辑这一层，正式画页和用于拖吊坠的拾取面会一起移动。外层跟随观察镜头和病房过渡。
- `Observation_View` 下的线索属于抬头观察的画面。进入章节时，观察命中区域按这些物件的 Transform 对齐。 第一幕的木桥和林地取自正式 `ParkScene`，第二幕取自 `Bedroom_Winter` 的室内。两处固定观察点各使用六个方向的场景采样，取形命中体、秋千和房内线索由正式章节驱动。修改原场景布景后，在保存编辑内容后执行 `Tools / Album / Rebake observation views from game scenes` 更新背景。
- `Page_World` 下有秋千、桥、配重机关、花轮和吊坠门。在 Scene 视图中选中物件后按 F 可聚焦。卷页中的运动使用这一层的局部 XY 平面。调整秋千、花轮和桥时保留源模型比例；大幅改变跨度或高度后应复测跳跃可达性。门锁槽跟随门上的 `KeySeatAnchor`。画中人物、固定平台和侧面相机由原章节管理。

- `Ward_Epilogue / Room` 包含尾声的地面和后墙。`Bed` 包含床垫、床脚和 `Seated_Figure`。移动整张床时，坐姿人物和鼻部截止点随床移动。
- `Ward_Epilogue / Reveal_Start` 是显现起点，`Bed / Seated_Figure / Reveal_Cutoff` 是显现截止点。显现平面沿世界 Y 轴上升，保持截止点高于起点。两点默认相差 1.64 米。一起平移整套病房会保留显现所需的步行距离；单独升降截止点会改变切回春季的时机。编辑时可选中病房子物件并按 F 聚焦。

第四天完成时，游戏冻结画册侧面相机的终帧。春季场景的茶几画页直接显示同一张 RenderTexture。直接读取第五天存档时，使用原有病房步行距离和同一台画册相机重绘终帧。Play Mode 的两幕、病房和返回春季截图及读档结果存放在 `output/QA/WinterAlbumMove`。

秋季实际书写道具位于 `Bedroom_Autumn / Autumn_Writing / Writing_Desk`。在 Inspector 中调整四个直接子物体：

- `Paper` 控制纸面、点击范围、纸团和纸上墨迹的坐标。移动、旋转或缩放此根节点会同步改变运笔命中。
- `Pen` 设置静置时的笔尖位置与朝向；书写时只移动它下面的 `Writing_motion`。点击纸、笔或坐凳都能坐下。
- `Wastepaper_Basket` 同时移动纸篓模型、投放接收区和篓内废稿。取消揉纸会恢复当前页，松手投中才消耗当前稿。
- `Mechanical_Timer` 设置 D06 机械计时器的位置。显示读取现有正式稿的剩余时间，停笔继续计时，退出则保留进度。

右侧正俯视茶几上的信纸与纸篓，鼠标从左向右划过纸面会留下笔迹。左侧回忆里的未拾取物品带金色光圈，拾取后光圈消失。回忆收集和送别沿用 HouseScene 的章节模型、回忆相机和存档。新层级、移动纸面的鼠标实测和中途重载记录位于 `output/QA/AutumnWritingMove`。 完整键鼠路线记录在 `output/QA/AutumnNativeRun`。纸笔拖动离开纸面后，重新接触的位置作为新起点，继续向右运笔才累计进度。

入口位于各自正式场景的 `Autumn_Writing / Write_at_the_tea_table` 与 `Winter_Album / Observe_the_easel`。修改房间进门位置用 `Spawn_Autumn` 或 `Spawn_Winter`。纸笔原件与画布来自分支卧室，点选范围由入口上的触发器定义。编辑器开发快捷键 3、4、5 也会先加载相应卧室。玩法和存档仍由 HouseScene 管理。

实机截图、鼠标操作、读档与控制器验证记录位于 `output/QA/AutumnWinterBedroom`。其中 `autumn-room.png`、`autumn-writing.png` 和 `winter-room.png` 为正式入口的 Game View 画面。

## 第五日早春离家

冬季病房在鼻子位置中断后，HouseScene 加载第一天使用的 `Bedroom_Spring`，在通行范围内保留玩家进画册前的位置与朝向。直接读取第五天存档时，从春季卧室内的定位点恢复。第五天关闭整套第一日设备、检视和操作 UI，收纳格展示三份成品。第五天使用场景中默认关闭的 `Spring_Departure` 层级。这里可以调整窗台雏菊、窗外残雪、门把手命中区、门轴、柔光和门槛。第一天仍使用同场景中的磁带工作区。

春季环境循环接替冬季风声，窗边的滴水和短鸟鸣随距离变化。磁带成品、床边实际洗出的照片、纸篓里的信稿和茶几上的最终画纸作为前几天的痕迹。照片来自当前已冲洗胶卷，画纸显示冬季画册留下的真实终帧。门把手可以直接点击，无前置收集。开门后仍能在房间停留，从室内跨过门槛才写入结局。

开门、化雪进度和章节完成继续使用原结局存档字段。HouseScene 管理玩家、摄影控制器和共用章节流程。最终画页位于春季场景的 `Spring_Departure / Final_Drawing_On_Tea_Table / Final_Album_Page`。在 Inspector 移动或旋转父定位点可整张调整，子物体的局部变换可微调大小与贴桌高度。第一天使用同一场景时这个分组关闭。第五天关闭这些旧工作区的显示和命中，结局完成后使用既有白场。

Unity Play Mode 记录与窗台、成片墙和门口截图位于 `output/QA/SpringDeparture`。开发环境可按 5 从冬季尾声测试，或按 6 直接预览门口。

## HouseScene 保留的公共物件

四季房间的可见布置在各自的 Bedroom 场景中调整。HouseScene 的旧地板、墙体、顶板、重复的窗框和桌子装饰已移除。旧窗台仍是 `NarrativeSceneBinding.windowAnchor` 的定位物体。

`Environment / Environment_Study / Bedroom_Furnishings` 保留床架、椅子、画架区、收纳箱、茶几和衣柜，这六件各自带有玩家通行阻挡。旧书架、相框、地毯、护墙板、各季被面和窗帘通过 HouseScene 实例覆盖移除。原家具 Prefab 和美术资源可继续复用。

录音桌及录音设备由春季场景管理，暗室工作台由夏季场景管理。两套设备分别在所属季节加载时接入 HouseScene 的玩家和存档；门窗的共用定位仍随 HouseScene 加载，最终画纸的摆放定位属于春季场景。场景实例清理记录与各季入口的 Play Mode 截图位于 `output/QA/HouseDecorCleanup`。
