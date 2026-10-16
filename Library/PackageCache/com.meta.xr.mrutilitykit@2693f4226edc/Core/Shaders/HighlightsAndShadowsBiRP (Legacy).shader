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

Shader "Meta/MRUK/Scene/HighlightsAndShadowsBiRP (Legacy)"
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
            "Queue"="AlphaTest"
        }

        //Accumulate point light contribution
        Pass
        {
            Name "PointLight Contribution"
            Tags
            {
                "LightMode" = "ForwardAdd"
            }
            ZWrite Off
            ZTest LEqual
            Blend One OneMinusSrcAlpha

            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION //occl

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "AutoLight.cginc"
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/BiRP/EnvironmentOcclusionBiRP.cginc" //occl

            uniform float _ShadowIntensity;
            uniform float _HighLightAttenuation;
            uniform float _HighlightOpacity;
            uniform float _EnvironmentDepthBias;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                LIGHTING_COORDS(2, 3)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            struct Light {
                half3 direction;
                fixed4 color;
                float distanceAttenuation;
            };

            Light getLight(v2f i) {
                Light light;
                float3 dir;

                #if defined(POINT) || defined(POINT_COOKIE) || defined(SPOT)
                    dir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
                #else
                    dir = _WorldSpaceLightPos0.xyz;
                #endif

                light.direction = dir;
                light.color = _LightColor0;
                UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos);
                light.distanceAttenuation = attenuation;
                return light;
            }

            fixed4 frag(v2f i) : COLOR {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                Light light = getLight(i);
                float ndtol = max(0.0, dot(i.normal, light.direction));
                float lightContribution = light.distanceAttenuation * _HighLightAttenuation * ndtol * light.color.w;
                float4 color = light.color * lightContribution;
                float occlusionValue = META_DEPTH_GET_OCCLUSION_VALUE_WORLDPOS(i.worldPos, _EnvironmentDepthBias);//occl
                float alpha = lightContribution * _HighlightOpacity;
                fixed4 outputColor = fixed4(color.r, color.g, color.b, alpha);
                outputColor *= occlusionValue;
                return outputColor;
            }
            ENDCG
        }

        //Apply shadow attenuation for the main directionalLight
        Pass
        {
            Name "DirectionalShadows"
            Tags
            {
                "LightMode" = "ForwardBase"
            }
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION //occl

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/BiRP/EnvironmentOcclusionBiRP.cginc" //occl

            uniform float _ShadowIntensity;
            uniform float _DepthCheckBias;
            uniform float _EnvironmentDepthBias;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                LIGHTING_COORDS(2, 3)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag(v2f i) : COLOR {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float attenuation = UNITY_SHADOW_ATTENUATION(i, i.worldPos);
                float3 lightDirection = _WorldSpaceLightPos0.xyz;
                float ndtol = dot(i.normal, lightDirection);
                int directionCheck = step(0,ndtol);
                float occlusionValue = META_DEPTH_GET_OCCLUSION_VALUE_WORLDPOS(i.worldPos, _EnvironmentDepthBias); //occl

                float alpha = (1 - attenuation) * _ShadowIntensity * directionCheck * occlusionValue;
                return fixed4(0, 0, 0, alpha);
            }
            ENDCG
        }

        // Cast shadows
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma target 2.0

            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local _METALLICGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma skip_variants SHADOWS_SOFT
            #pragma multi_compile_shadowcaster

            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster

            #include "UnityStandardShadow.cginc"

            ENDCG
        }
}
}
