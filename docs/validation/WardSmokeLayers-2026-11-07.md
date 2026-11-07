# 双层融雪与烟雾 — 2026-11-07

在现有单 Pass Shader 内合成两层，未引入插件或替换病房图。

底层保留固定融雪噪声；第二层加入宽范围软过渡、低频滚动噪声、冷灰阴影和细颗粒。烟雾仅在融雪边缘带活动。鼻梁截止和面部遮罩在显现混合后应用；阴影与颗粒也受保护区域和前景纸面约束。

运行时 WardImageReveal 通过 Time.time 向材质传入 _SmokeTime，停步仍有烟雾运动；Unity timeScale 为零时停动。底层进度不回退，上层可以局部变亮或变暗，不再要求合成后的每个像素单调变化。

材质参数：SmokeStrength=0.72、SmokeWidth=0.18、SmokeScale=2.2、SmokeSpeed=0.35、SmokeShadow=0.65、SmokeGrain=0.10。SmokeStrength=0 可恢复仅薄雪的效果。

验证：底层 41 帧 GPU 单调显现检查通过；叠层 243 帧覆盖 16:9、20:9、4:3，在滚动和固定进度阶段检查鼻梁以上不露出；固定进度第31与49帧不同，证明噪声继续运动。Shader 编译检查与改动文件 diff whitespace 检查通过。预览 output/QA/WardSmokeLayers_20261107/WardSmokeLayers.gif 包含中途停步示意，为离屏 Shader 渲染，非完整关卡实玩。未构建 Player。
