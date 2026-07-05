using FixMath;
using static UnityEngine.ParticleSystem;
using UnityEngine;

public class SceneObjInfo :EntityInfo,ICollisionPatches
{
    public override void Init(object o)
    {
        if(o is ModeConfig modeconfig&&modeconfig!=null)
        {
            // ������ײ�������ݽ�ɫ���ã�
            if (modeconfig._colliderDatas != null && modeconfig._colliderDatas.Count != 0)
            {
                foreach (var data in modeconfig._colliderDatas)
                {
                    static_col = ColliderTool.ColliderDataConvertToCollider(data, _currLogicPos, false);
                    if (static_col is FixedPolygonCollider2D poly)
                    {
                        poly.isFill = false;
                        poly.SetFollowObj(this);
                        CollisionManager.Instance.AddStaticCollider(poly);
                        poly.Layer = (int)CollisionLayer.Wall;
                        continue;
                    }
                    static_col.SetFollowObj(this);
                    CollisionManager.Instance.AddStaticCollider(static_col);

                }
                FixedColliderPosSync(); // ͬ����ײ��λ��
            }
            if (static_col != null)
            {
                static_col.OnCollisionEnter += OnFixedCollisionEnter;
                static_col.OnCollisionStay += OnFixedCollisionStay;
            }
        }
    }
    public void OnFixedTriggerEnter(CollisionInfo info)
    {
        throw new System.NotImplementedException();
    }

    public void OnFixedTriggerExit(CollisionInfo info)
    {
        throw new System.NotImplementedException();
    }

    public void OnFixedTriggerStay(CollisionInfo info)
    {
        throw new System.NotImplementedException();
    }

    public void OnFixedCollisionEnter(CollisionInfo info)
    {
#if UNITY_EDITOR
        Debug.Log("OnFixedCollisionEnter");
#endif
        FixedCollider2D playerCol = null;
        if (info.colliderA == this.static_col)
        {
            playerCol = info.colliderB;
        }
        else playerCol = info.colliderA;
        // ���������������� colliderA������ -Normal������� colliderB������ +Normal
        FixedVector2 correctionDir = (playerCol == info.colliderA) ? -info.Normal : info.Normal;
        FixedVector2 correction = correctionDir * info.PenetrationDepth;
        playerCol.renderInfo.SetPosition(correction);
    }

    public void OnFixedCollisionExit(CollisionInfo info)
    {
        throw new System.NotImplementedException();
    }

    public void OnFixedCollisionStay(CollisionInfo info)
    {
#if UNITY_EDITOR
        Debug.Log("OnFixedCollisionStay");
#endif
        FixedCollider2D playerCol = null;
        if (info.colliderA == this.static_col)
        {
            playerCol = info.colliderB;
        }
        else playerCol = info.colliderA;
        // ���������������� colliderA������ -Normal������� colliderB������ +Normal
        FixedVector2 correctionDir = (playerCol == info.colliderA) ? -info.Normal : info.Normal;
        FixedVector2 correction = correctionDir * info.PenetrationDepth;
        playerCol.renderInfo.SetPosition(correction);
    }
}