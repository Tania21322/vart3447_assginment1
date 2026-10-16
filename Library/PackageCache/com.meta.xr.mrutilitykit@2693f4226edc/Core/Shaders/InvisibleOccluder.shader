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

Shader "Meta/MRUK/MixedReality/InvisibleOccluder" {
    SubShader
    {
        Tags{ "RenderPipeline" = "UniversalPipeline"  "Queue" = "Transparent"}

        Pass
        {
            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Universal Render Pipeline
            Name "ForwardLit"

            Tags{"LightMode" = "UniversalForward"}

            Cull off
            ZWrite On
            ZTest Less
            Blend Zero One, Zero One

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard SRP library
            // All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma multi_compile_instancing

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            ENDHLSL
        }
    }
}
