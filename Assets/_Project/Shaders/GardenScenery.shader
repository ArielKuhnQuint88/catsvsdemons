Shader "CatsVsDemons/GardenScenery"
{
    Properties
    {
        [MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
        [MainTexture] _BaseMap ("Painted Ground", 2D) = "white" {}
        _UseTexture ("Painted Detail", Range(0,1)) = 0
        _DetailScale ("Detail Scale", Float) = 0.09
        [Enum(None,0,StonePaving,1,RoofTiles,2,Timber,3)] _Pattern ("Surface Detail", Float) = 0
        _WindStrength ("Foliage Breeze", Float) = 0.075
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float4 _BaseMap_ST;
            float _UseTexture;
            float _DetailScale;
            float _Pattern;
            float _WindStrength;
        CBUFFER_END
        float3 GardenPosition(float3 position, float wind)
        {
            float3 world = TransformObjectToWorld(position);
            float sway = sin(world.x * 0.8 + world.z * 0.44 + _Time.y * 1.2);
            world.x += sway * wind * _WindStrength;
            world.z += cos(world.x * 0.6 + _Time.y) * wind * _WindStrength * 0.55;
            return world;
        }
        ENDHLSL
        Pass
        {
            Name "GardenForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            struct Attributes {
                float4 positionOS:POSITION; float3 normalOS:NORMAL;
                float2 uv:TEXCOORD0; half4 color:COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings {
                float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0;
                half3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2;
                half3 color:TEXCOORD3; half fog:TEXCOORD4; float4 shadowCoord:TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output=(Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS=GardenPosition(input.positionOS.xyz,input.color.a);
                output.positionCS=TransformWorldToHClip(output.positionWS);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.uv=input.uv;
                output.color=input.color.rgb;
                output.fog=ComputeFogFactor(output.positionCS.z);
                VertexPositionInputs positions=(VertexPositionInputs)0;
                positions.positionWS=output.positionWS; positions.positionCS=output.positionCS;
                output.shadowCoord=GetShadowCoord(positions);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 albedo=input.color*_BaseColor.rgb;
                half3 painted=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv*_DetailScale).rgb;
                albedo*=lerp(1.0,painted*1.8,_UseTexture*0.55);
                if (_Pattern > 0.5 && _Pattern < 2.5)
                {
                    // Shared world coordinates keep intersecting routes visually identical.
                    float2 paving=input.positionWS.xz*(_Pattern > 1.5 ? float2(2.8,1.8) : float2(1.8,1.25));
                    paving.x+=fmod(floor(paving.y),2.0)*0.5;
                    float2 tile=frac(paving);
                    float seam=min(min(tile.x,1.0-tile.x),min(tile.y,1.0-tile.y));
                    float aa=max(fwidth(seam),0.012);
                    float edge=smoothstep(0.028-aa,0.028+aa,seam);
                    float stone=frac(sin(dot(floor(paving),float2(127.1,311.7)))*43758.5453);
                    albedo*=lerp(_Pattern > 1.5 ? 0.84 : 0.74,1.02+stone*0.1,edge);
                }
                else if (_Pattern > 2.5)
                {
                    float grain=sin(input.positionWS.y*22.0+sin(input.positionWS.x*3.2+input.positionWS.z*2.7)*2.0);
                    albedo*=0.96+grain*0.055;
                }
                half3 normal=normalize(input.normalWS);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord=input.shadowCoord;
                #else
                    float4 shadowCoord=TransformWorldToShadowCoord(input.positionWS);
                #endif
                Light light=GetMainLight(shadowCoord);
                half diffuse=saturate(dot(normal,light.direction));
                half shadow=lerp(0.48,1.0,light.shadowAttenuation);
                half3 lighting=max(SampleSH(normal),half3(0.16,0.19,0.2))+
                    diffuse*shadow*light.color*0.82;
                return half4(MixFog(albedo*lighting,input.fog),1.0);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            float3 _LightDirection; float3 _LightPosition;
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 ShadowVert(Attributes input):SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 world=GardenPosition(input.positionOS.xyz,input.color.a);
                float3 normal=TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 direction=normalize(_LightPosition-world);
                #else
                    float3 direction=_LightDirection;
                #endif
                float4 position=TransformWorldToHClip(ApplyShadowBias(world,normal,direction));
                #if UNITY_REVERSED_Z
                    position.z=min(position.z,UNITY_NEAR_CLIP_VALUE*position.w);
                #else
                    position.z=max(position.z,UNITY_NEAR_CLIP_VALUE*position.w);
                #endif
                return position;
            }
            half4 ShadowFrag():SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 DepthVert(Attributes input):SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformWorldToHClip(GardenPosition(input.positionOS.xyz,input.color.a));
            }
            half4 DepthFrag():SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex NormalsVert
            #pragma fragment NormalsFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; half3 normalWS:TEXCOORD0; };
            Varyings NormalsVert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                output.positionCS=TransformWorldToHClip(GardenPosition(input.positionOS.xyz,input.color.a));
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 NormalsFrag(Varyings input):SV_Target
            {
                half3 normal=NormalizeNormalPerPixel(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct=PackNormalOctQuadEncode(normal);
                    return half4(PackFloat2To888(saturate(oct*0.5+0.5)),0.0);
                #else
                    return half4(normal,0.0);
                #endif
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Lit"
}
