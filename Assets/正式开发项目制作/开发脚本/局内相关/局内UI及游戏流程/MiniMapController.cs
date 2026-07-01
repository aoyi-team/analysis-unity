using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniMapController : MonoBehaviour
{
    [Header("°ó¶¨")]
    public Transform PlayerTrans;
    public RectTransform MapArea;
    public RectTransform playerDot;
    public Transform MapTrans;

    private SpriteRenderer MapRender;
    private Vector2 WorldMin;
    private Vector2 WorldMax;
    private Vector2 mapSize;
    private void Start()
    {
        MapRender = MapTrans.GetComponentInParent<SpriteRenderer>();
        GetMaxPos();
        mapSize = MapArea.rect.size;
    }
    private void Update()
    {
        UpdateMinMapPosition();
    }
    private void GetMaxPos()
    {
        float MaxPx = MapTrans.position.x + MapRender.sprite.bounds.size.x/2;
        float Minx = MapTrans.position.x - MapRender.sprite.bounds.size.x/2;
        float MaxPy = MapTrans.position.y + MapRender.sprite.bounds.size.y/2;
        float Miny = MapTrans.position.y - MapRender.sprite.bounds.size.y/2;
        WorldMin = new Vector2(Minx,Miny);
        WorldMax = new Vector2(MaxPx,MaxPy);
    }
    void UpdateMinMapPosition()
    {
        Vector2 normalizePos = new(Mathf.InverseLerp(WorldMin.x,WorldMax.x,PlayerTrans.position.x),Mathf.InverseLerp(WorldMin.y,WorldMax.y,PlayerTrans.position.y));;
        Vector2 DotPosition = new(normalizePos.x*mapSize.x,normalizePos.y*mapSize.y);
        playerDot.anchoredPosition = DotPosition;
    }
    
}
