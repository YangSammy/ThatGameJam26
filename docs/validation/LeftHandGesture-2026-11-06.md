# 公园左手牵手短动画 — 2026-11-06

## 本轮范围
复用现有 PSX 左手臂、少女模型、Generic 动画控制器及左臂单侧网格。没有导入新模型、插件或全身动作；没有改摄影通过条件、胶卷记录、存档结构、场景布局或定位物体。

## 表现
- 0–0.28 秒：左手由画面下方张开伸出。
- 0.28–0.44 秒：到达少女右掌后轻握，手指使用专用姿态修正；不再沿用握相机的闭合姿态。
- 保持：跟随少女实际掌心位置与朝向，手腕由现有 IK 解算，不缩放骨骼。
- 解除：约 0.14 秒张手、0.28 秒退回，之后隐藏；期间右臂始终隐藏。
- 主动暂停、失焦和输入交接冻结/隐藏；恢复不跳到动画末端。
- 满卷自动返回面板允许已经开始的短收手表现完成，仍禁止玩法输入；主动暂停和场景切换不属于此例外。
- 测光表、机身和参数特写隐藏并取消临时手部动画；返回取景时按照已有牵手状态重新张手接近。
- 拍照只同步当前姿态，不提前推进牵手动画，不改变原来的摄影冷却时间。

## 修改文件
- Assets/Scripts/Player/FirstPersonHands.Handhold.cs：短动画阶段、轨迹、专用手指与拇指姿态。
- Assets/Scripts/Player/FirstPersonHands.Skeletal.cs：与现有腕部 IK 及动画混合衔接。
- Assets/Scripts/Player/FirstPersonHands.cs：左臂显示、收回后的隐藏、缩放时间和快门采样。
- Assets/Art/Characters/ParkGirlTripoRigged/Scripts/ParkGirlPresentation.cs：少女在接触后握手，自动满卷面板期间可松手。
- Assets/Scripts/Park/ParkPhotoPrototype.Controls.cs：仅新增读取式的满卷收手表现许可，不修改输入或拍照判定。
- Assets/Editor/Player/FirstPersonArmChecks.cs：原骨架检查补充张手与收回断言，测试销毁前清除临时胶卷引用。
- Assets/Editor/Player/LeftHandGestureChecks.cs：真实模型隔离姿态、截图及中断检查。

## 验证
- Unity 2022.3.62f3 实际编译，新类型加载成功。
- FirstPersonArmChecks.Run 与 LeftHandGestureChecks.Run 通过。
- 张手初始抓握权重 0；第 17 帧开始闭合；第 26 帧闭合完成。
- 本测试站位两掌定位点距离 0.018 米。这是掌心定位点间距，不是皮肤表面的空隙，也不是全站位穿模证明。
- 覆盖左臂单独显示、接触前不握拳、输入交接冻结、快门不跳动画、解除收回、满卷面板收回、仪器切换、重新进入、应用暂停、伸手中取消。
- 测试胶卷仅在内存，清理前解除引用，未改记录；退出 Play 前备份的两个 Editor 存档槽文件哈希检查无变化。
- 初始 Console 存在旧的 `Exception: Pour chemical 1`；本轮没有清空 Console。重新编译后的错误查询为空。
- 全库 diff --check 有现有场景尾随空白；本轮修改脚本的定向检查没有空白错误。

## 视觉证据
output/QA/LeftHandGesture_20261106/reach-10.png：张手伸出。
output/QA/LeftHandGesture_20261106/reach-60.png：轻握接触。
output/QA/LeftHandGesture_20261106/release-16.png：松开收回。
这些是 Unity 真实模型和实际姿态代码的隔离渲染，使用 CPU BakeMesh 固定每帧姿态以避免一次调用多次渲染的蒙皮缓存；不是 ParkScene 实际键鼠流程截图。

## 人工检查（未执行真实鼠标测试）
1. 草地等待少女伸手，点中她的右手：只有左臂张开伸出，接触后握住；右臂不出现。
2. 牵手后拍照：相片使用当时的姿态，不突然把手腕挪到另一处；满卷面板出现后左手松开并收回。
3. 伸手中按 Esc：设置菜单打开，手臂隐藏/动画不继续；恢复后无跳位。
4. 牵手后切到测光表或机身近看，再回取景：无残留手臂，按原牵手状态恢复。
5. 在允许范围内改变站位，观察手指、袖口和掌心是否明显穿插。当前验证只覆盖隔离标准站位，未承诺任意距离的网格碰撞避免。
6. 取消、离开公园、重新读档：胶卷张数与牵手资格仍由原流程决定，其他场景不残留左臂。

## 回退
本轮改动前的同名脚本副本在 output/QA/LeftHandGesture_20261106/before。按本轮差异回退，不能用 git reset 覆盖工作区其它改动。Unity 已停止在夏季卧室的编辑状态，没有保存或替换场景布局。

> 后续修正：少女手指同步改动已按用户要求撤回，玩家侧接触与避让已更新。以 LeftHandContactCorrection-2026-11-06.md 为准。
