
using FixMath;

/// <summary>
/// 非玩家物体接口（如子弹、召唤物等）
/// </summary>
public interface IEntityLogic
{
    void onLogicUpdate(int frameId);
    bool isAlive { get; }
}

public interface ISkillLogic
{

}