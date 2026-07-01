using FixMath;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FuncTestSt : MonoBehaviour
{
    public EdgeCollider2D edge;

    private FixedEdgeCollider2D fixedEdge;
    private void Start()
    {
        if (edge == null)
        {
            Debug.LogError("EdgeCollider2D reference is missing!");
            return;
        }
        List<FixedVector2> fixedPoints = new List<FixedVector2>();
        foreach (var point in edge.points)
        {
            fixedPoints.Add(new FixedVector2(point.x, point.y));
        }
        fixedEdge = new FixedEdgeCollider2D(new FixedVector2(edge.transform.position.x, edge.transform.position.y), new FixedVector2(edge.offset.x, edge.offset.y), fixedPoints,0.ToFixed(),false);
        fixedEdge.Layer = (int)CollisionLayer.Wall;
        CollisionManager.Instance.AddStaticCollider(fixedEdge);
        if (fixedEdge != null) Debug.Log("Ç½±ÚÒÑ×¢²á");
    }

}
