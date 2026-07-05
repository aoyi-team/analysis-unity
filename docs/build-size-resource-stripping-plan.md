# 打包体积与资源治理规划

## 目标

这份规划用于减少 `aoyi team2` Unity 工程的初始包体、降低误打包风险，并让后续新增资源有明确归属。

核心目标不是立刻删除资源，而是建立一套可验证的资源管理流程：

- 不滥用 `Assets/Resources`。
- 大资源逐步迁移到 Addressables 或 AssetBundle。
- 构建前能发现未引用、放错目录、平台不匹配的资源。
- 运行时代码和编辑器代码明确隔离。
- IL2CPP 构建时启用合理的托管代码剥离。

## 当前项目证据

本次检查到的当前状态：

| 项目 | 当前情况 | 影响 |
| --- | --- | --- |
| `Assets/Resources` | Unity 资产报告约 `57` 个资产、约 `8.79 MB`；文件系统含 `.meta` 约 `141` 个文件、约 `8.93 MB` | 里面所有资源都会进 Player 初始包 |
| 已优化资源 | `Assets/Resources/简宋体.ttc` 已移动到 `Assets/Fonts/Source/简宋体.ttc` | `Resources` 体积减少约 `17.46 MB` |
| 已优化资源 | `Assets/Resources/测试3Pro.asset`、`Assets/Resources/测试3.ttf` 已移动到 `Assets/Fonts/TMP` 和 `Assets/Fonts/Source` | `Resources` 体积减少约 `2.04 MB` |
| 当前最大资源 | `Assets/Resources/UISprites/CharacterChoose/SkinPosters/101/04.png` 约 `2.04 MB` | 皮肤海报成为当前最大 Resources 资源 |
| 次大资源 | `Assets/Resources/测试3Pro.asset` 约 `2.01 MB` | 字体资产/字体配置类资源需要重点复核 |
| `UISprites` | 约 `5.31 MB` | 皮肤海报、选角 UI 图适合按需加载 |
| `HeroAnimSprites` | 约 `3.00 MB` | 英雄动画贴图适合按英雄/皮肤拆包 |
| Addressables | `Packages/manifest.json` 未发现 `com.unity.addressables` | 当前还没有 Addressables 工作流 |
| 动态加载入口 | 正式代码主要在 `ResMgr` 和 `SupabaseConfig` 使用 `Resources.Load` | 迁移前要先收口加载接口 |
| asmdef | 当前自有业务代码没有 asmdef，第三方 Mirror 有 asmdef | 后续可给项目运行时代码和编辑器代码拆程序集 |
| Editor 代码 | 自有目录中有 `UnityEditor` 引用，部分已用 `#if UNITY_EDITOR`，但 `BattleManager.cs` 直接 `using UnityEditor` | Player 打包前需要复核，避免运行时程序集引用编辑器 API |
| Managed Stripping | `ProjectSettings` 中部分平台为 `1`，Android 未看到单独配置 | Android/IL2CPP 发布包需要单独设置 Medium/High 后测试 |

## 直接结论

当前最值得优先处理的不是 `Managed Stripping Level`，而是 `Resources` 治理。

原因：`Resources` 里的资源会无条件进入初始包，而本项目已经把字体、UI 面板、角色图、皮肤海报、英雄动画、配置资源都放在了 `Resources` 下。即使代码完全没有引用某个 `Resources` 资源，它也会被打包。

推荐优先级：

1. 先盘点 `Resources` 资源归属和加载路径。
2. 把 `Resources.Load` 封装到统一加载接口，禁止新增散落调用。
3. 引入 Addressables，把 UI、英雄、皮肤、动画资源按组迁移。
4. 再做构建检查脚本、平台过滤、asmdef、Managed Stripping。

## 总体策略

### 1. `Resources` 只保留极少量启动必需资源

长期目标：`Assets/Resources` 只允许保留以下类型：

| 类型 | 是否可留在 Resources | 说明 |
| --- | --- | --- |
| 启动前必须同步读取的小配置 | 可以暂留 | 例如极小的本地默认配置 |
| TextMesh Pro 官方 `Resources` | 可以保留 | TMP 有自己的约定目录，不要随便移动 |
| DOTweenSettings | 可以暂留 | DOTween 自身会使用，后续按插件说明处理 |
| 大字体文件 | 不建议 | `简宋体.ttc` 已移出 Resources；后续源字体继续放 `Assets/Fonts/Source` |
| UI 面板 Prefab | 不建议长期保留 | 可迁移到 Addressables local group |
| 角色贴图/皮肤海报 | 不建议 | 应按英雄/皮肤拆 Addressables group 或远程包 |
| 英雄动画贴图/Prefab/Animator | 不建议 | 应按英雄拆组，首包只带默认英雄或基础资源 |
| 测试资源 | 不允许 | 移到 `_AbandonedLocal/` 或测试专用目录 |

短期不要直接清空 `Resources`。当前 `ResMgr` 依赖这些路径，粗暴移动会导致登录、选角、战斗加载失败。

### 2. 先收口加载代码，再替换底层实现

当前主要入口：

```text
Assets/正式开发项目制作/开发脚本/NetWorkScripts/Manager/ResMgr.cs
Assets/正式开发项目制作/开发脚本/NetWorkScripts/Supabase/SupabaseConfig.cs
```

建议把所有资源加载统一收口到一个运行时服务，例如：

```text
Assets/正式开发项目制作/开发脚本/Resource/AssetLoader.cs
```

第一阶段内部仍然可以调用 `Resources.Load`，但外部代码只允许调用 `AssetLoader` 或 `ResMgr`。这样后续把实现换成 Addressables 时，不需要全项目改业务代码。

禁止新增：

```csharp
Resources.Load(...)
Resources.LoadAsync(...)
Resources.LoadAll(...)
```

允许临时存在：

```csharp
ResMgr.LoadResource<T>(path)
ResMgr.LoadPanelPrefabs(panelName)
AssetLoader.LoadAsync<T>(key)
```

### 3. Addressables 按“功能”和“更新频率”拆组

建议引入包：

```text
com.unity.addressables
```

推荐分组：

| Group | 内容 | 初始包策略 | Label |
| --- | --- | --- | --- |
| `Local_Bootstrap` | 登录、注册、加载页必要 Prefab，小图标 | 可进首包 | `boot`, `ui` |
| `Local_Config` | 模式配置、英雄基础配置 | 可进首包或随版本更新 | `config` |
| `Remote_UI_Posters` | 皮肤海报、选角大图 | 建议远程/按需下载 | `poster`, `hero_101`, `skin` |
| `Remote_Hero_101` | 101 英雄 Prefab、动画贴图、Animator、技能图 | 建议按英雄下载 | `hero_101`, `battle` |
| `Remote_Maps` | 地图大图、地图相关资源 | 按模式/地图下载 | `map`, `mode_dantiao` |
| `ThirdParty_Keep` | 必须保留的第三方运行时资源 | 跟随插件要求 | `third_party` |

首包只应该包含“打开游戏必须出现”的资源。英雄皮肤海报、未进入当前模式的英雄、非当前平台资源，都不应该默认进初始包。

### 4. AssetBundle 可以作为备选，不建议优先手写

Unity Addressables 底层就是基于 AssetBundle 的管理系统，已经包含依赖分析、分组、缓存、远程下载、版本更新等能力。

除非后续有自己的 CDN/热更协议和清单格式，否则本项目建议优先 Addressables，不建议先手写 AssetBundle 管线。

## 分阶段执行计划

### 阶段 0：建立资源规则

目标：先防止问题继续扩大。

立即执行：

1. 新增资源放置规则：大图、皮肤、角色动画、地图资源禁止直接放入 `Assets/Resources`。
2. 新增代码规则：除 `ResMgr`/`AssetLoader` 外，业务代码禁止直接写 `Resources.Load`。
3. 建立资源登记表：

```text
docs/resources-map.md
```

建议字段：

| 字段 | 示例 |
| --- | --- |
| 资源路径 | `Assets/Resources/LoginPanel.prefab` |
| 当前加载 Key | `LoginPanel` |
| 使用位置 | 登录流程 |
| 是否首包必需 | 是/否 |
| 目标方案 | 保留 Resources / Addressables Local / Addressables Remote / 废弃隔离 |
| 风险 | Prefab 引用、代码路径、场景直接引用 |

验收标准：

- 新增 `Resources.Load` 调用必须能被代码搜索发现并解释原因。
- `Resources` 内新增资源必须在 `docs/resources-map.md` 登记。

### 阶段 1：盘点并清理 `Resources` 根目录

目标：优先处理最大、最容易误打包的资源。

当前根目录最大资源：

| 资源 | 当前大小 | 建议 |
| --- | --- | --- |
| `LoginPanel.prefab` / `RegisterPanel.prefab` | 各约 `0.10 MB` | 可先迁移到 Addressables Local |
| `SupabaseConfig.asset` | 很小 | 可暂留，后续改为配置服务或 Addressables Local |

建议动作：

1. `简宋体.ttc` 已确认没有被项目资源引用，并已移动到非 Resources 目录：

```text
Assets/Fonts/Source/
```

2. 如果运行时确实需要完整字体，考虑制作只包含中文常用字和项目文本的 TMP Font Asset，减少体积。
3. `测试3Pro.asset`、`测试3.ttf` 已移出 Resources，但仍需后续评估是否重新生成更小的 TMP 字体资产。

验收标准：

- 移动字体后，登录、注册、选角、战斗 UI 文本无 Missing Font。
- Unity Console 无字体材质、TMP fallback 错误。

### 阶段 2：迁移 UI 面板和 UI 图

目标：让 UI 资源不再依赖 `Resources` 全量打包。

优先迁移：

```text
Assets/Resources/LoginPanel.prefab
Assets/Resources/RegisterPanel.prefab
Assets/Resources/LoadCanvas.prefab
Assets/Resources/GameLoadPanel.prefab
Assets/Resources/LoadAnimPanel.prefab
Assets/Resources/UISprites/CharacterChoose
Assets/Resources/GameUISprites
Assets/Resources/GameCommandBtnSprites
```

迁移方式：

1. 安装 Addressables。
2. 把登录/注册/加载页放入 `Local_Bootstrap`。
3. 把选角皮肤海报放入 `Remote_UI_Posters`。
4. 给资源加 Label：`ui`、`poster`、`hero_101`、`boot`。
5. 修改 `ResMgr.LoadPanelPrefabs` 和皮肤海报加载逻辑，支持 Addressables 异步加载。

推荐加载接口：

```csharp
public static IEnumerator LoadAssetAsync<T>(string key, Action<T> onLoaded) where T : UnityEngine.Object;
public static void Release(object handleOrAsset);
```

注意：Addressables 迁移后要处理释放。皮肤海报这种大图不能只 `Clear` 字典，还要释放 Addressables handle。

验收标准：

- 登录、注册、加载页可以正常打开。
- 选角页切换皮肤海报无卡死、无 Missing Sprite。
- 重复打开/关闭选角页后内存不会持续上涨。

### 阶段 3：按英雄拆分战斗资源

目标：首包不携带所有英雄和所有皮肤资源。

优先迁移：

```text
Assets/Resources/HeroConfigs
Assets/Resources/HeroPrefabs
Assets/Resources/HeroAnimSprites
Assets/Resources/Animator/Hero
Assets/Resources/Animations/Hero
Assets/Resources/SkillSprites
Assets/Resources/HeroSmallHeads
```

建议拆分方式：

| 资源类型 | 分组方式 | 加载时机 |
| --- | --- | --- |
| 英雄配置 | `Local_Config` 或 `Remote_Hero_101` | 登录后/进入选角前 |
| 英雄小头像 | `Local_Config` 或 `Remote_UI_Posters` | 选角页打开前 |
| 皮肤海报 | `Remote_UI_Posters` | 选中英雄后按需加载 |
| 战斗 Prefab | `Remote_Hero_101` | 进入战斗前预加载 |
| 英雄动画贴图 | `Remote_Hero_101` | 进入战斗前预加载 |
| Animator Controller | 跟随英雄组 | 进入战斗前预加载 |

验收标准：

- 从大厅进入选角时，只加载当前需要展示的英雄 UI 资源。
- 从选角进入战斗时，先完成当前英雄战斗资源预加载，再进入场景。
- 断网或远程资源加载失败时，有明确错误提示和回退逻辑。

### 阶段 4：建立构建前资源检查脚本

目标：构建前自动发现明显错误。

建议新增 Editor 工具：

```text
Assets/Editor/BuildValidation/ResourceBuildValidator.cs
```

检查项：

1. 扫描 `Assets/Resources` 总大小，超过阈值则构建失败。
2. 扫描 `Resources` 中是否出现禁止类型，例如大图、测试资源、未登记资源。
3. 使用 `AssetDatabase.GetDependencies` 分析 Build Settings 场景依赖。
4. 生成“被场景/Prefab 依赖”和“只在 Resources/Addressables 中动态加载”的资源报告。
5. 检查业务代码中是否新增散落的 `Resources.Load`。

输出建议：

```text
Library/BuildReports/resources-report.json
Library/BuildReports/resources-report.md
```

注意：`AssetDatabase.GetDependencies` 只能分析静态引用，无法自动发现字符串动态加载，例如：

```csharp
Resources.Load($"UISprites/CharacterChoose/SkinPosters/{heroId}/{numStr}")
```

所以动态资源必须依赖 `docs/resources-map.md` 或 Addressables label 清单补充。

验收标准：

- 构建前能看到 `Resources` 总大小和 Top 20 大资源。
- 未登记的 `Resources` 新资源会被报告出来。
- 构建报告能区分“静态依赖资源”和“动态加载资源”。

### 阶段 5：平台过滤和导入设置

目标：减少不同平台携带不合适的资源和插件。

需要区分两类设置：

| 类型 | 作用 | 说明 |
| --- | --- | --- |
| Texture/Audio Importer Platform Overrides | 控制压缩格式、尺寸、质量 | 通常不决定资源是否打包，只影响该平台下的导入结果 |
| Plugin Import Settings | 控制 DLL/Native Plugin 支持平台 | 可以决定插件是否进入某个平台 Player |
| Addressables Group/Label | 控制资源分组和下载策略 | 推荐用于按平台/按功能控制资源进入首包或远程包 |
| 自定义构建脚本 | 构建前移动、过滤、校验资源 | 适合做强约束，但要谨慎 |

建议：

1. 对图片设置平台压缩：Android 使用 ASTC/ETC2，Standalone 使用合适的压缩格式。
2. 对插件检查 `Select platforms for plugin`，不用的平台取消勾选。
3. Addressables 按平台建 Group 或 Profile，例如：

```text
Android_Remote_Heroes
Standalone_Remote_Heroes
```

4. 不建议靠手动 Inspector 设置来“保证资源不打包”。资源是否进入包体，应以 Build Report/Addressables Analyze 为准。

验收标准：

- Android 图集压缩格式正确。
- 不属于 Android 的 Native Plugin 不进入 Android 包。
- Addressables 构建报告能看到各组大小。

### 阶段 6：托管代码剥离和 Preserve 策略

目标：发布包剥离未使用托管代码，但不破坏反射、序列化和网络协议。

建议配置：

```text
Edit -> Project Settings -> Player -> Other Settings -> Optimization -> Managed Stripping Level
```

推荐策略：

| 构建类型 | Backend | Managed Stripping Level | 说明 |
| --- | --- | --- | --- |
| Editor/开发调试 | Mono/IL2CPP | Low | 方便定位问题 |
| Android 测试包 | IL2CPP | Medium | 先跑完整主流程 |
| Android 正式包 | IL2CPP | Medium 或 High | High 需要更完整测试 |
| Standalone 调试 | Mono | Low/Medium | 看发布目标决定 |

容易被剥离影响的内容：

- 通过反射调用的类和方法。
- JSON 反序列化只靠字符串字段名访问的类型。
- 网络协议消息类型。
- UnityEvent 或第三方 SDK 内部动态调用。
- Supabase/REST DTO 类型。

保留方式：

```csharp
using UnityEngine.Scripting;

[Preserve]
public class SomeDto
{
}
```

或者创建：

```text
Assets/link.xml
```

验收标准：

- Android IL2CPP Medium 可以完成登录、注册、选角、进入战斗。
- High 级别若出现异常，先补 `[Preserve]` 或 `link.xml`，不要直接回退到完全不剥离。

### 阶段 7：asmdef 和 Editor 目录隔离

目标：运行时代码不编译进编辑器专用 API，减少编译范围和误引用。

建议长期结构：

```text
Assets/正式开发项目制作/开发脚本/
  Runtime/
    Aoyi.Runtime.asmdef
  Editor/
    Aoyi.Editor.asmdef
```

`Aoyi.Editor.asmdef` 只勾选：

```text
Editor
```

近期必须检查：

```text
Assets/正式开发项目制作/开发脚本/Battle/Managers/BattleManager.cs
```

它当前直接出现 `using UnityEditor;`。如果该脚本是运行时战斗管理器，应该移除该引用，或把编辑器逻辑包进：

```csharp
#if UNITY_EDITOR
using UnityEditor;
#endif
```

更好的方式是把编辑器功能拆到 `Editor/` 目录。

验收标准：

- Player 构建不依赖 `UnityEditor`。
- 自有业务代码至少拆出 Runtime 和 Editor 两个 asmdef。
- 编辑器工具代码无法被运行时程序集引用。

## 推荐近期执行顺序

### 第一批：低风险、收益明确

1. 新增 `docs/resources-map.md`，登记当前 `Resources`。
2. 在 `ResMgr` 旁边建立加载接口约定，禁止新增散落 `Resources.Load`。
3. 继续评估是否把 `测试3Pro.asset` 重新生成为更小的 TMP 字体资产。
4. 修复或隔离自有代码中的 `UnityEditor` 运行时引用。
5. 建立构建前 `Resources` 大小报告工具。

### 第二批：引入 Addressables

1. 安装 `com.unity.addressables`。
2. 先迁移 `LoginPanel`、`RegisterPanel`、`LoadCanvas` 到 `Local_Bootstrap`。
3. 修改 `ResMgr.LoadPanelPrefabs` 支持异步加载。
4. 再迁移 `UISprites/CharacterChoose/SkinPosters` 到 `Remote_UI_Posters`。
5. 加入 Addressables Analyze 和 Build Report 检查。

### 第三批：英雄和战斗资源拆包

1. 按英雄 ID 建 Addressables group，例如 `Remote_Hero_101`。
2. 进入战斗前预加载英雄 Prefab、Animator、Animation、Sprite。
3. 战斗结束后释放 handle。
4. 测试重复进入/退出战斗的内存变化。

### 第四批：发布包优化

1. Android 切 IL2CPP。
2. Managed Stripping Level 设为 Medium。
3. 跑完整主流程。
4. 补 `[Preserve]` 或 `link.xml`。
5. 再评估 High。

## 不建议现在做的事

1. 不建议直接删除 `Assets/Resources` 里看似未引用的资源。
2. 不建议一次性把所有资源迁移到 Addressables。
3. 不建议先开 High Stripping 再猜哪里坏了。
4. 不建议把字体、皮肤、英雄资源同时迁移，问题会很难定位。
5. 不建议只看 Inspector 平台设置就认定资源不会打进包。

## 完成标准

这套资源治理完成后，项目应满足：

- `Assets/Resources` 总大小有上限，并且每个资源都有登记原因。
- 大字体、皮肤海报、英雄动画不再无条件进入首包。
- Addressables 分组能解释“哪些资源首包带、哪些资源按需下载”。
- 构建前脚本能输出资源体积报告和异常资源列表。
- 运行时代码没有直接依赖 `UnityEditor`。
- Android IL2CPP Medium 至少能跑通登录、注册、选角、战斗主流程。
- High Stripping 的保留项通过 `[Preserve]` 或 `link.xml` 明确记录。
