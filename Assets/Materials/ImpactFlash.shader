Shader "Custom/Impact Flash"
{
    Properties
    {
        _BaseColor      ("Base Color",      Color)   = (0.06, 0.0, 0.0, 1.0)
        _FlashColor     ("Flash Color",     Color)   = (1.0, 0.4, 0.0, 1.0)
        _FlashIntensity ("Flash Intensity", Range(0.0, 10.0)) = 5.0

        // _FlashProgress se controla desde C# en tiempo de ejecución:
        // 0 = sin impacto, 1 = impacto máximo, decrece hacia 0 con el tiempo
        _FlashProgress  ("Flash Progress",  Range(0.0, 1.0))  = 0.0

        _RimPower       ("Rim Power",       Range(1.0, 8.0))  = 4.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ── Propiedades ─────────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _FlashColor;
                float  _FlashIntensity;
                float  _FlashProgress;
                float  _RimPower;
            CBUFFER_END

            // ── Estructuras ─────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
            };

            // ── Vertex shader ───────────────────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);

                float3 posWS    = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.viewDirWS   = normalize(GetCameraPositionWS() - posWS);
                return OUT;
            }

            // ── Fragment shader ─────────────────────────────────────────────
            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);

                // ── Rim light (bordes iluminados en el impacto) ────────────
                // Igual que Fresnel: brilla en los bordes perpendiculares a la cámara
                float NdotV = saturate(dot(N, V));
                float rim   = pow(1.0 - NdotV, _RimPower);

                // ── Flash radial ───────────────────────────────────────────
                // _FlashProgress baja de 1→0 desde C#; multiplicamos por rim
                // para que el destello sea más intenso en los bordes y decaiga
                // suavemente hacia el centro.
                float flashMask = _FlashProgress * rim;

                // ── Color final ────────────────────────────────────────────
                // Interpolamos entre el color base y el color de flash ponderado
                // por la intensidad del impacto.
                float3 flashContrib = _FlashColor.rgb * flashMask * _FlashIntensity;
                float3 finalColor   = _BaseColor.rgb + flashContrib;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
