

using System.Collections.Generic;
using UnityEngine;

public class paiwei_ModeConfig:ModeConfig
{
    [Header("胜利条件")]
    public int winScore;
    public int holdTimeSeconds;// 倒计时模式的持有时间，单位逻辑帧

    [Header("物资系统")]
    // TODO: 物资系统的配置项，暂时先放在这里，后续可以抽离成一个单独的 ScriptableObject
    public List<Vector2> smallBoxPoints;
    public List<Vector2> bigBoxPoints;
}