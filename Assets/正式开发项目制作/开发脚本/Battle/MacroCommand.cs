using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 宏命令：顺序执行一组子命令。
/// 每执行完一个子命令后自动 yield return null 插入一帧延迟，
/// 防止初始化步骤堆积在同一帧导致卡顿。
/// </summary>
public class MacroCommand : ILoadCommand
{
    public string Name { get; private set; }
    private readonly List<ILoadCommand> _commands;
    private readonly int _commandsPerFrame;

    public MacroCommand(string name, int commandsPerFrame = 1)
    {
        Name = name;
        _commands = new List<ILoadCommand>();
        _commandsPerFrame = Mathf.Max(1, commandsPerFrame);
    }

    public MacroCommand Add(ILoadCommand command)
    {
        _commands.Add(command);
        return this;
    }

    public MacroCommand AddRange(params ILoadCommand[] commands)
    {
        _commands.AddRange(commands);
        return this;
    }

    public IEnumerator Execute(LoadContext ctx)
    {
        int count = 0;
        foreach (var cmd in _commands)
        {
            Debug.Log($"[MacroCommand] {Name} → {cmd.Name}");

            var step = cmd.Execute(ctx);
            if (step != null)
            {
                while (step.MoveNext())
                {
                    yield return step.Current;
                }
            }

            count++;
            if (count >= _commandsPerFrame)
            {
                count = 0;
                yield return null;
            }
        }
    }
}
