using FixMath;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTestController : MonoBehaviour, ICollisionPatches
{
    private _playerInfo testPlayer;
    private FixedSphereCollider2D playerCollider;
    public CircleCollider2D playerCircle;
    public PolygonCollider2D edge;   //public BoxCollider2D box;
    private bool hasInit = false;

    #region 调试Gizmons
    /*void OnDrawGizmos()
    {
        if (edge == null) return;
        Gizmos.color = Color.blue;
        for (int i = 0; i <= edge.points.Length - 1; i++)
        {
            Vector3 start = edge.transform.position + (Vector3)RotatePoint(edge.points[i], edge.transform.eulerAngles.z);
            Vector3 end = edge.transform.position +(Vector3)RotatePoint(edge.points[i + 1],edge.transform.eulerAngles.z);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(start, end);
        }
    }*/
    /*private void OnDrawGizmos()
    {
        if (box == null) return;

        // 绘制 OBB 边界
        Vector2 center = new Vector2( box.transform.position.x,box.transform.position.y) + (Vector2)box.offset;
        Vector2 size = box.size;
        float angle = box.transform.eulerAngles.z; // 如果是 90 度

        Gizmos.color = Color.red;
        // 绘制旋转后的矩形
        Vector2[] corners = new Vector2[4];
        Vector2 half = size / 2;
        corners[0] = new Vector2(-half.x, -half.y);
        corners[1] = new Vector2(half.x, -half.y);
        corners[2] = new Vector2(half.x, half.y);
        corners[3] = new Vector2(-half.x, half.y);

        // 旋转并平移
        for (int i = 0; i < 4; i++)
        {
            Vector2 rotated = RotatePoint(corners[i], angle);
            corners[i] = center + rotated;
        }

        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);

        // 绘制玩家圆形（已有）
        if (playerCircle != null)
        {
            Gizmos.color = Color.green;
            DrawCircle(playerCircle.transform.position, playerCircle.radius);
        }
    }
    */
    private Vector2 RotatePoint(Vector2 point, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(point.x * cos - point.y * sin, point.x * sin + point.y * cos);
    }

    void DrawCircle(Vector2 center, float radius, int segments = 36)
    {
        Vector2 prev = center + new Vector2(radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            Vector2 next = center + new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            ) * radius;

            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    #endregion
    

    void Start()
    {
        // 1. 初始化碰撞管理器
        CollisionManager.Instance.Init(
            new FixedVector2(-10.5f, -10.5f),
            new FixedVector2(10.5f, 10.5f)
        );
        List<FixedVector2> fixedPoints = new List<FixedVector2>();
        foreach(var p in edge.points)
        {
            fixedPoints.Add(p);
        }
        // 2. 传入的是欧拉角(角度制)
        FixedPolygonCollider2D fixedpoly = new FixedPolygonCollider2D(new Vector2(edge.transform.position.x, edge.transform.position.y), edge.offset, fixedPoints, edge.transform.localEulerAngles.z.ToFixed(), false,false);
        fixedpoly.Layer = (int)CollisionLayer.Wall;
        fixedpoly.IsTrigger = false;
        _playerInfo newEdge = new _playerInfo("TestEdge",101,1,new FixedVector2(edge.transform.position.x, edge.transform.position.y));
        CollisionManager.Instance.AddStaticCollider(fixedpoly);
        Debug.Log($"墙已注册");

        // 3. 创建玩家
        testPlayer = new _playerInfo("TestPlayer", 101, 1, new FixedVector2(transform.position.x, transform.position.y));

        // 4. 创建玩家碰撞体（圆形，阻挡类型）
        if(playerCircle!=null)
        {
            playerCollider=new FixedSphereCollider2D(testPlayer._currLogicPos,playerCircle.offset,playerCircle.radius.ToFixed(), playerCircle.transform.localEulerAngles.z.ToFixed(), false);
        }
        playerCollider.Layer = (int)CollisionLayer.Player;
        playerCollider.SetFollowObj(testPlayer);
        //testPlayer.SetDycCollider(playerCollider);

        // 订阅事件
        fixedpoly.OnCollisionEnter += OnFixedCollisionEnter;
        fixedpoly.OnCollisionStay += OnFixedCollisionStay;

        //fixedpoly.OnCollisionStay += OnFixedCollisionStay; // 可选

        CollisionManager.Instance.AddDynamicCollider(playerCollider);
        Debug.Log("玩家碰撞体已注册");

        hasInit = true;
    }

    void Update()
    {
        if (!hasInit) return;

        // 采集输入
        float h = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        Fixed64 speed = new Fixed64(3f);
        Fixed64 deltaTime = new Fixed64(0.02f);
        FixedVector2 move = new FixedVector2(h, y) * speed * deltaTime;

        // 1. 逻辑移动
        testPlayer.SetPosition(move);

        // 2. 执行碰撞检测（内部会触发事件，修正逻辑位置）
        CollisionManager.Instance.LogicUpdate();

        // 3. 将逻辑位置同步到表现层（替换掉你之前的 transform.position += ...）
        transform.position = testPlayer._currLogicPos.ToVector3();
    }
    public void OnFixedTriggerEnter(CollisionInfo info)
    {
        Debug.Log($"TriggerEnter{info.colliderA} vs {info.colliderB}");
        Debug.Log("碰撞深度:"+info.PenetrationDepth);
        Debug.Log($"碰撞法线{info.Normal}，碰撞点:{info.ContactPoint}");
    }

    public void OnFixedTriggerExit(CollisionInfo info)
    {
        Debug.Log($" TriggerExit{info.colliderA} vs {info.colliderB}");
    }

    public void OnFixedTriggerStay(CollisionInfo info)
    {
        Debug.Log($"TriggerStay{info.colliderA} vs {info.colliderB}");
    }

    public void OnFixedCollisionEnter(CollisionInfo info)
    {
        Debug.Log($" CollisionEnter！{info.colliderA} vs {info.colliderB}");

        // 确定哪个是玩家碰撞体
        FixedCollider2D playerCol = null;
        if (info.colliderA == playerCollider)
        {
            playerCol = info.colliderA;
            Debug.Log("info.colliderA == playerCollider");
        }
        else if (info.colliderB == playerCollider)
        {
            Debug.Log("info.colliderB == playerCollider");
            playerCol = info.colliderB;
        }
    }

    public void OnFixedCollisionExit(CollisionInfo info)
    {
        Debug.Log($" CollisionExi{info.colliderA} vs {info.colliderB}");
    }

    public void OnFixedCollisionStay(CollisionInfo info)
    {
        FixedCollider2D playerCol = null;
        if (info.colliderA == playerCollider)
        {
            playerCol = info.colliderA;
            Debug.Log("info.colliderA == playerCollider");
        }
        else if (info.colliderB == playerCollider)
        {
            Debug.Log("info.colliderB == playerCollider");
            playerCol = info.colliderB;
        }
        Debug.Log($" CollisionStay{info.colliderA} vs {info.colliderB}");
        // 修正方向：如果玩家是 colliderA，则沿 -Normal；如果是 colliderB，则沿 +Normal
        FixedVector2 correctionDir = (playerCol == info.colliderA) ? -info.Normal : info.Normal;
        FixedVector2 correction = correctionDir * info.PenetrationDepth;
        Debug.Log($"深度:{info.PenetrationDepth},法向量{info.Normal}");
        // 直接设置位置（而不是增量移动）
    }
}
