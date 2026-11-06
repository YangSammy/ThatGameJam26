# 左手接触修正 — 2026-11-06

用户约束：保持少女原来的伸手动作和位置；只让玩家左手伸向少女实际手掌并握住，减少手部穿插。

## 已核实
上一轮没有修改少女上臂/前臂 IK、定位点或场景位置，但修改了手指闭合时序和满卷暂停时的表现。本轮已将 ParkGirlPresentation.cs 恢复为 output/QA/LeftHandGesture_20261106/before/ParkGirlPresentation.cs 的原始内容，逐文件比较一致。没有用 Git HEAD 覆盖更早的“不低头”等既有修改。
上一轮截图为正面隔离站位；本轮另按 ParkScene 草地 referenceView、handContact 与响应朝向的相对几何进行隔离渲染。没有改 ParkScene。

## 本轮修改
- FirstPersonHands.Handhold.cs：只读取少女当前掌心/方向；加入皮肤厚度间隔，调整伸手弧线；指节和拇指弯曲遇接触限制。
- FirstPersonHands.HandClearance.cs：少女手掌及各指节的简化保护体，只约束玩家手指和玩家手臂显示根，不创建场景碰撞体、不修改少女骨骼、不修改玩家移动坐标。
- FirstPersonHands.Skeletal.cs：姿态完成后执行玩家皮肤间隔修正。
- LeftHandGestureChecks.cs：逐帧检查少女掌心/手腕/上臂不被玩家动作改变；检查手指保护体间距，增加草地相对机位测试。

## 检查
Unity 编译成功。正面与草地相对机位的 61 帧伸手/握持序列通过手指保护体检查；输入交接、快门不跳帧、收回、仪器切换、重进和取消检查通过。牵手时保持原左臂网格，未改人物尺寸或骨长。测试是隔离模型调用，不是实际键鼠游玩，也不代表全部站位的精确三角网格碰撞验证。
在验证中发现当前编辑器切到 MainMenuScene 的 Play、timeScale=0；隔离测试仅在同一同步调用内临时使用 timeScale=1，finally 恢复原值，没有停止该 Play 或改变其场景。
没有改存档数据或摄影判定。

## 证据
- output/QA/LeftHandContactCorrection_20261106/meadow/reach-60.png
- output/QA/LeftHandContactCorrection_20261106/meadow/checks.txt
- output/QA/LeftHandContactCorrection_20261106/front/checks.txt

人工复核：草地点击少女右手，确认她仍使用原动作；玩家左手张开、接触、握住；试稍近/稍远站位；拍完或退出时左手收回且无右臂残留。未做真实鼠标测试。
