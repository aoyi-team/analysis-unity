# 项目结构整理规划

## 目标

本规划用于整理 `aoyi team2` Unity 工程，让项目更适合长期维护和上传 GitHub。当前目标不是马上大规模移动资源，而是先把正式代码、旧代码、第三方插件、服务端和文档边界分清。

本文里的“清理”不等于删除。清理的默认动作是：把废弃候选资源移动到本地废弃隔离目录，并确保这个目录不会被 GitHub 上传。

推荐本地废弃目录：

```text
_AbandonedLocal/
```

整理完成后的状态应该是：

- 新开发知道代码应该放哪里。
- 旧资源不会继续污染正式目录。
- Unity 主流程不因为移动资源而丢引用。
- GitHub 仓库不包含 Unity 缓存和 IDE 生成文件。
- 服务端和 Unity 客户端边界清楚。

## 当前结构问题

### 1. 正式代码和旧代码混在一起

当前正式代码主要在：

```text
Assets/正式开发项目制作/开发脚本
```

但旧玩法、活动脚本和测试脚本还分布在：

```text
Assets/脚本文件
Assets/存储资源夹
Assets/2025生日用素材
Assets/UI及功能性脚本
Assets/PlayerTestController.cs
Assets/FuncTestSt.cs
```

这些旧目录可能仍有资源引用，不能直接删除，但需要标记状态。

### 2. `Resources` 目录承担了太多动态加载职责

当前正式代码大量依赖 `Resources.Load`、`Resources.LoadAsync`、`Resources.LoadAll`。

典型路径：

```text
Assets/Resources/LoginPanel.prefab
Assets/Resources/RegisterPanel.prefab
Assets/Resources/LoadAnimPanel.prefab
Assets/Resources/HeroConfigs/Hero_101.asset
Assets/Resources/HeroPrefabs/101/101.prefab
Assets/Resources/ModeConfigs/dantiao_ModeConfig.asset
Assets/Resources/UISprites/CharacterChoose
```

这些资源不能只凭 GUID 未引用就删除，也不适合立刻大规模移动。

### 3. 第三方插件和示例资源混杂

项目里存在：

```text
Assets/Photon
Assets/Plugins/Demigiant/DOTween
Assets/TextMesh Pro
Assets/Thaleah_PixelFont
Assets/BlackBold + GreyBold Font Pack
Assets/Fonts
```

其中 Photon Demo、TextMesh Pro Examples、字体示例资源大概率不是正式主流程需要的内容，但 Photon Demo 命名空间目前被部分旧脚本引用，清理前要先处理引用。

### 4. 服务端在根目录，但边界还不够清楚

当前服务端目录：

```text
Aoyi_TCPServer
```

它是配套服务端源码，应该保留进仓库。但 `bin`、`obj`、`.vs`、`packages` 等生成目录不应该提交。

### 5. 中文目录较多，跨工具时不够稳定

Unity 可以正常使用中文路径，但 Git、CI、脚本、第三方工具在处理中文路径时更容易出现统计、编码或转义问题。现阶段不建议全量改名，但新增目录建议使用英文。

## 整理原则

1. 先标记，再移动到本地废弃目录，最后再决定是否删除。
2. 移动 Unity 资源时优先在 Unity Editor 内操作，保留 `.meta` 和 GUID。
3. 不在第一阶段移动 `Resources` 下的正式运行资源。
4. 不在第一阶段删除旧玩法资源，只做归档和说明。
5. 不把 `Library`、`Temp`、`Logs`、`.vs`、`obj`、`bin`、`_AbandonedLocal` 提交到 GitHub。
6. 每一批整理后都要打开 Unity 验证 Console、场景和 Prefab 引用。

## 目标结构

长期目标可以向下面结构收敛：

```text
Assets/
  _Project/
    Scripts/
      Runtime/
        Battle/
        Network/
        Login/
        Lobby/
        UI/
        Config/
        Common/
      Editor/
    Resources/
      Runtime/
      UI/
      Hero/
      Mode/
    Scenes/
    Prefabs/
    Art/
      Characters/
      UI/
      Maps/
      Effects/
    Audio/
  _Legacy/
    OldScripts/
    OldPrefabs/
    Events2025/
  ThirdParty/
    Photon/
    DOTween/
    Fonts/
  TextMesh Pro/
Packages/
ProjectSettings/
Server/
  Aoyi_TCPServer/
docs/
```

注意：这是长期目标，不建议一次性把现有目录全部搬成这样。

## 分阶段计划

### 阶段 0：GitHub 发布前准备

状态：已基本完成。

需要保留：

```text
Assets
Packages
ProjectSettings
Aoyi_TCPServer
docs
.gitignore
.gitattributes
```

需要忽略：

```text
Library
Temp
Logs
UserSettings
.vs
.plastic
obj
bin
Aoyi_TCPServer/packages
```

验收标准：

- `git status` 里不出现 Unity 缓存目录。
- 没有超过 GitHub 100MB 限制的单文件。
- `.meta` 文件随资源一起提交。

### 阶段 1：建立状态标记

目标：先让团队知道哪些目录是正式主线，哪些是旧资源。

建议新增文档：

```text
docs/asset-ownership.md
```

建议标记：

| 区域 | 状态 | 处理方式 |
| --- | --- | --- |
| `Assets/正式开发项目制作/开发脚本` | 正式开发中 | 保持主线 |
| `Assets/Resources` | 正式运行依赖 | 暂不移动 |
| `Assets/Scenes` | 正式场景 + 部分测试场景 | 先按 Build Settings 标记 |
| `Assets/脚本文件` | 旧脚本 | 标为 Legacy |
| `Assets/存储资源夹` | 旧资源/魔王模式/通用资源 | 标为 Legacy 或 Candidate |
| `Assets/2025生日用素材` | 活动资源 | 标为 Archive |
| `Assets/Photon/*/Demos` | 第三方示例 | 标为 Cleanup Candidate |
| `Assets/TextMesh Pro/Examples & Extras` | 第三方示例 | 标为 Cleanup Candidate |

验收标准：

- 每个大目录都有状态说明。
- 新功能不再写入旧脚本目录。

### 阶段 2：移动低风险示例和文档到本地废弃目录

参考：

```text
docs/cleanup-analysis.md
```

优先移动到 `_AbandonedLocal/`：

```text
Assets/TextMesh Pro/Examples & Extras
Assets/BlackBold + GreyBold Font Pack
Assets/Fonts/Documentation
Assets/Fonts/Lecompte
Assets/Fonts/_Scene
Assets/Fonts/artianniugbbd_test
Assets/Thaleah_PixelFont/Thaleah_Demo.unity
Assets/Thaleah_PixelFont/Thaleah_Readme.pdf
Assets/Scenes/NewTest
Assets/Photon/PhotonNetworking-Documentation.pdf
Assets/Photon/PhotonNetworking-Documentation.chm
```

GitHub 上传收益：

- 第一批低风险清理约减少 `10.71 MB`。

验收标准：

- Unity Console 无编译错误。
- 登录、大厅、选角、战斗地图可打开。
- 无明显 Missing Script、Missing Sprite、Missing Material。

### 阶段 3：处理 Photon Demo 依赖并移动到本地废弃目录

当前旧脚本里存在：

```csharp
using Photon.Pun.Demo.Asteroids;
using Photon.Pun.Demo.Cockpit;
using Photon.Pun.Demo.PunBasics;
```

处理顺序：

1. 搜索所有 `Photon.Pun.Demo` 引用。
2. 判断是否真的使用 Demo 类型。
3. 未使用的 `using` 直接移除。
4. 使用了 Demo 类型的脚本，替换成项目自己的类型或移入 Legacy。
5. 再把 Photon Demo 目录移动到 `_AbandonedLocal/`。

预计收益：

- Photon Demo 约减少 `29.80 MB`。

验收标准：

- 删除 Photon Demo 后 Unity 无编译错误。
- 正式网络链路仍使用自定义 TCP/UDP，不依赖 Photon Demo。

### 阶段 4：整理正式代码目录

不建议立刻移动所有脚本。先从新增代码开始使用新的分层。

建议新代码进入：

```text
Assets/正式开发项目制作/开发脚本/Battle
Assets/正式开发项目制作/开发脚本/NetWorkScripts
Assets/正式开发项目制作/开发脚本/LobbyScripts
Assets/正式开发项目制作/开发脚本/CharactersChosePages
```

当主流程稳定后，再迁移到英文目标结构：

```text
Assets/_Project/Scripts/Runtime/Battle
Assets/_Project/Scripts/Runtime/Network
Assets/_Project/Scripts/Runtime/Login
Assets/_Project/Scripts/Runtime/Lobby
Assets/_Project/Scripts/Runtime/UI
Assets/_Project/Scripts/Runtime/Config
Assets/_Project/Scripts/Editor
```

重点处理：

- 把 `UnityEditor` 引用放入 `Editor` 目录或 `#if UNITY_EDITOR`。
- 把运行时单例管理器集中。
- 把协议、网络、UI、战斗逻辑边界分清。

验收标准：

- 打包 Player 时不因为 `UnityEditor` 引用失败。
- 代码目录能从名字看出职责。
- 新脚本不再散落在 Assets 根目录。

### 阶段 5：服务端目录整理

建议长期结构：

```text
Server/
  Aoyi_TCPServer/
    Aoyi_TCPServer.csproj
    Program.cs
    Net/
    Proto/
    MySQLController/
```

迁移前提：

- Git 状态干净。
- 服务端工程路径变更后可以正常打开。
- Unity 客户端没有硬编码依赖服务端项目路径。

验收标准：

- 服务端源码可编译。
- `bin`、`obj`、`.vs`、`packages` 不进入 Git。
- README 说明服务端启动方式。

### 阶段 6：资源系统升级

短期继续使用 `Resources`，但要先建立资源清单。

建议新增：

```text
docs/resources-map.md
```

内容包括：

- 面板 Prefab 路径。
- 英雄配置路径。
- 英雄 Prefab 路径。
- 模式配置路径。
- UI 图集路径。
- 哪些资源由代码动态加载。

长期可以考虑：

- Addressables
- ScriptableObject 注册表
- 资源路径常量化
- 构建时资源校验工具

验收标准：

- 新增动态加载资源时必须更新资源清单。
- 删除资源前能查到是否被代码路径引用。

## 不建议现在做的事

以下动作风险较高，建议等主流程稳定后再做：

1. 全量英文重命名所有中文目录。
2. 直接删除 `Assets/Resources` 中未被 GUID 引用的资源。
3. 直接删除整个 `Assets/Photon`。
4. 一次性移动所有 Prefab、Animator、材质、贴图。
5. 在没打开 Unity 验证的情况下批量删除 `.meta`。

## 推荐下一步

1. 保持当前 GitHub 发布准备。
2. 建立 `docs/asset-ownership.md`，给大目录标状态。
3. 清理第一批低风险示例资源。
4. 修复 Photon Demo namespace 引用。
5. 再决定是否把 Photon Demo 移动到 `_AbandonedLocal/`。
6. 主流程稳定后，再开始正式目录迁移。

## 完成标准

项目结构整理完成至少要满足：

- GitHub 仓库只包含源码、资源、配置和文档。
- Unity 打开无编译错误。
- Build Settings 中启用的场景都能打开。
- 登录、大厅、选角、战斗地图主流程能跑通。
- 新增代码有明确归属目录。
- 旧资源有 Legacy 或 Archive 标记。
- 移动到本地废弃目录的资源体积和风险记录在 docs 中。
- `_AbandonedLocal/` 不会出现在 GitHub 仓库中。
