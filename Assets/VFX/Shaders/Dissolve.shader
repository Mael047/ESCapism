// Shader de disolución para enemigos (URP).
// _DustAmount: 0 = textura original, 1 = color de polvo
// _Dissolve:   0 = intacto, 1 = desintegrado por completo
Shader "ESCapism/Dissolve"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        [Header(Polvo)]
        _DustColor ("Dust Color", Color) = (0.45, 0.42, 0.5, 1)
        _DustAmount ("Dust Amount", Range(0, 1)) = 0

        [Header(Disolucion)]
        _NoiseMap ("Noise Map", 2D) = "gray" {}
        _NoiseScale ("Noise Scale", Float) = 1
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _EdgeWidth ("Edge Width", Range(0, 0.3)) = 0.08
        [HDR] _EdgeColor ("Edge Color", Color) = (2.5, 1.5, 3, 1)

        [Header(Luz)]
        _Ambient ("Ambient", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
        TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _DustColor;
            half _DustAmount;
            float4 _NoiseMap_ST;
            float _NoiseScale;
            half _Dissolve;
            half _EdgeWidth;
            half4 _EdgeColor;
            half _Ambient;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float2 uv         : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
            float3 normalWS   : TEXCOORD1;
        };

        Varyings vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            return output;
        }

        // Devuelve cuánto le falta a este píxel para desaparecer (negativo = ya desapareció)
        half DissolveMargin(float2 uv)
        {
            half noise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uv * _NoiseScale).r;
            // Se escala para que con _Dissolve = 1 también desaparezca el borde
            half threshold = _Dissolve * (1.0h + _EdgeWidth);
            return noise - threshold;
        }
        ENDHLSL

        // Pase principal
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            half4 frag(Varyings input, FRONT_FACE_TYPE isFront : FRONT_FACE_SEMANTIC) : SV_Target
            {
                half margin = DissolveMargin(input.uv);
                clip(margin);

                // Color: de la textura original al color de polvo
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 albedo = lerp(tex.rgb, _DustColor.rgb, _DustAmount);

                // Luz simple para que no se vea plano
                float3 normal = normalize(input.normalWS) * IS_FRONT_VFACE(isFront, 1.0, -1.0);
                Light mainLight = GetMainLight();
                half ndl = saturate(dot(normal, mainLight.direction));
                half3 color = albedo * mainLight.color * (ndl * (1.0h - _Ambient) + _Ambient);

                // Borde brillante donde se está desintegrando
                if (_Dissolve > 0.001h)
                {
                    half edge = 1.0h - smoothstep(0.0h, _EdgeWidth, margin);
                    color = lerp(color, _EdgeColor.rgb, edge);
                }

                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        // Profundidad (necesario si el proyecto usa depth prepass)
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragDepth

            half4 fragDepth(Varyings input) : SV_Target
            {
                clip(DissolveMargin(input.uv));
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
