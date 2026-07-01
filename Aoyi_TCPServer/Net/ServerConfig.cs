using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class ServerConfig
{
    public const int UdpServerPort = 887;
    public const int TcpServerPort = 888;

    public const int _frameTime = 50;

    public class modeConfig
    {
        public int MaxPlayerNum { get; set; }
        public int MaxTeamNum { get; set; }
        public int EachTeamNum { get; set; }//等于MaxPlaeyerNum/MaxTeamNum
    }
    //模式信息配置
    public static Dictionary<GameModes,modeConfig> modesConfig=new Dictionary<GameModes, modeConfig>() {
        { GameModes.dantiao,new modeConfig(){MaxPlayerNum=2,MaxTeamNum=2,EachTeamNum=1 } },{GameModes.shengcun,new modeConfig(){MaxPlayerNum=12,MaxTeamNum=12,EachTeamNum=1
        } },{GameModes.paiwei,new modeConfig(){ MaxPlayerNum=15,MaxTeamNum=3,EachTeamNum=5} }
    };

}

