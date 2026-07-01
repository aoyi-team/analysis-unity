Shader "Unlit/Newshader"
{
    Properties
    {
        _Radius ("Radius (Role A)", Range(0, 1)) = 0.2
        _Softness ("Softness (Role A)", Range(0, 1)) = 0.1
        _Center ("Center (Role A)", Vector) = (0.5, 0.5, 0, 0)

        _Radius2 ("Radius (Role B)", Range(0, 1)) = 0.2 // 角色B光圈半径
        _Softness2 ("Softness (Role B)", Range(0, 1)) = 0.1 // 角色B光圈柔和度
        _Center2 ("Center (Role B)", Vector) = (0.5, 0.5, 0, 0) // 角色B光圈中心

        _MaskColor("Mask Color", Color) = (0, 0, 0, 1) 
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
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

            // 角色A光圈参数
            float _Radius;
            float _Softness;
            float4 _Center;
            // 角色B光圈参数
            float _Radius2;
            float _Softness2;
            float4 _Center2;

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

                // 1. 计算角色A光圈的透明值（alpha越大，遮罩越黑）
                float distA = distance(uv, _Center.xy);
                float alphaA = smoothstep(_Radius - _Softness, _Radius, distA);

                // 2. 计算角色B光圈的透明值
                float distB = distance(uv, _Center2.xy);
                float alphaB = smoothstep(_Radius2 - _Softness2, _Radius2, distB);

                // 3. 合并两个光圈：取最小alpha值（关键！）
                // 原理：两个光圈的亮光区域（alpha≈0）会相互保留，遮罩区域（alpha≈1）正常显示
                // 最终效果：两个亮光区域共存，无遮挡，其余区域为黑色遮罩
                float finalAlpha = min(alphaA, alphaB);

                return fixed4(_MaskColor.rgb, finalAlpha * _MaskColor.a);
            }
            ENDCG
        }
    }
}
