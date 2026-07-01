Shader "Unlit/VisionMaskMulti"
{
    Properties
    {
        _RadiusArr ("Radius Array", Vector) = (0.2,0.2,0.2,0.2)   // 最多 4 个
        _CenterArr ("Center Array", Vector) = (0.5,0.5,0.7,0.3)   // xy zw 各 2 个
        _Softness ("Softness", Range(0,1)) = 0.05
        _Count    ("Active Count", Int) = 1                       // 实际激活个数
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
LOD 100
        Blend
SrcAlpha OneMinusSrcAlpha

ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
#include "UnityCG.cginc"

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};
struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
};

float4 _RadiusArr; // x,y,z,w 分别存 4 个半径
float4 _CenterArr[2]; // [0] 存 1,2 中心  [1] 存 3,4 中心
float _Softness;
int _Count;

v2f vert(appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    return o;
}

fixed4 frag(v2f i) : SV_Target
{
    float2 uv = i.uv;
    float alpha = 1.0;
    for (int k = 0; k < _Count; k++)
    {
        float2 center = (k < 2) ? _CenterArr[0].xy : _CenterArr[1].xy;
        if (k == 1)
            center = _CenterArr[0].zw;
        if (k == 3)
            center = _CenterArr[1].zw;

        float r = _RadiusArr[k];
        float mask = smoothstep(r - _Softness, r, distance(uv, center));
        alpha *= mask;
    }
    /*float alpha = 1.0; // 初始全黑

                // 最多 4 个光圈，循环做「挖洞」
    for (int k = 0; k < _Count; k++)
    {
        float2 center = 0;
        float radius = _RadiusArr[k];

                    // 前两个中心在 CenterArr[0]，后两个在 CenterArr[1]
        if (k < 2)
            center = _CenterArr[0].xy;
        else
            center = _CenterArr[0].zw;

        float dist = distance(uv, center);
        float mask = smoothstep(radius - _Softness, radius, dist);
        alpha *= mask; // 叠加遮罩：任何一圈外都变黑
    }*/

    return fixed4(0, 0, 0, alpha);
}
            ENDCG
        }
    }
}