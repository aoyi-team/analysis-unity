using FixMath;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "dantiao_ModeConfig", menuName = "Game/dantiao_ModeConfig")]
public class dantiao_ModeConfig : ModeConfig
{
    [Header("胜利条件")]
    public int winScore; // 达到这个分数的一方获胜

}