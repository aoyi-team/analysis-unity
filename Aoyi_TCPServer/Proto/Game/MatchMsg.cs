using MsgFramework;
using System.Collections.Generic;

//玩家缓存的选择,null则检索自带userId
public class PlayerChooseCache
{
    public int userId;
    public int selectedHeroId;     // 选中的英雄ID

}

//退出匹配请求
public class MsgExitRequest:MsgBase
{
    public MsgExitRequest() { protoName = "MsgExitRequest"; }
    public GameModes mode;
    public List<int> PlayerList;
}

// 2. 新增匹配请求协议（客户端+服务端共用）
public class MsgMatchRequest : MsgBase
{
    public MsgMatchRequest() { protoName = "MsgMatchRequest"; }
    public GameModes GameModes { get; set; }
    public List<PlayerChooseCache> playerPack;
    //public string userId;          // 用户ID
    //public int selectedHeroId;     // 选中的英雄ID
}
// 新增匹配成功协议（客户端+服务端共用）
public class MsgMatchSuccess : MsgBase
{
    public MsgMatchSuccess() { protoName = "MsgMatchSuccess"; }
    public string roomId;           // 房间ID
    public List<PlayerData> playerInfos;
}