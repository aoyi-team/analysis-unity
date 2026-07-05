# Resources 资源登记表

## 用途

本文件用于登记当前 `Assets/Resources` 中的运行时资源、加载方式和后续迁移方向。

规则：

- 新增资源不要直接放进 `Assets/Resources`，除非它是启动必需的小配置。
- 新增动态加载不要直接写 `Resources.Load`，统一走 `ResMgr` 或后续的 `AssetLoader`。
- 删除或移动资源前，先确认静态引用、运行时代码路径和 Addressables 迁移状态。
- `Resources` 资源即使没有被场景或 Prefab 引用，也会进入 Player 初始包。

## 当前总体情况

本次盘点结果：

| 项目 | 数值 |
| --- | --- |
| `Assets/Resources` Unity 资产数 | 约 `57` 个 |
| `Assets/Resources` Unity 资产总大小 | 约 `8.79 MB` |
| `Assets/Resources` 文件系统数量 | 含 `.meta` 约 `141` 个文件，约 `8.93 MB` |
| 最大文件 | `Assets/Resources/UISprites/CharacterChoose/SkinPosters/101/04.png`，约 `2.04 MB` |
| 主要动态加载入口 | `ResMgr.cs`、`SupabaseConfig.cs` |
| 当前 Addressables 状态 | 尚未引入 `com.unity.addressables` |

## 目录级登记

| 路径 | 当前内容 | 当前加载方式 | 首包必需 | 后续处理 |
| --- | --- | --- | --- | --- |
| `Assets/Resources` 根目录 | 字体、TMP/字体资产、登录/注册/加载 Prefab、SupabaseConfig、DOTweenSettings | `Resources.Load`、插件默认约定、Prefab 直接路径 | 部分必需 | 优先复核大字体和测试命名资源；UI Prefab 后续迁移到 Addressables Local |
| `Assets/Resources/UISprites` | 选角 UI、模式图标、皮肤海报 | `ResMgr.PreLoadModesIcons`、`ResMgr.PreLoadHeroSkinDic` | 否，除基础图标外 | 皮肤海报迁移到 `Remote_UI_Posters`，模式图标可进 `Local_Bootstrap` |
| `Assets/Resources/HeroAnimSprites` | 101 英雄动作、普攻、技能、奥义图片 | Prefab/Animator/代码间接依赖 | 否 | 按英雄迁移到 `Remote_Hero_101` |
| `Assets/Resources/HeroPrefabs` | 101 英雄战斗 Prefab | `Resources.Load` 或战斗流程间接引用 | 否 | 进入战斗前按英雄预加载 |
| `Assets/Resources/HeroConfigs` | 英雄 ScriptableObject 配置 | `ResMgr.LoadResource<T>` 或配置加载 | 可能 | 小配置可先保留，后续进 `Local_Config` |
| `Assets/Resources/ModeConfigs` | 模式配置 | 配置加载 | 可能 | 可进 `Local_Config` |
| `Assets/Resources/Animator` | UI 和英雄 Animator Controller | Prefab/运行时依赖 | 部分 | 英雄 Animator 跟随英雄组，UI Animator 跟随 UI 组 |
| `Assets/Resources/Animations` | UI 和英雄 AnimationClip | Animator/Prefab 引用 | 部分 | 英雄动画跟随英雄组，UI 动画跟随 UI 组 |
| `Assets/Resources/MapSprits` | 地图相关 UI/图片 | 运行时或 Prefab 引用 | 否 | 后续按地图/模式拆到 `Remote_Maps` |
| `Assets/Resources/SkillSprites` | 英雄技能图 | 运行时 UI/战斗依赖 | 否 | 跟随英雄组或战斗 UI 组 |
| `Assets/Resources/HeroSmallHeads` | 英雄小头像 | 选角/大厅 UI | 可能 | 少量可本地，后续按英雄 Label 管理 |
| `Assets/Resources/GameUISprites` | 战斗 UI 图片 | UI 引用 | 可能 | 进 `Local_Bootstrap` 或战斗 UI 组 |
| `Assets/Resources/GameCommandBtnSprites` | 战斗命令按钮图片 | 战斗 UI 引用 | 可能 | 进战斗 UI 组 |

## Top 大资源登记

| 资源 | 大小 | 当前判断 | 建议 |
| --- | --- | --- | --- |
| `Assets/Resources/UISprites/CharacterChoose/SkinPosters/101/04.png` | 约 `2.04 MB` | 皮肤海报 | 迁移到 `Remote_UI_Posters` |
| `Assets/Resources/HeroAnimSprites/101/01/诺亚奥义图(800,800).png` | 约 `1.85 MB` | 英雄奥义图 | 迁移到 `Remote_Hero_101` |
| `Assets/Resources/UISprites/CharacterChoose/SkinPosters/101/02.png` | 约 `1.18 MB` | 皮肤海报 | 迁移到 `Remote_UI_Posters` |
| `Assets/Resources/UISprites/CharacterChoose/SkinPosters/101/01.png` | 约 `1.02 MB` | 皮肤海报 | 迁移到 `Remote_UI_Posters` |
| `Assets/Resources/UISprites/CharacterChoose/SkinPosters/101/03.png` | 约 `0.69 MB` | 皮肤海报 | 迁移到 `Remote_UI_Posters` |

## 已知运行时加载路径

来自当前代码搜索：

| 代码位置 | 加载路径 | 资源类型 | 备注 |
| --- | --- | --- | --- |
| `ResMgr.LoadPanelPrefabs(string PanelName)` | `PanelName` | 面板 Prefab | 登录、注册、加载等 UI 面板可能依赖 |
| `ResMgr.PreLoadModesIcons()` | `UISprites/CharacterChoose/ModesIcon/ModesIcons` | 模式图标 Sprite[] | 使用 `Resources.LoadAll<Sprite>` |
| `ResMgr.PreLoadHeroSkinDic(int heroId, int skinCounts)` | `UISprites/CharacterChoose/SkinPosters/{heroId}/{numStr}` | 皮肤海报 | 字符串拼接，静态依赖扫描无法发现 |
| `ResMgr.PreLoadHeroSkinDic(int heroId, int skinCounts)` | `UISprites/CharacterChoose/SkinPosters/{heroId}/{numStr}_bg` | 皮肤海报背景 | 字符串拼接，静态依赖扫描无法发现 |
| `ResMgr.LoadResource<T>(string path)` | `path` | 任意资源 | 需要调用方登记具体 path |
| `SupabaseConfig.Instance` | `SupabaseConfig` | Supabase 配置 | 找不到时会创建内存默认实例 |

## 迁移优先级

### P0：先登记，不移动

- `SupabaseConfig.asset`
- `DOTweenSettings.asset`
- 当前登录/注册/加载 UI Prefab
- 英雄配置、模式配置

### P1：优先复核大资源

- `测试3Pro.asset`
- `测试3.ttf`
- 皮肤海报大图
- 101 英雄奥义大图

## 已处理记录

| 日期 | 资源 | 动作 | 结果 |
| --- | --- | --- | --- |
| 2026-07-03 | `Assets/Resources/简宋体.ttc` | 移动到 `Assets/Fonts/Source/简宋体.ttc`，保留原 `.meta` GUID | `Resources` 从约 `28.29 MB` 降到约 `10.83 MB` |
| 2026-07-03 | `Assets/Resources/测试3Pro.asset`、`Assets/Resources/测试3.ttf` | 分别移动到 `Assets/Fonts/TMP/测试3Pro.asset` 和 `Assets/Fonts/Source/测试3.ttf`，保留原 `.meta` GUID | `Resources` 从约 `10.83 MB` 降到约 `8.79 MB` |

### P2：适合 Addressables Local

- 登录/注册/加载页 Prefab
- 基础 UI 小图标
- 模式图标
- 小型配置资源

### P3：适合 Addressables Remote

- 皮肤海报
- 英雄动画贴图
- 英雄战斗 Prefab
- 英雄技能图
- 地图大图

## 删除/移动前检查清单

移动任何 `Resources` 资源前，至少检查：

1. 是否在场景、Prefab、Animator、ScriptableObject 中有静态引用。
2. 是否被 `Resources.Load`、`Resources.LoadAsync`、`Resources.LoadAll` 动态路径加载。
3. 是否被字符串拼接路径加载。
4. 是否是 TMP、DOTween 或第三方插件的固定约定资源。
5. 是否需要保留 `.meta`，并通过 Unity Editor 移动以保持 GUID。
6. 移动后是否跑过登录、注册、选角、战斗主流程。

## 后续维护规则

- 每次新增 `Resources` 资源，都在本文件补一行登记。
- 每次新增 `Resources.Load` 调用，都说明为什么暂时不能使用 Addressables。
- 当资源迁移到 Addressables 后，把“后续处理”更新为对应 Group/Label。
- 只凭 GUID 未引用不能判定资源无用；运行时字符串加载必须人工确认。
