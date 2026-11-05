# 暗室全流程与独立构建验收 · 2026-11-05

结论：完整暗室控制器流程、Windows x64开发构建、独立进程运行及重启持久化通过。全程原生鼠标拖拽/长按和不受遮挡的暗室最终画面仍未完成验收；本报告不宣称原生输入全流程通过。

## 构建

- Unity2022.3.62f3，Windows x86_64，Mono，Development，与现有GameBuild默认配置一致；包含当前全部六个启用场景，没有删减场景或关闭检查以换取成功。
- 从当前工作目录复制Assets/Packages/ProjectSettings至`D:/GameJams/PhotoAcceptance-20261105`，未修改共享Unity实例的Play、场景或存档。公园与摄影源码逐文件哈希一致，见`source-comparison.json`。
- 只在隔离副本将公司/产品名改为`MemoryWhiteboxQA/PhotographyAcceptance20261105`，保证独立存档；增加仅开发构建可用、必须指定命令行参数才启动的验收器。未改生产玩法代码。
- BuildPipeline.BuildPlayer：Succeeded，0 errors、1 warning，166.05秒，2,645,166,917字节。警告来自现有`Hidden/Post FX/Screen Space Reflection`的isnan提示；无着色器编译错误。
- 包目录：`output/QA/2026-11-05-DarkroomBuild/Windows64`；入口`MemoryGame.exe`，须保留同目录Data、MonoBleedingEdge及DLL。
- EXE SHA256：`76A4560E40EC43334F5300F52D45E74F55579709D5EA01EB838F24C7AC9E568B`。它只标识入口文件，非整包校验。

## 完整暗室交互

在真实Windows运行包中创建开发测试满卷，调用玩家所用的Press/Move/Release/Scroll、物理接收区及TickDirectDarkroom，执行现有ContinuousDarkroomChecks的全部35个检查：

1. 回卷后开背、将曝光胶卷拖入暗袋、转动装罐并封盖。
2. 显影/停显/定影依次倒入，搅动、等待、要求倒液；每道药液在待倒液状态保存、重新加载并退出/重入操作，仍需正确倒液后才能前进。
3. 水洗后开罐，底片挂夹干燥；纸和底片放入接触印相框、合盖、长按曝光、开盖取纸。
4. 实体纸依次进入显影、停显、定影、水洗槽，开水龙头，挂夹干燥。
5. 成品才可查看；保存并重载后仍可查看。

没有直接赋值推进材料阶段。测试使用加速的计时器dt；满卷为开发快捷生成的三张480×270占位底片，未冒充公园玩家真实照片。

## 跨进程恢复

首个运行进程完成印相后退出；启动第二个独立进程，读取同一验收存档。两个进程均报告`PHOTO_ACCEPTANCE_PASS`，且无Exception/NullReference/保存失败/底片不可用日志。

重启后确认：3张，workflow3，printStage Complete，胶卷Developed。分别打开第1、2、3张，纹理均480×270且底片可读，关闭/重新打开图册、保存成功。最终磁盘状态selectedFrame=1，未创建替代胶卷。

## 原生画面与限制

额外不带验收参数启动，可见Windows窗口成功显示房间，使用D3D11与RTX4060 Laptop GPU。Windows网络权限询问遮挡了窗口；未授予网络权限，工具未能直接操作该系统对话框。未将其视为玩法失败，也未将被遮挡的尝试记为原生暗室验收成功。测试进程已关闭。

隐藏窗口自动运行生成的`player-final.png`为黑帧，不能作为视觉通过证据；重启截图同样不用于视觉验收。编辑器批处理验收器先前有启动等待/截图等待问题，相关失败日志已保留；完整流程的通过证据来自独立运行包，而非这些失败尝试。

本次未验证发布构建、其它GPU/分辨率/系统、全程原生鼠标拖拽与长按、正常故事从春季玩至夏季的入场链路，以及真实公园拍摄内容的最终印相外观。

## 证据与复现

所有文件位于`output/QA/2026-11-05-DarkroomBuild`：

- `build-summary.txt`、`build.log`：构建结果及完整日志。
- `full-flow-checks.txt`：35个流程检查。
- `player-flow.txt`、`player.log`：首进程结果。
- `player-reload-flow.txt`、`player-reload.log`：第二进程结果。
- `Harness/`：隔离副本专用运行器与构建器，未加入正式Assets。

初次运行：`MemoryGame.exe --photo-acceptance -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile <独立日志路径>`。仅针对新建的验收产品存档；对已完成胶卷不要再次运行全流程用例。重启验证使用`--photo-reload`。两者均会运行完自动退出。无参数是正常开发包，数字2为现有夏季暗室开发入口。

验收存档：`C:/Users/Chu/AppData/LocalLow/MemoryWhiteboxQA/PhotographyAcceptance20261105`。共享项目与正式玩家存档未重置。
