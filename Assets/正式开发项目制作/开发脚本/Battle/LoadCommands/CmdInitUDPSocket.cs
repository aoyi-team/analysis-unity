using System.Collections;
using Aoyi.Mirror;

public class CmdInitUDPSocket : ILoadCommand
{
    public string Name => "InitUDPSocket";

    public IEnumerator Execute(LoadContext ctx)
    {
        if (MirrorNetBridge.IsMirrorActive)
        {
            UnityEngine.Debug.Log("[CmdInitUDPSocket] Mirror 已激活，跳过旧 UDP Socket 初始化");
            yield break;
        }

        UDPSocketManager.Instance.InitUDPSocket();
        UDPSocketManager.Instance.Handle = ctx.UdpMessageHandler;
        yield break;
    }
}
