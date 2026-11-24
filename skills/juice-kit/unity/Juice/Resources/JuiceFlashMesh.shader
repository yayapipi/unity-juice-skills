// 閃白用（3D 模型）：整個模型畫成純色 _FlashColor。
// 內建管線與 URP 都能用；多的兩個 Pass 是給 URP 的深度預先處理用的。
Shader "Hidden/Juice/FlashMesh"
{
    Properties
    {
        _FlashColor ("Flash Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _FlashColor;

            float4 vert(float4 vertex : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(vertex);
            }

            fixed4 frag() : SV_Target
            {
                return fixed4(_FlashColor.rgb, 1);
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 vert(float4 vertex : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(vertex);
            }

            fixed4 frag() : SV_Target
            {
                return 0;
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
            };

            v2f vert(float4 vertex : POSITION, float3 normal : NORMAL)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.normal = UnityObjectToWorldNormal(normal);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return float4(normalize(i.normal), 0);
            }
            ENDCG
        }
    }
}
