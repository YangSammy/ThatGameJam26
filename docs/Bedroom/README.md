# 四季卧室 V17

此目录说明第 17 轮四季卧室的 Unity 运行场景及其依赖。它是可第一人称查看的卧室美术场景，**不是完整五章游戏**。本轮完成后停止继续迭代，不代表视觉目标已经达成。

## 工程与目录

使用 **Unity 2022.3.62f3 / Built-in Render Pipeline**。保留完整四墙与天花板，室内尺寸约 4.3 × 5.4 × 2.85 m；床到书桌的空间保持开放，中央圆毯、朝窗的纸笔、床内有厚度的被子、素色床品和木本色椅子作为本轮基线保留。

| 内容 | 仓库路径 |
| --- | --- |
| 模型、材质、纹理等美术依赖 | `Assets/Art/Environments/House/Bedroom/V17/` |
| 四季场景和对应烘焙 GI 数据 | `Assets/Scenes/Bedroom/V17/` |
| 场景逻辑脚本 | `Assets/Scripts/House/Bedroom/` |
| 第一人称脚本 | `Assets/Scripts/Player/Bedroom/` |
| 检查与捕获脚本 | `Assets/Scripts/Diagnostics/Bedroom/` |
| 模型与 UV 导入配置 | `Assets/Editor/House/BedroomModelUV.cs` |
| V17 打开、校验与独立构建工具 | `Assets/Editor/Build/BedroomV17Tools.cs` |
| 第三方作者、来源与许可记录 | [ThirdParty/README.md](ThirdParty/README.md) |

场景名保留为 `Bedroom_Spring`、`Bedroom_Summer`、`Bedroom_Autumn`、`Bedroom_Winter`。直接打开 `Assets/Scenes/Bedroom/V17/Bedroom_Spring.unity` 后进入 Play Mode，可用数字键切换季节；四个场景须同时包含在独立查看器的构建场景列表中。

`Tools > Bedroom V17` 提供打开春季、检查四季引用和构建 Windows 查看器的菜单。构建菜单只选这四个场景，使用当前工程的颜色空间和质量设置，需要交互选择输出目录；不会运行旧的全量生成器。批处理校验入口为 `BedroomV17Tools.ValidateScenes`。

**颜色空间差异：**专用卧室生产工程使用 Linear（`m_ActiveColorSpace: 1`），目标仓库当前使用 Gamma（`m_ActiveColorSpace: 0`）。本次导入不自动修改全项目颜色空间。材质、曝光和烘焙结果在目标工程中可能与专用工程截图有差异，需以目标仓库的实际运行结果确认，不能沿用旧截图宣称一致。

## 操作

以下按 `BedroomWalk` 的实际 HUD 与按键实现记录。

| 输入 | 行为 |
| --- | --- |
| WASD / 鼠标 | 移动 / 观察 |
| C | 按住蹲下 |
| 左 Shift | 按住加快行走 |
| 1 / 2 / 3 / 4 | 春 / 夏 / 秋 / 冬 |
| Esc | 切换鼠标锁定；释放后单击可重新锁定 |
| H | 显示 / 隐藏帮助 |

## 本次 Git 范围

仅收录四季 Unity 运行场景及必要依赖，沿用仓库普通 Git 规则，本次不引入 Git LFS。保留 Unity `.meta` 文件及 GUID 对应关系。

约 296 MB 的 Blender 编辑源、超过 100 MiB 的 GLB 和交付 ZIP 保留在本地交付，不纳入本次运行场景提交。专用工程的 `Library`、缓存、参考图、商店宣传图、下载缓存、未导入资产包以及旧的全量 `BedroomBuild` 工具也不属于本次范围。本次不覆盖目标工程全部 `ProjectSettings`。

## 验证状态与已知差距

原专用卧室工程的第 17 轮已有实际 Cycles 渲染、Unity 原生运行捕获、移动通路检查和独立视觉评审记录；这些是**原工程的证据**，不能替代此次目标仓库导入后的检查。

已在独立 Unity 工程中通过 `AssetDatabase.MoveAsset` 完成迁移，并实际打开四季场景：缺失脚本、材质和网格均为 0，每季 1 张烘焙灯图引用有效。12,305 个运行资源文件的 SHA256 与 v17 原件逐项相同，详见 [integration-verification.json](integration-verification.json)。目标仓库已用 Unity 2022.3.62f3 完成首次导入、脚本编译与四季场景引用校验，进程返回 0；四季检查均通过，原 Gamma 设置保持。本次未重新运行或构建目标查看器，未作五章完整游戏测试。

衣物已包含独立实体布面和肩袖拼接，但局部领肩仍偏硬，领口有深腔感、领片偏厚，部分袖身连接仍有管状感，细缝线连续性和布料垂坠感尚未达到现实衣物目标；没有采用完整布料动力学模拟。已有 lightmap UV 重叠警告仍需保留并继续评估。既有检查范围有限，不能据此保证所有角度、路径、硬件或全部几何均无问题。

房间内的相片 / 装饰画图集及部分基色纹理由原创图像生成流程制作，用作实际 3D 对象表面；场景本身为真实 3D，并非生成房间图片。第三方 CC0 只适用于对应来源资产，不是对整个仓库的统一授权；详见随附来源清单。
