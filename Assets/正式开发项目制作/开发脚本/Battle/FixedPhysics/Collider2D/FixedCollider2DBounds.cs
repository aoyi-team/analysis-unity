using FixMath;
using System.Collections.Generic;

public readonly struct FixedBounds2D
{
    public readonly Fixed64 MinX, MinY, MaxX, MaxY;
    public FixedBounds2D(Fixed64 minX, Fixed64 minY, Fixed64 maxX, Fixed64 maxY)
    {
        MinX = minX; MinY = minY; MaxX = maxX; MaxY = maxY;
    }

    public bool Overlaps(in FixedBounds2D other)
    {
        return MaxX >= other.MinX && MinX <= other.MaxX
            && MaxY >= other.MinY && MinY <= other.MaxY;
    }

    public bool Contains(Fixed64 x, Fixed64 y)
    {
        return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
    }
}

public static class FixedCollider2DBounds
{
    public static FixedBounds2D GetBounds(this FixedCollider2D c)
    {
        FixedVector2 center = c.LogicPos + c.Center;

        switch (c)
        {
            case FixedBoxCollider2D box:
                return new FixedBounds2D(
                    center.x - box.width, center.y - box.height,
                    center.x + box.width, center.y + box.height);

            case FixedSphereCollider2D sphere:
                return new FixedBounds2D(
                    center.x - sphere.radius, center.y - sphere.radius,
                    center.x + sphere.radius, center.y + sphere.radius);

            case FixedOBBCollider2D obb:
                return GetOBBBounds(obb);

            case FixedPolygonCollider2D poly:
                return GetPolygonBounds(poly.GetWorldVertices);

            case FixedEdgeCollider2D edge:
                return GetEdgeBounds(edge);

            default:
                return new FixedBounds2D(center.x, center.y, center.x, center.y);
        }
    }

    private static FixedBounds2D GetOBBBounds(FixedOBBCollider2D obb)
    {
        FixedVector2 c = obb.LogicPos + obb.Center;
        Fixed64 cos = FixedMath.Cos(obb.Rotation, true);
        Fixed64 sin = FixedMath.Sin(obb.Rotation, true);
        Fixed64 hx = obb.HalfWidth;
        Fixed64 hy = obb.HalfHeight;

        // OBB 投影到世界轴的 AABB
        Fixed64 ex = Fixed64.Abs(cos) * hx + Fixed64.Abs(sin) * hy;
        Fixed64 ey = Fixed64.Abs(sin) * hx + Fixed64.Abs(cos) * hy;
        return new FixedBounds2D(c.x - ex, c.y - ey, c.x + ex, c.y + ey);
    }

    private static FixedBounds2D GetPolygonBounds(IReadOnlyList<FixedVector2> verts)
    {
        Fixed64 minX = verts[0].x, minY = verts[0].y;
        Fixed64 maxX = minX, maxY = minY;
        for (int i = 1; i < verts.Count; i++)
        {
            if (verts[i].x < minX) minX = verts[i].x;
            if (verts[i].y < minY) minY = verts[i].y;
            if (verts[i].x > maxX) maxX = verts[i].x;
            if (verts[i].y > maxY) maxY = verts[i].y;
        }
        return new FixedBounds2D(minX, minY, maxX, maxY);
    }

    private static FixedBounds2D GetEdgeBounds(FixedEdgeCollider2D edge)
    {
        var segs = edge.GetSegments;
        if (segs.Count == 0)
            return new FixedBounds2D(edge.X, edge.Y, edge.X, edge.Y);

        Fixed64 minX = segs[0].start.x, minY = segs[0].start.y;
        Fixed64 maxX = minX, maxY = minY;
        foreach (var seg in segs)
        {
            Expand(ref minX, ref minY, ref maxX, ref maxY, seg.start);
            Expand(ref minX, ref minY, ref maxX, ref maxY, seg.end);
        }
        return new FixedBounds2D(minX, minY, maxX, maxY);
    }

    private static void Expand(ref Fixed64 minX, ref Fixed64 minY, ref Fixed64 maxX, ref Fixed64 maxY, FixedVector2 p)
    {
        if (p.x < minX) minX = p.x;
        if (p.y < minY) minY = p.y;
        if (p.x > maxX) maxX = p.x;
        if (p.y > maxY) maxY = p.y;
    }
}