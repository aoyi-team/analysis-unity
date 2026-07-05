using System.Collections;

public class CmdInitUDPSocket : ILoadCommand
{
    public string Name => "InitUDPSocket";

    public IEnumerator Execute(LoadContext ctx)
    {
        UDPSocketManager.Instance.InitUDPSocket();
        UDPSocketManager.Instance.Handle = ctx.UdpMessageHandler;
        yield break;
    }
}
