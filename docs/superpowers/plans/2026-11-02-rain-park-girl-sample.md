# Rain 公园少女首轮样板 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** 完成 Rain 源文件审计、可编辑少女样板、FBX、独立 Prefab 和 Unity 实际验证，不替换既有角色。

**Architecture:** 源文件在 output/Characters/ParkGirl；游戏资产在 Assets/Art/Characters/ParkGirl。Rain 制作绑定保留，独立导出骨架与必要修形负责游戏表现。专用检查场景不包含游戏存档或关卡脚本。

**Tech Stack:** 兼容 Rain v3 的 Blender、Python、Unity 2022.3.62f3 Built-in、Editor C#。

**Spec:** docs/superpowers/specs/2026-11-02-rain-park-girl-design.md

## Global Constraints

- 保留 HouseScene、ParkScene、当前人物、固定锚点、存档、输入与渲染管线。
- 本轮范围 A/B/C；细节纹理与完整动作属于后续阶段。
- 在当前已连接的项目里新增独立目录，避免工作树拷贝与 Unity Editor 指向不同项目。
- 不自动提交用户当前 dev-Nicole 分支的其他变更。
- 用户已批准方案并要求执行，本次不重复请求方案批准。

## Review Focus

- 原场景或设置被意外保存：比较受保护文件 SHA256。
- 原绑定含驱动器与非导出修改器：审计清单和 Blender/Unity 实测。
- FBX 丢失左右、单位或根稳定性：实际 bounds、标尺与多帧骨骼记录。
- 遮脸只在单帧成立：环绕多视角与整段动作逐帧接触表。
- 动作与修形未真正播放：Unity 实际 Play 骨骼与形态权重采样。

## Task 1 源文件审计

- [x] 下载官方原始包，记录来源、版本、SHA256、CC BY 4.0 和内嵌脚本声明。
- [x] 使用独立后台兼容 Blender，默认禁用自动运行；输出对象、蒙皮、约束、驱动器、形态键与修改器 JSON。
- [x] 检查内嵌脚本后再执行必要绑定注册；验证 IK/FK 与修形求值。
- [x] 报告保留、烘焙、替换、删除清单和版本差异。

## Task 2 少女灰模与导出

- [x] Scripts/audit_rain.py、build_sample.py 只操作官方源副本与本任务目录。
- [x] 基于 Rain 制作 1.62 m 少女、实体半束发、完整衣着和独立 FaceGuard；保留源 rig。
- [x] 制作 10 秒转头抬臂伸手动作、独立导出骨架、必要修形及 FBX。
- [x] 输出可编辑 blend、默认状态和转台预览、统计、动作参数与第三方说明。
- [ ] 外观验收未完成：前侧发束过厚、服装接缝粗糙，仍需精修。

## Task 3 Unity 独立验证

- [x] Editor/ParkGirlSampleBuild.cs 负责导入设置、材质、Prefab 与专用场景，禁止打开保存原场景。
- [x] 使用真实导入网格和骨骼检查单位、轴向、蒙皮、动画和 BlendShape。
- [x] 专用场景播放完整动作，记录多帧采样、Console 与真实渲染预览。
- [x] 恢复原打开场景，核对受保护文件、检查新资产引用。
- [x] Reports/validation.md 分别列出通过、失败和未测事项；如有美术局限明确标注，不把技术样板称为最终人物。

## 执行记录

- 2026-11-02：Unity 已连接本项目，HouseScene 未修改、非 Play。Console 基线为一条 MCP WebSocket 警告。已保存受保护文件与工作区基线。
- 决策：在独立任务目录执行，无需创建新 worktree 或切换当前分支；现有 Editor 与大量其他工作保持一致。
- 决策：使用兼容的便携 Blender 后台进程，不改变用户当前 Blender 3.6 会话或系统默认安装。

## 最终记录

技术样板与 Unity 导入/Play 证据已产出，B 阶段外观验收未完成。Rain 源颈肩局部残差 9.27 mm（3 mm 阈值 FAIL）；当前 Blender 样板到 Unity 抽样最大误差 0.04773 mm（PASS）。失败与未测项目完整记录在 `output/Characters/ParkGirl/Reports/Validation.md`。

独立审阅发现全局保存风险，已改为本样板目录定向保存，重新编译与 Build 通过。次要待改：多场景 active/unloaded 状态不能完整恢复；本次单一 HouseScene 已恢复且干净。五个受保护文件哈希一致。

范围决定：四面部标记的 301 帧几何检查及 66 张多视角图只作首轮证据，不据此判定全脸、正式摄影机和全部过渡安全。完整脸部遮挡、正式接触点、所有 Rain 修改器、动画混合/中断、性能和 Player 构建留待后续。这会增加正式接入前的验收工作。未自动提交当前用户工作区。
