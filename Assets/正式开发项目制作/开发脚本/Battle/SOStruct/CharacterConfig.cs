using FixMath;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterConfig", menuName = "Game/Character Config")]
public class CharacterConfig : ScriptableObject
{
    [Header("名字")]
    public string CharacterName;

    /* [Header("基础属性（初始值）")]
     public float maxHp = 1000;
     public float attack = 100;
     //移速
     public float moveSpeed = 5f;
     //技能伤害
     public float skillDamage = 200;
     //暴击率
     public float critRate = 0.2f;
    */
    // ========== 编辑器友好的 float 字段（不参与逻辑） ==========
    [Header("基础属性（初始值）")]
    [SerializeField] private float _maxHp = 1000f;
    [SerializeField] private float _attack = 100f;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _skillDamage = 200f;
    [SerializeField] private float _critRate = 0.2f;
    [SerializeField] private float _attackSpeed = 1f;
    [SerializeField] private float _skillCoolRate = 1f;

    // ========== 实际存储的定点数原始整数（打包后固定） ==========
    [Header("定点数原始整数（打包后固定）")]
    [SerializeField] private long _maxHpBits;
    [SerializeField] private long _attackBits;
    [SerializeField] private long _moveSpeedBits;
    [SerializeField] private long _skillDamageBits;
    [SerializeField] private long _critRateBits;
    [SerializeField] private long _attackSpeedBits;
    [SerializeField] private long _skillCoolRateBits;

    // 技能ID
    [SerializeField] public int[] Skillids;

    [Header("碰撞体数据")]
    [SerializeField] public List<ColliderData> _colliderDatas;

    // ========== 逻辑层使用的定点数属性 ==========
    public Fixed64 MaxHp => new Fixed64(_maxHpBits, true);
    public Fixed64 AttackBits => new Fixed64(_attackBits, true);
    public Fixed64 MoveSpeedBits => new Fixed64(_moveSpeedBits, true);
    public Fixed64 SkillDamageBits => new Fixed64(_skillDamageBits, true);
    public Fixed64 CritRateBits => new Fixed64(_critRateBits, true);
    public Fixed64 AttackSpeedBits => new Fixed64(_attackSpeedBits, true);
    public Fixed64 SkillCoolRateBits => new Fixed64(_skillCoolRateBits, true);

    // 在编辑器下修改数值时，自动转换并存储定点数原始值
    private void OnValidate()
    {
        _maxHpBits = new Fixed64(_maxHp).m_Bits;
        _attackBits = new Fixed64(_attack).m_Bits;
        _moveSpeedBits = new Fixed64(_moveSpeed).m_Bits;
        _skillDamageBits = new Fixed64(_skillDamage).m_Bits;
        _critRateBits = new Fixed64(_critRate).m_Bits;
        _attackSpeedBits = new Fixed64(_attackSpeed).m_Bits;
        _skillCoolRateBits = new Fixed64(_skillCoolRate).m_Bits;
    }


    [Header("动画/资源配置")]
    public RuntimeAnimatorController[] animatorController;
    //英雄头像图标，后续添加
    //public Sprite characterIcon;

#if UNITY_EDITOR
    [ContextMenu("Import from Selected Collider")]
    public  void ImportFromCollider()
    {
        var obj = UnityEditor.Selection.activeGameObject;
        if (obj == null)
        {
            Debug.LogWarning("No gameobject selected!");
            return;
        }
        var col = obj.GetComponents<Collider2D>();
        if (col != null)
        {
            _colliderDatas.Clear();
            foreach (var c in col)
            {
                var data= ColliderTool.GetColliderDataFromUnityCollider2D(c);
                if(data.colliderType != Collider2DEnum.None)
                {
                    _colliderDatas.Add(data);
                }
                Debug.Log($"import collider data from {obj.name} : {data.colliderType}, center: {data.center}, size: {data.size}, points count: {data.points?.Count ?? 0}");
            }
        }
    }
#endif
}