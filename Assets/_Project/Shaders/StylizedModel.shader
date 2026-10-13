Shader "CatsVsDemons/StylizedModel"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (0.3, 0.5, 0.7, 1)
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
        _SpecularStrength ("Soft Highlight", Range(0, 1)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
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
            float _SpecularStrength;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirectionWS : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
                float heightOS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                float3 positionWS : TEXCOORD5;
                half fog : TEXCOORD6;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
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
                output.positionWS = positions.positionWS;
                output.fog = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float heightRange =
                    max(_MaxHeight - _MinHeight, 0.0001);
                float normalizedHeight = saturate(
                    (input.heightOS - _MinHeight) / heightRange
                );

                float topMask = smoothstep(
                    _TopStart,
                    max(_TopEnd, _TopStart + 0.0001),
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

                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = input.shadowCoord;
                #else
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                Light mainLight = GetMainLight(shadowCoord);
                half3 normal = normalize(input.normalWS);
                half lightAmount = saturate(
                    dot(normal, mainLight.direction)
                );
                half bandWidth = max(fwidth(lightAmount), 0.025);
                half bands = 0.35 +
                    smoothstep(0.34 - bandWidth, 0.34 + bandWidth, lightAmount) * 0.32 +
                    smoothstep(0.68 - bandWidth, 0.68 + bandWidth, lightAmount) * 0.33;

                half shadow =
                    lerp(0.42, 1.0, mainLight.shadowAttenuation);
                half3 litColor =
                    color * (SampleSH(normal) * 0.55 + bands * shadow * mainLight.color);

                half3 viewDirection =
                    normalize(input.viewDirectionWS);
                half rim = pow(
                    1.0 - saturate(dot(normal, viewDirection)),
                    2.2
                );
                litColor +=
                    _RimColor.rgb * rim * _RimStrength;
                half3 halfDirection = SafeNormalize(mainLight.direction + viewDirection);
                half specular = pow(saturate(dot(normal, halfDirection)), 24.0);
                litColor += specular * _SpecularStrength * mainLight.color * shadow;

                return half4(MixFog(litColor, input.fog), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirection = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirection = _LightDirection;
                #endif
                output.positionCS = TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, lightDirection));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE * output.positionCS.w);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE * output.positionCS.w);
                #endif
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormalsOnly" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex NormalsVert
            #pragma fragment NormalsFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings NormalsVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 NormalsFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 normal = NormalizeNormalPerPixel(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormal = PackNormalOctQuadEncode(normal);
                    half3 packed = PackFloat2To888(saturate(octNormal * 0.5 + 0.5));
                    return half4(packed, 0.0);
                #else
                    return half4(normal, 0.0);
                #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings DepthVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            half4 DepthFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}
