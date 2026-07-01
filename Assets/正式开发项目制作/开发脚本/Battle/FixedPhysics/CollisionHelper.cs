using FixMath;
using System.Collections.Generic;
using System.Drawing;


public static class CollisionHelper
{

    #region 对外接口
    public static bool DetectCollision(FixedCollider2D colliderA, FixedCollider2D colliderB, out CollisionInfo info)
    {
        info = new CollisionInfo { colliderA = colliderA, colliderB = colliderB, IsTrigger = colliderA.IsTrigger || colliderB.IsTrigger };
        // Box
        if (colliderA is FixedBoxCollider2D boxA)
        {
            switch (colliderB)
            {
                case FixedBoxCollider2D boxB: return DetectBoxBox(boxA, boxB, out info);
                case FixedSphereCollider2D SphereB: return DetectBoxSphere(boxA, SphereB, out info);
                case FixedOBBCollider2D obbB: return DetectBoxOBB(boxA, obbB, out info);
                case FixedPolygonCollider2D polygonB: return DetectBoxPolygon(boxA, polygonB, out info);
                case FixedEdgeCollider2D edgeB: return DetectBoxEdge(boxA, edgeB, out info);
            }
        }
        // Sphere
        if (colliderA is FixedSphereCollider2D sphereA)
        {
            switch (colliderB)
            {
                case FixedBoxCollider2D boxB: return DetectBoxSphere(boxB, sphereA, out info, swap: true);
                case FixedSphereCollider2D SphereB: return DetectSphereSphere(sphereA, SphereB, out info);
                case FixedOBBCollider2D obbB: return DetectSphereOBB(sphereA, obbB, out info);
                case FixedPolygonCollider2D polygonB: return DetectSpherePolygon(sphereA, polygonB, out info);
                case FixedEdgeCollider2D edgeB: return DetectSphereEdge(sphereA, edgeB, out info);
            }
        }
        // OBB
        if (colliderA is FixedOBBCollider2D obbA)
        {
            switch (colliderB)
            {
                case FixedBoxCollider2D boxB: return DetectBoxOBB(boxB, obbA, out info, swap: true);
                case FixedSphereCollider2D SphereB: return DetectSphereOBB(SphereB, obbA, out info, swap: true);
                case FixedOBBCollider2D obbB: return DetectOBBOBB(obbA, obbB, out info);
                case FixedPolygonCollider2D polygonB: return DetectOBBPolygon(obbA, polygonB, out info);
                case FixedEdgeCollider2D edgeB: return DetectOBBEdge(obbA, edgeB, out info);
            }
        }
        // Polygon
        if (colliderA is FixedPolygonCollider2D polygonA)
        {
            switch (colliderB)
            {
                case FixedBoxCollider2D boxB: return DetectBoxPolygon(boxB, polygonA, out info, swap: true);
                case FixedSphereCollider2D SphereB: return DetectSpherePolygon(SphereB, polygonA, out info, swap: true);
                case FixedOBBCollider2D obbB: return DetectOBBPolygon(obbB, polygonA, out info, swap: true);
                case FixedPolygonCollider2D polygonB: return DetectPolygonPolygon(polygonA, polygonB, out info);
                case FixedEdgeCollider2D edgeB: return DetectEdgePolygon(edgeB, polygonA, out info);
            }
        }
        // Edge
        if (colliderA is FixedEdgeCollider2D edgeA)
        {
            switch (colliderB)
            {
                case FixedBoxCollider2D boxB: return DetectBoxEdge(boxB, edgeA, out info, swap: true);
                case FixedSphereCollider2D SphereB: return DetectSphereEdge(SphereB, edgeA, out info, swap: true);
                case FixedOBBCollider2D obbB: return DetectOBBEdge(obbB, edgeA, out info, swap: true);
                case FixedPolygonCollider2D polygonB: return DetectEdgePolygon(edgeA, polygonB, out info);
                case FixedEdgeCollider2D edgeB: return DetectEdgeEdge(edgeA, edgeB, out info);
            }
        }
        return false;
    }

    #endregion

    // 核心算法模块
    #region BoxCollider2D相关算法
    public static bool DetectBoxBox(FixedBoxCollider2D A, FixedBoxCollider2D B, out CollisionInfo info)
    {
        info = new CollisionInfo { colliderA = A, colliderB = B };
        // 这里可以实现Box-Box的碰撞检测逻辑
        Fixed64 overlapX = (A.width + B.width) - Fixed64.Abs(A.X - B.X);
        Fixed64 overlapY = (A.height + B.height) - Fixed64.Abs(A.Y - B.Y);
        if (overlapX < 0 || overlapY < 0) return false;

        // 碰撞发生，计算碰撞信息
        info.PenetrationDepth = Fixed64.Min(overlapX, overlapY);
        info.Normal = overlapX < overlapY ? (A.X < B.X ? FixedVector2.Right : FixedVector2.Left) : (A.Y < B.Y ? FixedVector2.Up : FixedVector2.Down);
        info.ContactPoint = new FixedVector2(Fixed64.Lerp(A.X, B.X, 0.5f), Fixed64.Lerp(A.Y, B.Y, 0.5f));
        return true;
    }

    public static bool DetectBoxSphere(FixedBoxCollider2D box, FixedSphereCollider2D Sphere, out CollisionInfo info, bool swap = false)
    {
        info = new CollisionInfo();
        // 这里可以实现Box-Circle的碰撞检测逻辑

        FixedVector2 center = Sphere.WorldLogicPos;
        FixedVector2 closet = new FixedVector2(
            Fixed64.Clamp(center.x, box.X - box.width, box.X + box.width),
            Fixed64.Clamp(center.y, box.Y - box.height, box.Y + box.height)
        );
        FixedVector2 dir = center - closet;
        Fixed64 distSqr = dir.SqrMagnitude;
        if (distSqr > Sphere.radius * Sphere.radius) return false;

        Fixed64 dis = Fixed64.Sqrt(distSqr);
        info.PenetrationDepth = Sphere.radius - dis;
        info.Normal = dis > Fixed64.Zero ? dir.Normalize : FixedVector2.Up; // 如果圆心在矩形内部，默认法线向上
        info.ContactPoint = closet;

        if (swap) (info.colliderA, info.colliderB) = (info.colliderB, info.colliderA);
        return true;
    }
    public static bool DetectBoxOBB(FixedBoxCollider2D box, FixedOBBCollider2D obb, out CollisionInfo info, bool swap = false)
    {
        var boxAsOBB = new FixedOBBCollider2D(box.LogicPos, box.Center, box.Size, Fixed64.Zero);
        bool result = DetectOBBOBB(boxAsOBB, obb, out info);
        if (swap && result) (info.colliderA, info.colliderB) = (info.colliderB, info.colliderA);
        return result;

    }
    public static bool DetectBoxPolygon(FixedBoxCollider2D box, FixedPolygonCollider2D polygon, out CollisionInfo info, bool swap = false)
    {
        var boxAsPolygon = new FixedPolygonCollider2D(box.LogicPos, box.Center, GetBoxVertices(box), Fixed64.Zero);
        bool result = DetectPolygonPolygon(boxAsPolygon, polygon, out info);
        if (swap && result) (info.colliderA, info.colliderB) = (info.colliderB, info.colliderA);
        return result;
    }
    public static bool DetectBoxEdge(FixedBoxCollider2D box, FixedEdgeCollider2D edge, out CollisionInfo info, bool swap = false)
    {
        var boxAsPolygon = new FixedPolygonCollider2D(box.LogicPos, box.Center, GetBoxVertices(box), Fixed64.Zero);
        bool result = DetectEdgePolygon(edge, boxAsPolygon, out info);
        if (swap && result) (info.colliderA, info.colliderB) = (info.colliderB, info.colliderA);
        return result;
    }
    private static List<FixedVector2> GetBoxVertices(FixedBoxCollider2D box)
    {
        Fixed64 halfW = box.width;
        Fixed64 halfH = box.height;
        return new List<FixedVector2>
        {
            new FixedVector2(- halfW, - halfH),
            new FixedVector2(halfW, - halfH),
            new FixedVector2(halfW, halfH),
            new FixedVector2(- halfW, halfH)
        };
    }
    #endregion

    #region SphereCollider2D相关算法
    public static bool DetectSphereSphere(FixedSphereCollider2D A, FixedSphereCollider2D B, out CollisionInfo info)
    {
        info = new CollisionInfo { colliderA = A, colliderB = B };
        FixedVector2 delta = (A.WorldLogicPos) - (B.WorldLogicPos);
        Fixed64 distSqr = delta.SqrMagnitude;
        Fixed64 radSum = A.radius + B.radius;
        if (distSqr > radSum * radSum) return false;

        Fixed64 dis = Fixed64.Sqrt(distSqr);
        info.PenetrationDepth = radSum - dis;
        info.Normal = dis > Fixed64.Zero ? delta.Normalize : FixedVector2.Up; // 如果圆心重合，默认法线向上
        info.ContactPoint = (A.WorldLogicPos) - info.Normal * A.radius; // 碰撞点在A的圆周上
        return true;
    }
    public static bool DetectSphereOBB(FixedSphereCollider2D circle, FixedOBBCollider2D obb, out CollisionInfo info, bool swap = false)
    {
        info = new CollisionInfo { colliderA = circle, colliderB = obb };
        // 这里可以实现Circle-OBB的碰撞检测逻辑
        FixedVector2 localCenter = obb.TransformPointToLocal(circle.WorldLogicPos);
        FixedVector2 halfSize = new FixedVector2(obb.HalfWidth, obb.HalfHeight);
        FixedVector2 closet = new FixedVector2(
            Fixed64.Clamp(localCenter.x, -halfSize.x, halfSize.x),
            Fixed64.Clamp(localCenter.y, -halfSize.y, halfSize.y)
        );
        FixedVector2 delta = localCenter - closet;
        Fixed64 distSq = delta.SqrMagnitude;
        if (distSq > circle.radius * circle.radius) return false;

        Fixed64 dist = Fixed64.Sqrt(distSq);
        info.PenetrationDepth = circle.radius - dist;
        // 将法线从局部空间转换回世界空间
        FixedVector2 worldNormal = delta.Rotate(obb.Rotation);
        info.Normal = worldNormal.Normalize;
        info.ContactPoint = obb.WorldLogicPos + closet.Rotate(obb.Rotation); // 碰撞点在OBB表面
        if (swap) (info.colliderA, info.colliderB) = (info.colliderB, info.colliderA);
        return true;

    }
    public static bool DetectSpherePolygon(FixedSphereCollider2D circle, FixedPolygonCollider2D polygon, out CollisionInfo info, bool swap = false)
    {
        info = new CollisionInfo { colliderA = circle, colliderB = polygon };
        // 这里可以实现Circle-Polygon的碰撞检测逻辑
        FixedVector2 center = circle.WorldLogicPos;
        FixedVector2 closet = FindClosestPointOnPolygon(center, polygon.GetWorldVertices);
        FixedVector2 dir = center - closet;
        Fixed64 dist = dir.Magnitude;
        // 如果填充了且在内部，判定为碰撞
        if (polygon.isFill)
        {
            bool isInside = PointInPolygon(center, polygon.GetWorldVertices);
            if(isInside)
            {
                info.PenetrationDepth= circle.radius + dist;
                info.Normal = dist > Fixed64.Zero ? dir.Normalize : FixedVector2.Up;
                info.ContactPoint = closet;
                if (swap) (info.colliderA, info.colliderB) = (info.colliderB, info.colliderA);
                return true;
            }
        }
        // 在外部
        Fixed64 distSq = dir.SqrMagnitude;
        if (distSq > circle.radius * circle.radius) return false;

        info.PenetrationDepth = circle.radius - dist;
        info.Normal = dist > Fixed64.Zero ? dir.Normalize : FixedVector2.Up; // 如果圆心在多边形内部，默认法线向上
        info.ContactPoint = closet;
        if (swap) (info.colliderA, info.colliderB) = (info.colliderB, info.colliderA);
        return true;

    }
    public static bool DetectSphereEdge(FixedSphereCollider2D circle, FixedEdgeCollider2D edge, out CollisionInfo info, bool swap = false)
    {
        info = new CollisionInfo { colliderA = circle, colliderB = edge };
        // 这里可以实现Circle-Edge的碰撞检测逻辑
        FixedVector2 center = circle.WorldLogicPos;
        Fixed64 minPan = Fixed64.MaxValue;
        FixedVector2 bestNormal = FixedVector2.Zero;
        FixedVector2 bestPoint = FixedVector2.Zero;
        bool hit = false;

        foreach (var seg in edge.GetSegments)
        {
            FixedVector2 near = FixedVector2.ClosetPointToSegment(center, seg.start, seg.end);
            FixedVector2 dir = center - near;
            Fixed64 distSqr = dir.SqrMagnitude;
            if (distSqr > circle.radius * circle.radius) continue;
            Fixed64 dist = Fixed64.Sqrt(distSqr);
            Fixed64 penetration = circle.radius - dist;
            if (penetration < minPan)
            {
                minPan = penetration;
                bestNormal = dist > 0 ? dir.Normalize : FixedVector2.Up; // 如果圆心在边界线上，默认法线向上
                bestPoint = near;
                hit = true;
            }
        }
        if (!hit) return false;

        info.PenetrationDepth = minPan;
        info.Normal = bestNormal;
        info.ContactPoint = bestPoint;
        if (swap) (info.colliderA, info.colliderB) = (info.colliderB, info.colliderA);
        return true;
    }
    #endregion

    #region OBBCollider2D相关算法
    public static bool DetectOBBOBB(FixedOBBCollider2D A, FixedOBBCollider2D B, out CollisionInfo info)
    {
        info = new CollisionInfo { colliderA = A, colliderB = B };
        // 这里可以实现OBB-OBB的碰撞检测逻辑
        List<FixedVector2> axes = new List<FixedVector2>()
        {
            new FixedVector2(FixedMath.Cos(A.Rotation,true), FixedMath.Sin(A.Rotation,true)), // OBB的一个轴
            new FixedVector2(-FixedMath.Sin(A.Rotation,true), FixedMath.Cos(A.Rotation,true)), // OBB的另一个轴
            new FixedVector2(FixedMath.Cos(B.Rotation,true), FixedMath.Sin(B.Rotation,true)), // 另一个OBB的一个轴
            new FixedVector2(-FixedMath.Sin(B.Rotation,true), FixedMath.Cos(B.Rotation,true)) // 另一个OBB的另一个轴
        };

        Fixed64 minPen = Fixed64.MaxValue;
        FixedVector2 bestAxis = FixedVector2.Zero;

        foreach (var axis in axes)
        {
            var (minA, maxA) = ProjectOBB(A, axis);
            var (minB, maxB) = ProjectOBB(B, axis);
            if (maxA < minB || maxB < minA) return false; // 没有重叠，分离轴存在

            Fixed64 overlap = Fixed64.Min(maxA, maxB) - Fixed64.Max(minA, minB);
            if (overlap < minPen)
            {
                minPen = overlap;
                bestAxis = axis;
            }
        }

        info.PenetrationDepth = minPen;
        FixedVector2 centerA = A.WorldLogicPos;
        FixedVector2 centerB = B.WorldLogicPos;

        if (FixedVector2.Dot(centerB - centerA, bestAxis) < Fixed64.Zero)
        {
            bestAxis = -bestAxis;
        }
        info.Normal = bestAxis;
        // 近似接触点
        info.ContactPoint = centerA + bestAxis * (ProjectOBB(A, bestAxis).max - minPen / 2);// 后续考虑更精确的接触点计算方法
        return true;
    }
    public static bool DetectOBBPolygon(FixedOBBCollider2D obb, FixedPolygonCollider2D polygon, out CollisionInfo info, bool swap = false)
    {
        info = new CollisionInfo { colliderA = obb, colliderB = polygon };
        // 将 OBB 转为临时多边形
        var obbPoly = new FixedPolygonCollider2D(obb.LogicPos, obb.Center, GetOBBVertices(obb), obb.Rotation);
        bool result = DetectPolygonPolygon(obbPoly, polygon, out info);
        if (swap && result) (info.colliderA, info.colliderB) = (info.colliderB, info.colliderA);
        return result;

    }
    public static bool DetectOBBEdge(FixedOBBCollider2D obb, FixedEdgeCollider2D edge, out CollisionInfo info, bool swap = false)
    {
        info = new CollisionInfo { colliderA = obb, colliderB = edge };
        // 这里可以实现OBB-Edge的碰撞检测逻辑
        var obbPoly = new FixedPolygonCollider2D(obb.LogicPos, obb.Center, GetOBBVertices(obb), obb.Rotation);
        bool result = DetectEdgePolygon(edge, obbPoly, out info);
        if (swap && result) (info.colliderA, info.colliderB) = (info.colliderB, info.colliderA);
        return result;
    }

    // 投影OBB到轴上，返回最小和最大投影值
    private static (Fixed64 min, Fixed64 max) ProjectOBB(FixedOBBCollider2D obb, FixedVector2 axis)
    {
        FixedVector2 center = obb.WorldLogicPos;
        FixedVector2 half = new FixedVector2(obb.HalfWidth, obb.HalfHeight);
        FixedVector2 xAxis = new FixedVector2(FixedMath.Cos(obb.Rotation), FixedMath.Sin(obb.Rotation));
        FixedVector2 yAxis = new FixedVector2(-FixedMath.Sin(obb.Rotation), FixedMath.Cos(obb.Rotation));

        Fixed64 xProj = Fixed64.Abs(FixedVector2.Dot(axis, xAxis)) * half.x;
        Fixed64 yProj = Fixed64.Abs(FixedVector2.Dot(axis, yAxis)) * half.y;
        Fixed64 c = FixedVector2.Dot(center, axis);
        return (c - xProj - yProj, c + xProj + yProj);

    }
    private static List<FixedVector2> GetOBBVertices(FixedOBBCollider2D obb)
    {
        FixedVector2 half = new FixedVector2(obb.HalfWidth, obb.HalfHeight);
        List<FixedVector2> local = new List<FixedVector2>
        {
            new FixedVector2(-half.x, -half.y),
            new FixedVector2( half.x, -half.y),
            new FixedVector2( half.x,  half.y),
            new FixedVector2(-half.x,  half.y)
        };
        // 变成原初坐标系下的顶点
        for (int i = 0; i < local.Count; i++)
            local[i] = local[i].Rotate(obb.Rotation);
        return local;
    }
    #endregion
    #region PolygonCollider2D相关算法
    public static bool DetectPolygonPolygon(FixedPolygonCollider2D A, FixedPolygonCollider2D B, out CollisionInfo info)
    {
        info = new CollisionInfo { colliderA = A, colliderB = B };
        // 这里可以实现Polygon-Polygon的碰撞检测逻辑
        List<FixedVector2> verticesA = new List<FixedVector2>(A.GetWorldVertices);
        List<FixedVector2> verticesB = new List<FixedVector2>(B.GetWorldVertices);
        List<FixedVector2> axes = new List<FixedVector2>();
        axes.AddRange(GetPolygonAxes(verticesA));
        axes.AddRange(GetPolygonAxes(verticesB));

        Fixed64 minPen = Fixed64.MaxValue;
        FixedVector2 bestAxis = FixedVector2.Zero;

        foreach (var axis in axes)
        {
            var (minA, maxA) = ProjectPolygon(verticesA, axis);
            var (minB, maxB) = ProjectPolygon(verticesB, axis);
            if (maxA < minB || maxB < minA) return false;

            Fixed64 overlap = Fixed64.Min(maxA, maxB) - Fixed64.Max(minA, minB);
            if (overlap < minPen)
            {
                minPen = overlap;
                bestAxis = axis;
            }
        }

        // 修正法线方向
        FixedVector2 centerA = GetPolygonCenter(verticesA);
        FixedVector2 centerB = GetPolygonCenter(verticesB);
        if (FixedVector2.Dot(centerB - centerA, bestAxis) < Fixed64.Zero)
            bestAxis = -bestAxis;

        info.PenetrationDepth = minPen;
        info.Normal = bestAxis;
        info.ContactPoint = new FixedVector2((centerA.x + centerB.x) / 2, (centerA.y + centerB.y) / 2); // 简单地使用两个中心点的中点作为接触点
        return true;
    }
    private static List<FixedVector2> GetPolygonAxes(List<FixedVector2> vertices)
    {
        List<FixedVector2> axes = new List<FixedVector2>();
        int count = vertices.Count;
        for (int i = 0; i < count; i++)
        {
            FixedVector2 edge = vertices[(i + 1) % count] - vertices[i];
            FixedVector2 normal = new FixedVector2(-edge.y, edge.x).Normalize;
            axes.Add(normal);
        }
        return axes;
    }
    private static (Fixed64 min, Fixed64 max) ProjectPolygon(List<FixedVector2> vertices, FixedVector2 axis)
    {
        Fixed64 min = FixedVector2.Dot(vertices[0], axis);
        Fixed64 max = min;
        for (int i = 1; i < vertices.Count; i++)
        {
            Fixed64 val = FixedVector2.Dot(vertices[i], axis);
            if (val < min) min = val;
            if (val > max) max = val;
        }
        return (min, max);
    }
    private static FixedVector2 GetPolygonCenter(List<FixedVector2> vertices)
    {
        FixedVector2 sum = FixedVector2.Zero;
        foreach (var v in vertices) sum += v;
        return sum / vertices.Count.ToFixed();
    }
    private static FixedVector2 FindClosestPointOnPolygon(FixedVector2 point, IReadOnlyList<FixedVector2> vertices)
    {
        Fixed64 minDistSq = Fixed64.MaxValue;
        FixedVector2 closest = vertices[0];
        int count = vertices.Count;
        for (int i = 0; i < count; i++)
        {
            FixedVector2 a = vertices[i];
            FixedVector2 b = vertices[(i + 1) % count];
            FixedVector2 p = FixedVector2.ClosetPointToSegment(point, a, b);
            Fixed64 distSq = (point - p).SqrMagnitude;
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                closest = p;
            }
        }
        return closest;
    }

    #endregion

    #region EdgeCollider2D相关算法
    private static bool DetectEdgeEdge(FixedEdgeCollider2D A, FixedEdgeCollider2D B, out CollisionInfo info)
    {
        info = new CollisionInfo { colliderA = A, colliderB = B };
        // 这里可以实现Edge-Edge的碰撞检测逻辑
        info = new CollisionInfo();
        var segsA = A.GetSegments;
        var segsB = B.GetSegments;

        foreach (var segA in segsA)
        {
            foreach (var segB in segsB)
            {
                if (LineSegmentIntersection(segA.start, segA.end, segB.start, segB.end, out FixedVector2 point))
                {
                    info.ContactPoint = point;
                    info.Normal = (segA.end - segA.start).Normalize.Rotate(Fixed64.pi / 2); // 近似
                    info.PenetrationDepth = Fixed64.Zero;
                    return true;
                }
            }
        }
        return false;
    }
    private static bool DetectEdgePolygon(FixedEdgeCollider2D edge, FixedPolygonCollider2D polygon, out CollisionInfo info, bool swap = false)
    {
        info = new CollisionInfo { colliderA = edge, colliderB = polygon };
        // 这里可以实现Edge-Polygon的碰撞检测逻辑
        info = new CollisionInfo();
        var vertices = polygon.GetWorldVertices;
        var segments = edge.GetSegments;

        foreach (var seg in segments)
        {
            // 线段与多边形的每条边相交检测
            for (int i = 0; i < vertices.Count; i++)
            {
                FixedVector2 a = vertices[i];
                FixedVector2 b = vertices[(i + 1) % vertices.Count];
                if (LineSegmentIntersection(seg.start, seg.end, a, b, out FixedVector2 point))
                {
                    info.ContactPoint = point;
                    info.Normal = (b - a).Normalize.Rotate(Fixed64.pi / 2);
                    info.PenetrationDepth = Fixed64.Zero;
                    return true;
                }
            }
            // 检查线段端点是否在多边形内部
            if (PointInPolygon(seg.start, vertices) || PointInPolygon(seg.end, vertices))
            {
                info.ContactPoint = seg.start;
                info.Normal = FixedVector2.Up;
                info.PenetrationDepth = Fixed64.Zero;
                return true;
            }
        }
        return false;
    }
    private static bool LineSegmentIntersection(FixedVector2 p1, FixedVector2 p2, FixedVector2 p3, FixedVector2 p4, out FixedVector2 intersection)
    {
        intersection = FixedVector2.Zero;
        FixedVector2 d1 = p2 - p1;
        FixedVector2 d2 = p4 - p3;
        Fixed64 denom = FixedVector2.Cross(d1, d2);
        if (denom == Fixed64.Zero) return false; // 平行
        Fixed64 t = FixedVector2.Cross(p3 - p1, d2) / denom;
        Fixed64 u = FixedVector2.Cross(p3 - p1, d1) / denom;
        if (t >= Fixed64.Zero && t <= Fixed64.One && u >= Fixed64.Zero && u <= Fixed64.One)
        {
            intersection = p1 + d1 * t;
            return true;
        }
        return false;
    }
    private static bool PointInPolygon(FixedVector2 point, IReadOnlyList<FixedVector2> vertices)
    {
        // 叉积法（凸多边形）
        for (int i = 0; i < vertices.Count; i++)
        {
            FixedVector2 a = vertices[i];
            FixedVector2 b = vertices[(i + 1) % vertices.Count];
            FixedVector2 edge = b - a;
            FixedVector2 toPoint = point - a;
            Fixed64 cross = FixedVector2.Cross(edge, toPoint);
            if (cross < Fixed64.Zero) return false;
        }
        return true;
    }

    #endregion
}