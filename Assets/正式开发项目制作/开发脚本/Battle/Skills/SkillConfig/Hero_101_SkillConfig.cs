using UnityEngine;

public class Hero_101_Attack_Config : SkillConfigBase
{
    // 编辑器配置
    [Header("攻击配置")]
    public float damage;
    // 普攻最大范围
    [SerializeField]
    private float attackRadius;


    public int cooldownFrames;

    // 外部定点数


    private void OnValidate()
    {
        
    }
}
public class Hero_101_Aoyi_Config : SkillConfigBase
{


}