using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VisionMaskMultiController : MonoBehaviour
{
    [Header("摄像机")]
    public Camera targetCamera;

    [Header("默认光圈半径（像素）")]
    public float defaultRadius = 200f;

    [Header("边缘过渡（像素）")]
    public float softness = 50f;

    [Header("最大支持光圈数（<=4）")]
    public int maxLights = 4;

    private RawImage rawImage;
    private Material mat;
    public GameObject A;
    public GameObject B;

    // Shader ID
    private static readonly int RadiusArrID = Shader.PropertyToID("_RadiusArr");
    private static readonly int CenterArrID = Shader.PropertyToID("_CenterArr");
    private static readonly int SoftnessID = Shader.PropertyToID("_Softness");
    private static readonly int CountID = Shader.PropertyToID("_Count");

    void Start()
    {
        rawImage = GetComponent<RawImage>();
        mat = rawImage.material;
        if (targetCamera == null) targetCamera = Camera.main;

    }

    void Update()
    {
        /*SendDataToShader();*/
    }


    /*    void SendDataToShader()
        {
            int count = Mathf.Min(players.Count, maxLights);

            Vector4 radiusV4 = Vector4.zero;
            Vector4 centerV4_xy = Vector4.zero;   // 前两个中心
            Vector4 centerV4_zw = Vector4.zero;   // 后两个中心

            for (int i = 0; i < count; i++)
            {
                Vector2 uv = WorldToUV(players[i].position);
                float r = defaultRadius / Screen.height;

                radiusV4[i] = r;

                if (i < 2) { centerV4_xy[i * 2] = uv.x; centerV4_xy[i * 2 + 1] = uv.y; }
                else { centerV4_zw[(i - 2) * 2] = uv.x; centerV4_zw[(i - 2) * 2 + 1] = uv.y; }
            }

            mat.SetVector(RadiusArrID, radiusV4);
            Vector4[] centerArr = { centerV4_xy, centerV4_zw };
            mat.SetVectorArray(CenterArrID, centerArr);

            mat.SetFloat(SoftnessID, softness / Screen.height);
            mat.SetInt(CountID, count);
            Debug.Log($"玩家数={players.Count}, 实际传入={count}");
        }*/
    /*void SendDataToShader()
    {
        Vector4 radiusV4 = Vector4.zero;          // 4 个半径
        Vector4[] centerArr = new Vector4[2];     // 2 个 float4 足够存 4 个中心

        Vector2 uv = WorldToUV(players[i].position);
        radiusV4[i] = defaultRadius / Screen.height;

        // 前两个中心 → centerArr[0] 的 xy/zw
        if (i < 2)
        {
            if (i == 0) centerArr[0].x = uv.x;
            if (i == 0) centerArr[0].y = uv.y;
            if (i == 1) centerArr[0].z = uv.x;
            if (i == 1) centerArr[0].w = uv.y;
        }
        // 后两个中心 → centerArr[1] 的 xy/zw
        else
        {
            if (i == 2) centerArr[1].x = uv.x;
            if (i == 2) centerArr[1].y = uv.y;
            if (i == 3) centerArr[1].z = uv.x;
            if (i == 3) centerArr[1].w = uv.y;
        }
        mat.SetVector(RadiusArrID, radiusV4);
        mat.SetVectorArray(CenterArrID, centerArr);
        mat.SetFloat(SoftnessID, softness / Screen.height);
        mat.SetInt(CountID, count);

        // 调试用，确认数据
        /*if (count >= 2)
            Debug.Log($"中心1=({centerArr[0].x},{centerArr[0].y}) 中心2=({centerArr[0].z},{centerArr[0].w})");*/
    /*}

    Vector2 WorldToUV(Vector3 worldPos)
    {
        Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);
        RectTransform canvasRT = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPos, null, out localPoint);
        return Rect.PointToNormalized(canvasRT.rect, localPoint);
    }*/
}
