using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreFollowCharacter : MonoBehaviour
{
    public GameObject CharacterPosition;
    private Transform CharacterPositionTransform;
    public Canvas canvas;
    private RectTransform ScoreRect;
    private void Start()
    {
        CharacterPositionTransform = CharacterPosition.GetComponent<Transform>();
        ScoreRect = gameObject.GetComponent<RectTransform>();
    }
    void Update()
    {
        Vector2 ScreenPosition = Camera.main.WorldToScreenPoint(CharacterPositionTransform.transform.position);
        Vector2 CanvasPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, ScreenPosition, canvas.worldCamera,out CanvasPoint);
        float x = CanvasPoint.x+14;
        float y = (float)(CanvasPoint.y + 75);
        Vector2 loca=new Vector2(x, y);
        ScoreRect.anchoredPosition = loca;
    }
}
