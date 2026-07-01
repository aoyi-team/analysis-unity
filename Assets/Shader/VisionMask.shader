Shader "Unlit/VisionMask"
{
    Properties
    {
        _Radius ("Radius", Range(0, 1)) = 0.2
        _Softness ("Softness", Range(0, 1)) = 0.1
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _MaskColor("Mask Color", Color) = (0, 0, 0, 1) 
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

float _Radius;
float _Softness;
float4 _Center;
float4 _MaskColor;

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
    float2 center = _Center.xy;
    float dist = distance(uv, center);
    float alpha = smoothstep(_Radius - _Softness, _Radius, dist);
    return fixed4(_MaskColor.rgb, alpha);
}
            ENDCG
        }
    }

}
