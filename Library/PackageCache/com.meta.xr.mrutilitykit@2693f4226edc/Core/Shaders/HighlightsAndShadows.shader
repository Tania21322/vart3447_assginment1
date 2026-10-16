// Copyright(c) Meta Platforms, Inc. and affiliates.
// All rights reserved.
//
// Licensed under the Oculus SDK License Agreement (the "License");
// you may not use the Oculus SDK except in compliance with the License,
// which is provided at the time of installation or download, or which
// otherwise accompanies this software in either electronic or hard copy form.
//
// You may obtain a copy of the License at
//
// https://developer.oculus.com/licenses/oculussdk/
//
// Unless required by applicable law or agreed to in writing, the Oculus SDK
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

Shader "Meta/MRUK/Scene/HighlightsAndShadows"
{
    Properties
    {
        _ShadowIntensity ("Shadow Intensity", Range (0, 1)) = 0.8
        _HighLightAttenuation ("Highlight Attenuation", Range (0, 1)) = 0.8
        _HighlightOpacity("Highlight Opacity", Range (0, 1)) = 0.2
        _EnvironmentDepthBias("Environment Depth Bias", Range (0, 1)) = 0.06
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline" "Queue"="Transparent"
        }
        Pass
        {

            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend One OneMinusSrcAlpha
            ZTest LEqual
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex ShadowReceiverVertex
            #pragma fragment ShadowReceiverFragment

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            // This multi_compile declaration is required for the Forward+ rendering path
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION //occl

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/URP/EnvironmentOcclusionURP.hlsl" //occl

            CBUFFER_START(UnityPerMaterial)
            float _HighLightAttenuation;
            float _ShadowIntensity;
            float _HighlightOpacity;
            float _EnvironmentDepthBias;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 normalWS : NORMAL;
                float3 positionWS : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ShadowReceiverVertex(Attributes input) {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                const VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalize(mul(unity_ObjectToWorld, float4(input.normal, 0.0)).xyz);
                return output;
            }

            half4 ShadowReceiverFragment(const Varyings input) : SV_Target {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // The Forward+ light loop (LIGHT_LOOP_BEGIN) requires the InputData struct to be in its scope.
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = input.normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                half3 color = half3(0, 0, 0);
                half mainLightShadowAttenuation;

                // Main light shadows.
                VertexPositionInputs vertexInput = (VertexPositionInputs)0;
                vertexInput.positionWS = input.positionWS;
                const float4 shadowCoord = GetShadowCoord(vertexInput);
                mainLightShadowAttenuation = MainLightRealtimeShadow(shadowCoord);
                half alpha = (1 - mainLightShadowAttenuation) * _ShadowIntensity;
				half finalAlpha = alpha;

#if defined(_ADDITIONAL_LIGHTS)
                //Additional lights highlights.
                float lightAlpha = 0;
#if USE_CLUSTER_LIGHT_LOOP
                UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    Light light = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                    float ndtol = saturate(dot(light.direction, input.normalWS));
                    lightAlpha = light.distanceAttenuation * ndtol * _HighLightAttenuation * light.shadowAttenuation;
                    color += light.color * lightAlpha * (1-alpha);
                    finalAlpha += lightAlpha * _HighlightOpacity * (1-alpha);
                }
#endif
                // Additional light loop. The GetAdditionalLightsCount method always returns 0 in Forward+.
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, input.positionWS, float4(0, 0, 0, 0));
                    float ndtol = saturate(dot(light.direction, input.normalWS));
                    lightAlpha = light.distanceAttenuation * ndtol * _HighLightAttenuation * light.shadowAttenuation;
                    color += light.color * lightAlpha * (1-alpha);
                    finalAlpha += lightAlpha * _HighlightOpacity * (1-alpha);
				LIGHT_LOOP_END
#endif
                float occlusionValue = META_DEPTH_GET_OCCLUSION_VALUE_WORLDPOS(input.positionWS, _EnvironmentDepthBias);//occl
                return half4(color * occlusionValue, finalAlpha * occlusionValue);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    Fallback "Off"
}
