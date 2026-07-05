using System.Collections;

public class CmdInitInputManager : ILoadCommand
{
    public string Name => "InitInputManager";

    public IEnumerator Execute(LoadContext ctx)
    {
        InputManager.Instance.Init();
        yield break;
    }
}
