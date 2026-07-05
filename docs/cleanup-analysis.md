# 清理分析与空间收益估算

## 结论

本次分析只做静态检查，没有删除任何文件。

本文里的“清理”不是直接删除资源，而是把候选资源移动到本地废弃隔离目录，并确保该目录不会上传到 GitHub。

推荐废弃目录：

```text
_AbandonedLocal/
```

该目录已规划为本地保留目录，应被 `.gitignore` 排除。移动到这里的文件仍然留在本机，后续确认无误后再决定是否真正删除。

当前项目里确实有一批高概率无用资源，主要集中在示例场景、第三方插件 Demo、字体包文档和测试场景。按磁盘占用估算：

- 第一批低风险清理：约 `10.71 MB`
- 第二批 Photon 示例清理：约 `29.80 MB`
- 两批都完成：约 `40.51 MB`
- 当前 `Assets` 目录约 `552.66 MB`，两批完成后预计降到约 `512.15 MB`

这里的“内存小多少”按工程磁盘体积估算，不等同于运行时内存。运行时内存需要用 Unity Profiler 在实际场景中测量。

## 检查方法

本次使用两类判断：

1. Unity GUID 静态引用扫描
   - 扫描 `.unity`、`.prefab`、`.asset`、`.controller`、`.anim`、`.mat` 等文件中的 GUID 引用。
   - 结果显示：`2998` 个带 GUID 的资源中，有 `625` 个没有被 Unity 序列化资源直接引用。

2. 动态加载风险排除
   - `Assets/Resources` 下很多资源不会通过 GUID 直接引用，而是通过 `Resources.Load`、`Resources.LoadAsync` 或 `Resources.LoadAll` 加载。
   - 因此 `Resources` 下的资源不能只凭“未被 GUID 引用”删除。

## 不建议直接删除的误报

以下资源虽然在 GUID 扫描里可能显示未引用，但当前正式代码会动态加载，不建议清理：

| 路径示例 | 使用原因 |
| --- | --- |
| `Assets/Resources/LoginPanel.prefab` | `ResMgr.LoadPanelPrefabs("LoginPanel")` |
| `Assets/Resources/RegisterPanel.prefab` | 注册面板动态加载 |
| `Assets/Resources/LoadAnimPanel.prefab` | 加载动画面板动态加载 |
| `Assets/Resources/UploadNamePanel.prefab` | 上传昵称面板动态加载 |
| `Assets/Resources/GameLoadPanel.prefab` | 选角/战斗加载面板动态加载 |
| `Assets/Resources/HeroConfigs/Hero_101.asset` | `_playerInfo` 按英雄 ID 动态加载 |
| `Assets/Resources/HeroPrefabs/101/101.prefab` | `BattleResourceManager` 按英雄 ID 动态加载 |
| `Assets/Resources/ModeConfigs/dantiao_ModeConfig.asset` | `BattleManager` 按游戏模式动态加载 |
| `Assets/Resources/UISprites/CharacterChoose/...` | 选角界面按路径动态加载 |

## 第一批：低风险清理候选

这一批主要是示例、文档、测试场景。建议先移动到 `_AbandonedLocal/` 这类本地废弃目录中验证，不直接删除。

| 候选项 | 路径 | 估算大小 | 文件数 | 判断 |
| --- | --- | ---: | ---: | --- |
| TextMesh Pro 示例 | `Assets/TextMesh Pro/Examples & Extras` | `3.38 MB` | `250` | 官方示例资源，非项目主流程 |
| Black/Grey 字体包示例 | `Assets/BlackBold + GreyBold Font Pack` | `0.10 MB` | `19` | 示例字体包和 Readme |
| 字体文档 | `Assets/Fonts/Documentation` | `<0.01 MB` | `5` | 文档类文件 |
| Lecompte 字体样例 | `Assets/Fonts/Lecompte` | `1.28 MB` | `25` | 样例、raw、截图较多 |
| 字体测试场景 | `Assets/Fonts/_Scene` | `0.02 MB` | `5` | 不在 Build Settings |
| 字体测试包 | `Assets/Fonts/artianniugbbd_test` | `0.48 MB` | `5` | 测试字体资源 |
| Thaleah 示例场景 | `Assets/Thaleah_PixelFont/Thaleah_Demo.unity` | `0.01 MB` | `2` | 示例场景 |
| Thaleah Readme | `Assets/Thaleah_PixelFont/Thaleah_Readme.pdf` | `0.04 MB` | `2` | 文档文件 |
| NewTest 测试场景 | `Assets/Scenes/NewTest` | `0.14 MB` | `3` | 不在 Build Settings |
| Photon PDF 文档 | `Assets/Photon/PhotonNetworking-Documentation.pdf` | `3.85 MB` | `2` | 文档文件 |
| Photon CHM 文档 | `Assets/Photon/PhotonNetworking-Documentation.chm` | `1.39 MB` | `2` | 文档文件 |

第一批合计预计减少：`10.71 MB`。

清完第一批后：

- `Assets` 目录预计从 `552.66 MB` 降到约 `541.95 MB`
- 约减少 `1.94%`

## 第二批：需要先处理脚本引用的候选

Photon 示例目录体积更大，但不能直接一刀删除。当前旧脚本里有如下引用：

- `using Photon.Pun.Demo.Asteroids`
- `using Photon.Pun.Demo.Cockpit`
- `using Photon.Pun.Demo.PunBasics`

这些引用出现在生日、魔王模式、活动角色等旧脚本里。即使实际没有用到 Demo 类型，只要删除 Demo 命名空间，Unity 编译也可能报错。

| 候选项 | 路径 | 估算大小 | 文件数 | 清理前置条件 |
| --- | --- | ---: | ---: | --- |
| Photon Chat Demos | `Assets/Photon/PhotonChat/Demos` | `0.83 MB` | `43` | 确认没有场景和脚本依赖 |
| Photon Realtime Demos | `Assets/Photon/PhotonRealtime/Demos` | `0.35 MB` | `8` | 确认没有场景和脚本依赖 |
| Photon PUN Demos | `Assets/Photon/PhotonUnityNetworking/Demos` | `28.62 MB` | `616` | 先移除旧脚本里的 Demo namespace 引用 |

第二批合计预计减少：`29.80 MB`。

第一批 + 第二批都清完后：

- `Assets` 目录预计从 `552.66 MB` 降到约 `512.15 MB`
- 约减少 `7.33%`

## 本地缓存目录

以下目录不是项目资源，属于 Unity/IDE 本地生成内容。清理它们能释放大量磁盘空间，但 Unity 打开项目后会重新生成一部分。

| 目录 | 当前大小 | 说明 |
| --- | ---: | --- |
| `Library` | `6355.52 MB` | Unity 导入缓存，删除后会全量重新导入 |
| `.vs` | `21.79 MB` | Visual Studio 本地缓存 |
| `Temp` | `5.27 MB` | Unity 临时文件 |
| `obj` | `1.46 MB` | C# 中间输出 |
| `Logs` | `0.28 MB` | Unity 日志 |
| `UserSettings` | `0.08 MB` | 本机编辑器设置 |

如果只想临时释放磁盘空间，清 `Library` 的收益最大，约 `6.21 GB`。但这不是资源瘦身，下一次打开 Unity 会花时间重新导入资源，并重新生成大量缓存。

## 清理完成的判断标准

清理不能只看文件被删掉，还要确认项目仍然可用。建议按下面标准验收：

1. Unity 打开项目后 Console 没有编译错误。
2. Build Settings 中启用的场景仍然存在：
   - `LoadScene`
   - `RegiserScene`
   - `LobbyPanel`
   - `paiwei_map`
   - `dantiao_map`
3. 主流程能跑通：
   - 启动加载页
   - 登录/注册面板正常显示
   - 大厅正常进入
   - 选角/模式入口正常
   - 战斗地图能加载
4. 场景和 Prefab 没有明显 `Missing Script`、`Missing Prefab`、`Missing Sprite`、`Missing Material`。
5. 如果清理 Photon Demo，旧脚本中的 `Photon.Pun.Demo.*` 引用已经移除或替换。
6. 重新计算 `Assets` 目录大小，确认空间下降接近预估值：
   - 只清第一批：减少约 `10.71 MB`
   - 清第一批 + Photon Demo：减少约 `40.51 MB`
7. `_AbandonedLocal/` 不出现在 `git status` 的待提交列表中。

## 推荐执行顺序

1. 先把第一批低风险示例/文档资源移动到 `_AbandonedLocal/`。
2. 打开 Unity，确认没有编译错误和 Missing 引用。
3. 再处理旧脚本里的 Photon Demo namespace 引用。
4. 把 Photon Demo 目录移动到 `_AbandonedLocal/`。
5. 再次打开 Unity，跑主流程验证。
6. 最后再考虑是否清理 `Library` 这类本地缓存目录。

## 最终判断

从资源瘦身角度看，当前真正值得优先清理的是示例和文档，不是正式资源。能稳定减少的项目资源体积大约是 `10.71 MB`；如果把 Photon Demo 也处理干净，总计大约能减少 `40.51 MB`。

如果目标是释放磁盘空间，`Library` 才是大头，单独约 `6.21 GB`。但它是缓存，不是项目无用资源，清理后会重新生成。
