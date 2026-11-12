// Shader para variantes de enemigos (URP).
// Cambia el aspecto de un enemigo sin texturas nuevas:
//  - Hue Shift: rota el tono de la textura (0 = original)
//  - Saturation / Brightness: más o menos color y luz
//  - Tint: multiplica el color final
//  - Rim: brillo en el contorno (con bloom se ve como un aura), que puede latir
//  - Pattern: vetas animadas que recorren el cuerpo (usa una textura de ruido)
Shader "ESCapism/EnemyVariant"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        [Header(Color)]
        _HueShift ("Hue Shift", Range(0, 1)) = 0
        _Saturation ("Saturation", Range(0, 2)) = 1
        _Brightness ("Brightness", Range(0, 2)) = 1
        _Tint ("Tint", Color) = (1, 1, 1, 1)

        [Header(Contorno)]
        [HDR] _RimColor ("Rim Color", Color) = (0, 0, 0, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _PulseSpeed ("Pulse Speed", Float) = 0
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0

        [Header(Vetas)]
        _PatternMap ("Pattern Map (ruido)", 2D) = "black" {}
        [HDR] _PatternColor ("Pattern Color", Color) = (0, 0, 0, 1)
        _PatternScale ("Pattern Scale", Float) = 2
        _PatternSpeed ("Pattern Speed", Float) = 0.2
        _PatternThreshold ("Pattern Threshold", Range(0, 1)) = 0.7

        [Header(Luz)]
        _Ambient ("Ambient", Range(0, 1)) = 0.35
        _SelfIllum ("Self Illumination (no depende de la luz)", Range(0, 1.5)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_BaseMap);    SAMPLER(sampler_BaseMap);
        TEXTURE2D(_PatternMap); SAMPLER(sampler_PatternMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _HueShift;
            half _Saturation;
            half _Brightness;
            half4 _Tint;
            half4 _RimColor;
            half _RimPower;
            half _PulseSpeed;
            half _PulseStrength;
            float4 _PatternMap_ST;
            half4 _PatternColor;
            half _PatternScale;
            half _PatternSpeed;
            half _PatternThreshold;
            half _Ambient;
            half _SelfIllum;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float2 uv         : TEXCOORD0;
        };
        ENDHLSL

        // Pase principal
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            // Rota el tono alrededor del eje gris (fórmula de Rodrigues)
            half3 HueShift(half3 color, half shift)
            {
                const half3 k = half3(0.57735h, 0.57735h, 0.57735h);
                half angle = shift * 6.2831853h;
                half cosA = cos(angle);
                return color * cosA + cross(k, color) * sin(angle) + k * dot(k, color) * (1.0h - cosA);
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // Color: tono, saturación, brillo y tinte
                half3 albedo = HueShift(tex.rgb, _HueShift);
                half luma = dot(albedo, half3(0.299h, 0.587h, 0.114h));
                albedo = lerp(luma.xxx, albedo, _Saturation) * _Brightness * _Tint.rgb;

                // Luz simple
                float3 normal = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                half ndl = saturate(dot(normal, mainLight.direction));
                half3 color = albedo * mainLight.color * (ndl * (1.0h - _Ambient) + _Ambient);

                // Luz propia: se ve igual aunque la escena se oscurezca
                color += albedo * _SelfIllum;

                // Contorno brillante que puede latir
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                half rim = pow(1.0h - saturate(dot(normal, viewDir)), _RimPower);
                half pulse = 1.0h + _PulseStrength * sin(_Time.y * _PulseSpeed);
                color += _RimColor.rgb * rim * pulse;

                // Vetas animadas
                float2 patternUV = input.uv * _PatternScale + float2(_Time.y * _PatternSpeed, _Time.y * _PatternSpeed * 0.6);
                half noise = SAMPLE_TEXTURE2D(_PatternMap, sampler_PatternMap, patternUV).r;
                half vein = smoothstep(_PatternThreshold, saturate(_PatternThreshold + 0.1h), noise);
                color += _PatternColor.rgb * vein;

                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        // Sombras
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            float4 ShadowVert(Attributes input) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                positionCS = ApplyShadowClamping(positionCS);
                return positionCS;
            }

            half4 ShadowFrag() : SV_Target { return 0; }
            ENDHLSL
        }

        // Profundidad (necesario si el proyecto usa depth prepass)
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            float4 DepthVert(Attributes input) : SV_POSITION
            {
                return TransformObjectToHClip(input.positionOS.xyz);
            }

            half4 DepthFrag() : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack Off
}
