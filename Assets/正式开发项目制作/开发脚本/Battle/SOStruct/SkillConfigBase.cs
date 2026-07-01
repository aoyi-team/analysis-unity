using UnityEngine;

public abstract class SkillConfigBase : ScriptableObject
{
    [Header("技能ID")]
    public int skillId;
    [Header("技能名字")]
    public string skillName;
    [Header("技能描述")]
    public string skillDescription;
}