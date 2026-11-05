# 病房柔和显影预览

## 第三版：融雪

最新预览使用 `Reveal style = SnowMelt`。白色覆盖层在多个不规则缺口处消融，缺口扩大、连接，留下缩小的白色残块；总体由下向上推进。采用固定细胞距离场与两层纹理噪声混合，避免均匀圆孔。没有下落粒子，纸张保持可见，眼部保护独立固定。

参数：`Snow patch scale = 8`、`Snow melt spread = 0.58`、`Snow edge softness = 0.035`。核心边缘外叠加较宽的低强度柔化。切回 `Reveal style = Flow` 可恢复第二版流动效果，原有参数保留。

视频 `output/WardRevealPreview/WardSnowMelt-v3.mp4` 为 15 秒、30fps Unity Shader 实际渲染。记录在 `output/WardRevealPreview/snow-v3/validation.txt`：初始白场、前景、由下向上显现、显现单调性、重复性、三种比例及全部 450 帧眼部保护通过。独立 Unity 批处理退出码 0。仍是独立效果预览，未连接游戏切场逻辑。

## 第二版：更宽柔化与边缘流动

按最新反馈，羽化宽度由 0.045 增至 0.12，不规则幅度由 0.07 增至 0.17，纹理尺度由 7 降至 4.3，形成更大的局部起伏。新增 `Edge flow strength`（默认 0.8），通过显现进度驱动缓慢的坐标扰动；同一进度仍可精确复现，运动过程中边缘形状会逐渐变化。脸部保护范围独立于流动，保持固定。

新版视频：`output/WardRevealPreview/WardSoftReveal-v2.mp4`。新版实际 GPU 检查仍全部通过，包括 450 帧眼部遮挡、显现单调性、比例适配和同进度可重复性；记录在 `output/WardRevealPreview/v2/validation.txt`。预览场景和材质已更新，原版视频保留用于对照。本次修改未改变自动播放组件，未重复完整游戏流程验收。

## 使用

打开 `Assets/Art/Environments/WardPreview/WardSoftRevealPreview.unity`，按 Play。
也可以使用菜单 `Memory Whitebox > Winter > Ward Soft Reveal > Open Preview`。

默认循环：暖白场停留 1 秒，自动显现 12 秒，末帧停留 2 秒。
播放器不读取角色输入，不调用关卡存档。桌面和纸从开始一直可见。当前使用完整概念图，纸上的男生是原图中的静态形象。

视频：`output/WardRevealPreview/WardSoftReveal.mp4`，1280×720、30fps、15 秒。
视频由 Unity 实际 Shader 的 450 帧 GPU 渲染编码，非生成式示意图，也非完整游戏实玩录屏。
另有 `playmode-final.png`，通过实际 Play Mode 自动播放和 Camera.OnRenderImage 截取。

## 调整

相机上的 `WardSoftRevealPreview`：Duration 控制显现秒数；Opening Hold、Ending Hold 控制停留；Loop 控制循环。

`WardSoftReveal.mat`：
- Soft edge width：柔化宽度。
- Edge irregularity / Noise scale：边缘不规则幅度和纹理尺度。
- Edge flow strength：显现过程中局部流动的强度，使用进度驱动，不读取独立的全局时间。
- Warm white：遮罩颜色。
- Final reveal height：最终显现高度，默认 0.70，保留鼻点上方白场。
- Protected face：UV 中心和椭圆半径，中心 `(0.496,0.78)`，半径 `(0.065,0.092)`；核心完全不透明，外侧羽化。
- Face mask feather：保护区域外侧柔化。

材质在运行时复制一份，停止后释放；运行前调整材质，再 Play 查看。当前脸部与桌面遮罩按这张概念图的 UV 校准，更换构图时需要重新校准。没有额外的显现顺序贴图，也没有额外噪声贴图。

## 验证

Unity 2022.3.62f3 Built-in，独立项目 `D:/GameJams/WardRevealPreview-20261105`，D3D11 / RTX 4060 Laptop。

- C#、Shader 编译通过，批处理退出码 0。
- 初始暖白、前景保留、下方优先显现通过。
- 六个阶段及全部 450 帧的双眼区域像素保护通过。
- 显现累计单调、不反向消失通过。
- 同一进度两次 GPU 渲染逐像素相同。
- 16:9、20:9、4:3 保持原图比例并补暖白边。
- Play Mode 无输入自动推进至 0.98；实际相机后处理截图符合预期。
- 独立代码复核未发现重要缺陷，GUID 引用匹配。

主项目仅添加此预览目录；没有替换冬日关卡流程、鼻点切场或存档逻辑，没有在共享编辑器里切场景或停止 Play。尚未进行主项目完整构建或游戏流程验收。
