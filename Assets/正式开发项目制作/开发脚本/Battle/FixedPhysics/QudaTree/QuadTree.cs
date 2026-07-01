// QuadTree.cs — 性能改进版
using FixMath;
using System.Collections.Generic;

public class QuadTree
{
    public FixedVector2 Min { get; private set; }
    public FixedVector2 Max { get; private set; }

    private const int MaxObjects = 8;
    private const int MaxDepth = 6;

    private int _depth;
    private readonly List<FixedCollider2D> _objects = new();
    private QuadTree[] _children;

    public QuadTree(FixedVector2 min, FixedVector2 max, int depth = 0)
    {
        Min = min;
        Max = max;
        _depth = depth;
    }

    public void Clear()
    {
        _objects.Clear();
        if (_children != null)
        {
            for (int i = 0; i < 4; i++) _children[i].Clear();
            _children = null;
        }
    }

    public void Insert(FixedCollider2D collider)
    {
        if (!OverlapsMap(collider)) return;

        if (_children != null)
        {
            int mask = GetOverlappingChildMask(collider.GetBounds());
            if (mask == 0)
            {
                // 跨多个子区，放父节点
                _objects.Add(collider);
                return;
            }
            if (IsSingleChild(mask))
            {
                _children[FirstChildIndex(mask)].Insert(collider);
                return;
            }
            // 跨 2~4 个子区，放父节点
            _objects.Add(collider);
            return;
        }

        _objects.Add(collider);
        if (_objects.Count > MaxObjects && _depth < MaxDepth)
        {
            Split();
            Redistribute();
        }
    }

    public void Query(FixedCollider2D collider, List<FixedCollider2D> result)
    {
        Query(collider.GetBounds(), collider, result);
    }

    private void Query(in FixedBounds2D bounds, FixedCollider2D self, List<FixedCollider2D> result)
    {
        if (!NodeBounds.Overlaps(bounds)) return;

        foreach (var obj in _objects)
        {
            if (obj != self && obj.Active)
                result.Add(obj);
        }

        if (_children == null) return;

        for (int i = 0; i < 4; i++)
        {
            if (ChildBounds(i).Overlaps(bounds))
                _children[i].Query(bounds, self, result);
        }
    }

    private FixedBounds2D NodeBounds => new FixedBounds2D(Min.x, Min.y, Max.x, Max.y);

    private bool OverlapsMap(FixedCollider2D collider)
    {
        return NodeBounds.Overlaps(collider.GetBounds());
    }

    private void Split()
    {
        Fixed64 midX = (Min.x + Max.x) / 2;
        Fixed64 midY = (Min.y + Max.y) / 2;
        _children = new QuadTree[4];
        _children[0] = new QuadTree(new FixedVector2(Min.x, midY), new FixedVector2(midX, Max.y), _depth + 1);
        _children[1] = new QuadTree(new FixedVector2(midX, midY), new FixedVector2(Max.x, Max.y), _depth + 1);
        _children[2] = new QuadTree(new FixedVector2(Min.x, Min.y), new FixedVector2(midX, midY), _depth + 1);
        _children[3] = new QuadTree(new FixedVector2(midX, Min.y), new FixedVector2(Max.x, midY), _depth + 1);
    }

    private void Redistribute()
    {
        for (int i = _objects.Count - 1; i >= 0; i--)
        {
            var obj = _objects[i];
            int mask = GetOverlappingChildMask(obj.GetBounds());
            if (mask != 0 && IsSingleChild(mask))
            {
                _objects.RemoveAt(i);
                _children[FirstChildIndex(mask)].Insert(obj);
            }
        }
    }

    private FixedBounds2D ChildBounds(int index)
    {
        Fixed64 midX = (Min.x + Max.x) / 2;
        Fixed64 midY = (Min.y + Max.y) / 2;
        return index switch
        {
            0 => new FixedBounds2D(Min.x, midY, midX, Max.y),
            1 => new FixedBounds2D(midX, midY, Max.x, Max.y),
            2 => new FixedBounds2D(Min.x, Min.y, midX, midY),
            3 => new FixedBounds2D(midX, Min.y, Max.x, midY),
            _ => NodeBounds
        };
    }

    private int GetOverlappingChildMask(in FixedBounds2D b)
    {
        int mask = 0;
        for (int i = 0; i < 4; i++)
        {
            if (ChildBounds(i).Overlaps(b))
                mask |= 1 << i;
        }
        return mask;
    }

    private static bool IsSingleChild(int mask) => (mask & (mask - 1)) == 0;

    private static int FirstChildIndex(int mask)
    {
        int idx = 0;
        while ((mask >> idx & 1) == 0) idx++;
        return idx;
    }
}