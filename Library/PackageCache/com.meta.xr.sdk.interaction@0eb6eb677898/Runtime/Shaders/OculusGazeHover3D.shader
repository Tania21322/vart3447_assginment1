// Copyright (c) Meta Platforms, Inc. and affiliates.
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

Shader "Oculus/Interaction/GazeHover3D"
{
    Properties
    {
        [HideInInspector]_Radius ("Radius", Float) = 1.0
        [HideInInspector]_Falloff ("Falloff", Float) = 1.0
        [HideInInspector]_Power ("Power", Float) = 1.0
        [HideInInspector]_Color ("Color", Color) = (1, 1, 1, 1)

        [HideInInspector]_Center ("Center", Vector) = (0, 0, 0, 0)
        [HideInInspector]_Intensity ("Intensity", Float) = 0.0

        [HideInInspector]_Center2 ("Center 2", Vector) = (0, 0, 0, 0)
        [HideInInspector]_Intensity2 ("Intensity 2", Float) = 0.0

        [HideInInspector]_DitheringAmount ("Dithering Intensity", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "GazeHover3D_AlphaBlend"

            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "OculusGazeHover3DUtility.cginc"

            half _DitheringAmount;

            half4 frag(v2f i) : SV_Target
            {
                half radialGradient1 = ComputeRadialGradient(i, _Center) * _Intensity;
                half radialGradient2 = ComputeRadialGradient(i, _Center2) * _Intensity2;

                half finalGradient = saturate(radialGradient1 + radialGradient2);

                // Early-discard outside the spotlight reach. Skips the dither + color
                // math + framebuffer write for the vast majority of fragments on a
                // large affordance. Has to happen BEFORE the dither addition,
                // otherwise the dither magnitude (~0.008) keeps the alpha-discard
                // threshold (~0.001) from ever firing once the gradient is zero.
                if (finalGradient < 0.001)
                {
                    discard;
                }

                half3 color = _Color.rgb;
                half alpha = finalGradient * _Color.a;
                alpha += OculusGazeHover3DUniformDither(i.pos.xy, _DitheringAmount);

                return half4(color, alpha);
            }
            ENDCG
        }
    }

    FallBack Off
}
