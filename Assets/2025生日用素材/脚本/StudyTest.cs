using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillConfig", menuName = "Skill/通用技能配置")]
public class StudyTest :ScriptableObject
{
    // 基础参数（所有技能通用）
    public string skillName; // 技能名（比如“弓箭普攻”“光球术”）
    public GameObject skillPrefab; // 技能预制体（弓箭/光球/火球）
    public float maxDistance; // 最大飞行距离
    public float speed; // 飞行速度
    public int damage; // 基础伤害

    // 可选参数（不同技能按需用）
    public float explosionRadius; // 爆炸范围（弓箭/火球用，光球可能不用）
    public float coolDownTime; // 冷却时间
    public bool isHoming; // 是否追踪目标（比如导弹技能用）
    public GameObject hitEffect; // 命中特效
}
