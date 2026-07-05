using System.Collections;

/// <summary>
/// 战斗加载命令接口（命令模式）。
/// 参考 Flash 奥拉星 LoadSceneCommand 链设计：
/// 将 LoadAllManagers 中的耦合步骤拆解为独立命令，
/// 支持排序、跳过、按模式替换步骤，无需改动 BattleManager。
/// </summary>
public interface ILoadCommand
{
    /// <summary>命令名称，用于日志和调试。</summary>
    string Name { get; }

    /// <summary>执行命令。返回 null 表示同步完成；返回 IEnumerator 表示异步步骤。</summary>
    IEnumerator Execute(LoadContext ctx);
}
