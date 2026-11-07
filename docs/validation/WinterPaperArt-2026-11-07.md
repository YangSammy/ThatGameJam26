# 冬日纸上关卡美术接入

范围：两幕纸上平台场景及纸上角色。现实观察公园、卧室与病房底图不在本次替换范围。

导入 Assets/Resources/WinterPaperArt/ 的两张独立背景及24帧透明角色图集。背景由已确认概念图通过内置 image_gen 去除玩法物件后生成；机关和平台由独立平面网格呈现，受原有状态驱动，避免重复背景机关。平台边缘与原逻辑落点对齐，秋千两腿落地、单点悬挂，木桥双栏，花轮六踏板保持水平。门沿用真实吊坠凹槽网格并更新材质。

角色使用无围巾 Sprite；行走随有效位移推进，空中姿态随速度变化，起跳/落地沿用现有瞬态；花轮载人和复活瞬移不触发行走或转向。生成图逐格按不透明像素范围确定脚底，垂直运动仍归关卡逻辑所有。原骨架及接触逻辑保留为隐藏载体。

代码：PaintingWorld3D.PaperArt.cs；BuildAlbum/RefreshAlbum/Deactivate/Dispose 接入；EmptyShapes 使用新版秋千和桥的轮廓。运行时资源在销毁时释放，原渲染器与材质在退出时还原。冷加载结局也初始化新角色。

验证：
- Unity 编译完成，Console error 0。
- WinterPaperArtChecks.Run 通过：6张离屏图、checkpoint不变、24帧脚底对齐、左右跳跃、花轮乘坐、复活、未取形隐藏及渲染器还原。
- 临时以 additive 打开实际 Bedroom_Winter，使用真实 workspace/机关锚点渲染两幕，随后关闭且未保存场景；route scale 为 (1,1,1)。截图：output/QA/WinterPaperArt_20261107/actual-layout-0.png 和 actual-layout-1.png。
- NarrativeRegressionChecks：11项通过、1项失败。冬季第一幕秋千桥通行、第二幕压板花轮吊坠、观察冻结、存档等通过。失败项是病房退行旧测试：测试要求 A 退行，但本次未修改的 PaintingChapter.cs 第192行起已明确 D-only。该纯模型测试不调用新美术代码，未为迁就旧测试改变病房行为。
- 最终活动场景 ParkScene、Edit Mode、isDirty=false。

限制：未进行真实输入全流程通关、独立Player构建；坐秋千暂使用现有跳跃落地帧表现屈膝，未另创专用坐姿Sprite。外观适配保留原关卡比例，不将概念图像素位置当碰撞尺寸。
