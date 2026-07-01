//匹配请求协议（客户端+服务端共用）
using MsgFramework;
using System.Collections.Generic;

//存放玩家选择(后续包括存邀请的玩家的选择)
public class PlayerChooseCache
{
    public int userId;
    public int selectedHeroId;     // 选中的英雄ID
    //public int selectedHeroSkinId; // 皮肤对应ID

}

public class MsgMatchRequest : MsgBase
{
    public MsgMatchRequest() { protoName = "MsgMatchRequest"; }
    public GameModes GameModes { get; set; }
    public List<PlayerChooseCache> playerPack; 
    //public string userId;          // 用户ID
    //public int selectedHeroId;     // 选中的英雄ID
}
// 匹配成功协议（客户端+服务端共用）
public class MsgMatchSuccess : MsgBase
{
    public MsgMatchSuccess() { protoName = "MsgMatchSuccess"; }
    public string roomId;           // 房间ID
    public List<PlayerData> playerInfos;
}
//退出匹配请求
public class MsgExitRequest : MsgBase
{
    public MsgExitRequest() { protoName = "MsgExitRequest"; }
    public GameModes mode;
    public List<int> PlayerList;
}