using Mirror;

namespace Aoyi.Mirror
{
    /// <summary>
    /// Mirror 桥接消息：承载旧的 MsgFramework 消息二进制数据。
    /// 用于 Plan A：保留旧消息框架，仅把传输层替换为 Mirror。
    /// </summary>
    public struct AoyiRawMessage : NetworkMessage
    {
        public byte[] data;
    }

    /// <summary>
    /// Mirror 登录响应：服务器在 OnServerAddPlayer 时主动推送 tempUserId。
    /// </summary>
    public struct AoyiLoginResponse : NetworkMessage
    {
        public string tempUserId;
        public string playerName;
    }

    /// <summary>
    /// Mirror 匹配成功消息：房主开始战斗时广播给所有客户端。
    /// </summary>
    public struct AoyiMatchSuccess : NetworkMessage
    {
        public string roomId;
        public PlayerData[] playerInfos;
    }
}
