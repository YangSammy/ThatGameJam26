# 项目目录与场景命名规范

适用于 HouseScene、ParkScene 以及后续新增内容。采用项目内一致的规则，不强行改动导入模型内部名称或既有序列化接口。

## 场景层级

按职责建立顶层分组；没有内容的分组不创建。

| 分组 | 内容 |
|---|---|
| Systems | 场景总控制器 |
| Player | 玩家、主摄像机、第一人称手与附属摄像机 |
| Environment | 房屋、公园环境、环境碰撞 |
| Gameplay | 同伴与检视展示库 |
| Anchors | 玩家出生点、操作镜头、同伴路线及观察目标 |
| Lighting | 全局灯光 |
| Legacy | 仍有引用的旧设备或旧环境，不等于可以删除 |

分组 Transform 保持位置与旋转为零、缩放为一。不要用组织分组整体缩放或摆放场景。重设父级时保留世界坐标；不要改变物体启用状态。

新节点使用 `用途_对象_编号或方向`，例如 `Camera_Player`、`Spawn_Player`、`View_RadioOperation`、`Waypoint_07_Waterside`、`Collision_BridgeApproach_West`。序号用两位数字。玩家摄像机继续保留 MainCamera 标签，名称不决定其功能。

书房定位入口：`Anchors/Spawn_Player`、`Anchors/View_RadioOperation`。
公园定位入口：`Anchors/Anchors_SpawnAndLook` 与 `Anchors/Route_Companion`。

不要随意重命名或调整以下内部层级：
- 导入 FBX、Animator 下的骨骼和部件：动画绑定使用相对路径。
- 主房间已移除旧磁带设备；玩家摄像机和手部由主控制器直接引用。
- `Study entry props`、`Radio - tape chapter`、`Cassettes - tape chapter`、`Safe inspection - authored display models` 等：既有工具按名字查找。
- 检视展示副本与源物体的对应内部路径。

公园 V03、V04、V05 目前共同组成现有环境，并非仅凭版本号就能删除的重复资产。V02 保留在兼容分组且维持原有禁用状态。

## 资源目录

```text
Assets/
  Scenes/                     HouseScene、ParkScene
  Scripts/
    Core/                     主流程、章节基类
    Player/                   3C、输入、第一人称手、镜头定位
    Persistence/              共用存档路径
    Interaction/Inspection/   统一检视
    House/                    书房环境与入口
    Park/                     公园拍摄、同伴、胶卷
    Chapters/                 Tape、Photography、Writing、Painting、Ending
    Diagnostics/              运行时诊断支持
  Editor/
    Build/                    构建工具
    Inspection/               检视接入与设置
    Tape/                     收音机制作工具
    Park/                     公园工具
    Player/                   玩家配置工具
    Persistence/              Editor 存档策略
    Validation/               检查工具
  Art/
    Environments/House/Materials/
    Environments/Park/        保留已有版本与导入资源结构
    Props/Radio/              Models、Materials、Textures、Animations、Audio
    Interaction/Inspection/   检视反馈资源
    Gameplay/ParkPhotography/ Materials、Audio、Collision
    Characters/Player/Materials/ 第一人称手部材质
  Resources/                  仅保留动态加载及既有着色器资源
```

脚本文件名与类型名保持一致，partial 文件采用 `类型名.职责.cs`。目录按职责划分，暂不更改命名空间、程序集和序列化字段。
新增独立资源使用 PascalCase；方向使用 West/East/Left/Right 等语义名。导入模型的部件名、动画路径与已有动态加载键优先保持兼容。

## 移动资源

通过 Unity Project 窗口或 AssetDatabase.MoveAsset 移动，保留 GUID 和 `.meta`。每次同时更新工具中的固定路径；不要只在资源管理器移动资源而遗漏 `.meta`。
HouseScene、ParkScene 文件名与 Resources 内部加载路径本次保持不变。根目录 output 属于制作源文件及输出，不随运行资源目录移动；旧制作脚本重新使用前应按此目录更新导出目标。

本次整理不修改关卡逻辑与场景摆放。组织分组是编辑辅助，不是自动重建关卡的入口。
