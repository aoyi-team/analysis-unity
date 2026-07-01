// 操作类型,客户端需要同步的所有行为
using MsgFramework;
using System;
using System.Collections.Generic;


public enum ActionCode
{
    Attack = 0,
    Skill = 1
}
public enum AnimState
{
    up = 0,
    side = 1,
    down = 2
}

//玩家信息自定义类(后续优化？，，和PlayerCache好像冲突了)
public struct PlayerData
{

    public int teamId;
    public string userId;
    public int HeroId;
}

// 学院技能子类型
/*public enum SchoolSkillType
{
    Flash = 1,         // 闪现
    Heal = 2,          // 治疗
    Dash = 3           // 冲刺
}*/

// 玩家单帧操作协议（客户端→服务端）
public class MsgPlayerOp : MsgBase
{
    public MsgPlayerOp() { protoName = "MsgPlayerOp"; }
    public string roomId;       // 房间ID（分流核心，必填）
    public int teamId;          // 队伍ID,区分伤害.
    public int playerId;        // 玩家ID（校验核心，必填）
    public int frameId;         // 客户端帧号（校验用）
    public int moveDirX;
    public int moveDirY;
    public bool isMoving;

    // 动画状态 AnimState+ActionCode
    public int flipx = 1;//朝右？
    public AnimState animstate;
    public ActionCode actionCode;

    //额外操作
    //public List<ActionCode> Actions;
    //public float Execute_x;
    //public float Execute_y;
    //后续拓展
}
// 服务端发送的帧数据协议（服务端→客户端）
public class MsgFramePack:MsgBase
{
    public int frameId;         // 服务端权威帧号
    public string roomId;
    public MsgFramePack() { protoName = "MsgFramePack"; }
    public List<FrameData> frames=new List<FrameData>();

}
// 服务端的单帧数据结构(包含该帧所有玩家的操作，帧序号)<后续考虑要不要存状态量？>
// 和需要补帧的客户端区别开来
public class FrameData
{
    public int frameId;         // 服务端权威帧号
    public int randSeed;        // 本帧随机种子
    public List<MsgPlayerOp> allPlayerOps=new List<MsgPlayerOp>(); // 本帧全量玩家操作
}

// 客户端发送加载准备完成///客户端接收到说明可以开始游戏。//后面改造增加状态量用于更新初始化数据
public class MsgBattleReady : MsgBase
{
    public MsgBattleReady() { protoName = "MsgBattleReady"; }
    public string roomId;
    public int teamId;
    public int userId;
}

public class MsgBattleOver : MsgBase
{
    //
    public MsgBattleOver() {
        protoName = "MsgBattleOver";
    }
    public string roomId;
    public int userId;
}
public class MsgPlayerExit : MsgBase
{
    public MsgPlayerExit()
    {
        protoName = "MsgPlayerExit";
    }
    public string roomId;
    public int userId;
}
