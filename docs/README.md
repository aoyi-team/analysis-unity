# 项目文档

这个目录用于记录 `aoyi team2` Unity 项目的结构、主流程和维护建议。

## 文档索引

- [project-analysis.md](project-analysis.md): 当前项目分析，包括技术栈、目录结构、运行链路、资源组织、风险点和后续整理建议。
- [cleanup-analysis.md](cleanup-analysis.md): 无用资源清理候选、清理风险、验收标准和空间收益估算。
- [structure-organization-plan.md](structure-organization-plan.md): 项目结构整理规划，包括目标结构、分阶段迁移方案和验收标准。
- [build-size-resource-stripping-plan.md](build-size-resource-stripping-plan.md): 打包体积、Resources、Addressables、平台过滤、Managed Stripping 和 asmdef/Editor 隔离规划。
- [resources-map.md](resources-map.md): `Assets/Resources` 当前资源登记、动态加载路径、迁移优先级和移动前检查清单。

## 当前结论

项目是一个 Unity 2022.3 LTS 2D 多人对战 Demo，正式开发代码主要集中在 `Assets/正式开发项目制作/开发脚本`。当前代码架构已经开始向“登录/大厅/选角/战斗”分层，战斗部分使用 TCP/UDP 自定义协议、定点数逻辑、帧同步和自定义 2D 碰撞框架。

项目内还保留了大量旧玩法脚本、活动资源、Photon 示例和 TextMesh Pro 示例。后续开发建议优先围绕正式开发目录收束主流程，再逐步清理或归档历史资源。
