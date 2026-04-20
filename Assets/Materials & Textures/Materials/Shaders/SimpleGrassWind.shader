Shader "Custom/TerrainGrass_FixedRotation"
{
    Properties
    {
        _MainTex ("Grass Texture (RGBA)", 2D) = "white" {}
        _Tint ("Grass Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _WindSpeed ("Wind Speed", Float) = 1.0
        _WindStrength ("Wind Strength", Float) = 0.1
    }

    SubShader
    {
        Tags { 
            "Queue"="AlphaTest" 
            "IgnoreProjector"="True" 
            "RenderType"="TransparentCutout" 
            "DisableBatching"="True" // Desativamos o batching para garantir controle de rotação
        }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Tint;
            fixed _Cutoff;
            float _WindSpeed;
            float _WindStrength;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                // --- TRAVA DE ROTAÇÃO ---
                // Extraímos apenas a posição da instância no mundo, descartando a rotação aleatória do Terrain
                float3 worldPos = float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]);
                
                // Reconstrói a matriz de mundo sem a rotação (apenas escala e posição)
                float4x4 fixedMatrix = unity_ObjectToWorld;
                
                // Forçamos os eixos X e Z da matriz a serem neutros (sem rotação local da instância)
                fixedMatrix[0][0] = 1; fixedMatrix[0][1] = 0; fixedMatrix[0][2] = 0;
                fixedMatrix[1][0] = 0; fixedMatrix[1][1] = 1; fixedMatrix[1][2] = 0;
                fixedMatrix[2][0] = 0; fixedMatrix[2][1] = 0; fixedMatrix[2][2] = 1;

                // Cálculo de Vento
                float wind = sin(_Time.y * _WindSpeed + worldPos.x + worldPos.z) * _WindStrength * v.uv.y;
                v.vertex.x += wind;

                // Transforma o vértice usando a nossa matriz de rotação travada
                float4 worldVertex = mul(fixedMatrix, v.vertex);
                o.vertex = mul(UNITY_MATRIX_VP, worldVertex);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 col = tex2D(_MainTex, i.uv);
                col *= _Tint;
                clip(col.a - _Cutoff);
                return col;
            }
            ENDCG
        }
    }
}