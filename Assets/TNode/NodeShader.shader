Shader "Unlit/NodeShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MyColor ("Color", Color) = (1, 1, 1, 1)
        _MainLightDir ("Main Light Direction", Vector) = (0, 0, -1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normalWS : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4x4 MyTRSMatrix;
            fixed4 _MyColor;
            float3 _MainLightDir;

            v2f vert (appdata v)
            {
                v2f o;

                float4 worldPos = mul(MyTRSMatrix, v.vertex);
                o.vertex = mul(UNITY_MATRIX_VP, worldPos);
                o.normalWS = normalize(mul((float3x3)MyTRSMatrix, v.normal));

                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _MyColor;
                float3 L = normalize(_MainLightDir);
                float NdotL = saturate(dot(i.normalWS, -L));
                col.rgb *= NdotL;

                return col;
            }
            ENDCG
        }
    }
}
