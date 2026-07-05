using System.Collections;

public class CmdInitCameraManager : ILoadCommand
{
    public string Name => "InitCameraManager";

    public IEnumerator Execute(LoadContext ctx)
    {
        CameraManager.Instance.Init();
        yield break;
    }
}
