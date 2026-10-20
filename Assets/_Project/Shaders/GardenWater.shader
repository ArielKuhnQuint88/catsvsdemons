Shader "CatsVsDemons/GardenWater"
{
    Properties
    {
        [MainColor] _BaseColor ("Water Color", Color) = (0.06, 0.35, 0.45, 1)
        _HighlightColor ("Reflection Color", Color) = (0.55, 0.85, 0.95, 1)
        _WaveScale ("Ripple Scale", Float) = 2.2
        _WaveSpeed ("Ripple Speed", Float) = 0.65
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Name "GardenWater"
            Tags { "LightMode" = "UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _HighlightColor;
                float _WaveScale;
                float _WaveSpeed;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half fog : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.fog = ComputeFogFactor(position.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 p = input.positionWS.xz * _WaveScale;
                float t = _Time.y * _WaveSpeed;
                float waveA = sin(p.x + p.y * 0.7 + t);
                float waveB = cos(p.y * 1.3 - p.x * 0.4 - t * 0.8);
                half3 normal = normalize(half3(waveA * 0.12, 1.0, waveB * 0.12));
                half3 view = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                Light sun = GetMainLight();
                half fresnel = pow(1.0 - saturate(dot(normal, view)), 3.0);
                half specular = pow(saturate(dot(normal, SafeNormalize(sun.direction + view))), 48.0);
                half ripple = smoothstep(0.83, 1.0, (waveA + waveB) * 0.25 + 0.5);
                half3 color = lerp(_BaseColor.rgb, _HighlightColor.rgb, fresnel * 0.55 + ripple * 0.1);
                color += _HighlightColor.rgb * specular * 0.5 * sun.color;
                return half4(MixFog(color, input.fog), 1.0);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Unlit"
}
