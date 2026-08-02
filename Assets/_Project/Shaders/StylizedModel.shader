Shader "CatsVsDemons/StylizedModel"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.3, 0.5, 0.7, 1)
        [MainTexture] _BaseMap ("Albedo Texture", 2D) = "white" {}
        _UseBaseMap ("Use Albedo Texture", Range(0, 1)) = 0
        _SecondaryColor ("Secondary Color", Color) = (0.9, 0.9, 0.9, 1)
        _AccentColor ("Accent Color", Color) = (0.8, 0.1, 0.1, 1)
        _RimColor ("Rim Color", Color) = (1, 0.8, 0.45, 1)
        _MinHeight ("Minimum Height", Float) = 0
        _MaxHeight ("Maximum Height", Float) = 1
        _TopStart ("Top Color Start", Range(0, 1)) = 0.64
        _TopEnd ("Top Color End", Range(0, 1)) = 0.76
        _AccentCenter ("Accent Center", Range(0, 1)) = 0.43
        _AccentWidth ("Accent Width", Range(0.01, 0.4)) = 0.08
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.28
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
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                half4 _SecondaryColor;
                half4 _AccentColor;
                half4 _RimColor;
                float _MinHeight;
                float _MaxHeight;
                float _TopStart;
                float _TopEnd;
                float _AccentCenter;
                float _AccentWidth;
                float _RimStrength;
                float _UseBaseMap;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirectionWS : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
                float heightOS : TEXCOORD3;
                float2 uv : TEXCOORD4;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions =
                    GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = positions.positionCS;
                output.normalWS =
                    TransformObjectToWorldNormal(input.normalOS);
                output.viewDirectionWS =
                    GetWorldSpaceViewDir(positions.positionWS);
                output.shadowCoord =
                    GetShadowCoord(positions);
                output.heightOS = input.positionOS.z;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float heightRange =
                    max(_MaxHeight - _MinHeight, 0.0001);
                float normalizedHeight = saturate(
                    (input.heightOS - _MinHeight) / heightRange
                );

                float topMask = smoothstep(
                    _TopStart,
                    _TopEnd,
                    normalizedHeight
                );
                float accentDistance = abs(
                    normalizedHeight - _AccentCenter
                );
                float accentMask = 1.0 - smoothstep(
                    _AccentWidth * 0.45,
                    _AccentWidth,
                    accentDistance
                );

                half3 color = lerp(
                    _BaseColor.rgb,
                    _SecondaryColor.rgb,
                    topMask
                );
                color = lerp(
                    color,
                    _AccentColor.rgb,
                    accentMask * 0.92
                );

                half3 albedo = SAMPLE_TEXTURE2D(
                    _BaseMap,
                    sampler_BaseMap,
                    input.uv
                ).rgb;
                color = lerp(color, albedo, _UseBaseMap);

                Light mainLight = GetMainLight(input.shadowCoord);
                half3 normal = normalize(input.normalWS);
                half lightAmount = saturate(
                    dot(normal, mainLight.direction)
                );
                half bands =
                    lightAmount > 0.68h ? 1.0h :
                    lightAmount > 0.34h ? 0.72h : 0.48h;

                half shadow =
                    lerp(0.62h, 1.0h, mainLight.shadowAttenuation);
                half3 litColor =
                    color * (0.3h + bands * shadow * mainLight.color);

                half3 viewDirection =
                    normalize(input.viewDirectionWS);
                half rim = pow(
                    1.0h - saturate(dot(normal, viewDirection)),
                    2.2h
                );
                litColor +=
                    _RimColor.rgb * rim * _RimStrength;

                return half4(litColor, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}
