/*
This shader is used to render the impostor while clearing out the alpha of the eye buffer
The important details here are:
- the use of the Geometry+1 queue to make sure the impostor is drawn after all other opaque objects but before alpha
- the keepalpha parameter that allow unity to actually write the alpha we return at the end of the shader.

URP SubShader notes:
- Surface shaders are Built-in RP only; URP requires an HLSL pass tagged with RenderPipeline=UniversalPipeline.
- The URP pass below is intentionally unlit: alpha is forced to 0 so the compositor replaces the
  fragment with the underlay layer; the lit appearance of the BiRP path is invisible at runtime.
*/

Shader "Oculus/Underlay Impostor" {
    Properties{
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }

    // URP SubShader — picked when the Universal Render Pipeline is active.
    SubShader{
        PackageRequirements
        {
            "com.unity.render-pipelines.universal": "12.1" // 2021.3+
        }

        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+1" "RenderType" = "Opaque" }
        LOD 200

        Pass{
            Name "UnderlayImpostorURP"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back
            Blend Off
            ColorMask RGBA

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                float _Glossiness;
                float _Metallic;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN) {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;
                return half4(c.rgb, 0.0h);
            }
            ENDHLSL
        }
    }

    // Built-in RP SubShader — preserved as-is.
    SubShader{
        Tags { "Queue" = "Geometry+1" "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows keepalpha

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        void surf(Input IN, inout SurfaceOutputStandard o) {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 0.f;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
