Shader "Custom/Ball Fresnell"
{
    Properties
    {
        _BaseColor      ("Base Color",      Color)  = (0.1, 0.4, 1.0, 1.0)
        _FresnelColor   ("Fresnel Color",   Color)  = (0.0, 1.0, 1.0, 1.0)
        _FresnelPower   ("Fresnel Power",   Range(0.5, 8.0)) = 3.0
        _FresnelIntensity ("Fresnel Intensity", Range(0.0, 5.0)) = 2.5
        _EmissionStrength ("Emission Strength", Range(0.0, 5.0)) = 1.5
    }

    SubShader
    {
        // URP: renderizado opaco con soporte de luz
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

            // Includes de URP necesarios para transformaciones y luz
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ── Propiedades expuestas al inspector ──────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _FresnelColor;
                float  _FresnelPower;
                float  _FresnelIntensity;
                float  _EmissionStrength;
            CBUFFER_END

            // ── Estructuras de vértice / fragmento ──────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;   // posición en espacio objeto
                float3 normalOS   : NORMAL;     // normal en espacio objeto
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION; // posición clip-space
                float3 normalWS    : TEXCOORD0;   // normal mundo (para Fresnel)
                float3 viewDirWS   : TEXCOORD1;   // dirección vista (para Fresnel)
            };

            // ── Vertex shader ───────────────────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Transformar posición al clip space
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Transformar la normal al espacio mundo (sin traslación)
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);

                // Vector de la superficie hacia la cámara en espacio mundo
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.viewDirWS = normalize(GetCameraPositionWS() - posWS);

                return OUT;
            }

            // ── Fragment shader ─────────────────────────────────────────────
            half4 frag(Varyings IN) : SV_Target
            {
                // Normalizar vectores interpolados (pueden perder magnitud unitaria)
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);

                // ── Cálculo Fresnel ────────────────────────────────────────
                // dot(N,V) ≈ 1 en frente, ≈ 0 en el borde → invertimos para
                // que el brillo neón aparezca en los bordes del objeto.
                float NdotV      = saturate(dot(N, V));
                float fresnel    = pow(1.0 - NdotV, _FresnelPower);

                // ── Color final ────────────────────────────────────────────
                // Mezcla del color base con el color neón ponderada por el Fresnel
                float3 baseRGB    = _BaseColor.rgb;
                float3 fresnelRGB = _FresnelColor.rgb * fresnel * _FresnelIntensity;

                // Añadir emisión global para reforzar el look neón
                float3 emission   = baseRGB * _EmissionStrength;

                float3 finalColor = baseRGB + fresnelRGB + emission;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
