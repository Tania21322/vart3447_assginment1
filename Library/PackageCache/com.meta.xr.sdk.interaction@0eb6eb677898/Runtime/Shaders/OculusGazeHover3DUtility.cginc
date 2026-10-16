/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// Shared math, vertex IO, and gradient helpers for the GazeHover3D affordance shaders.
// All three layer shaders (alpha-blend glow, multiplicative contrast boost, inverse darkening)
// include this file and only differ in their fragment program and blend mode.

#ifndef OCULUS_GAZE_HOVER_3D_UTILITY_CGINC
#define OCULUS_GAZE_HOVER_3D_UTILITY_CGINC

half4 _Color;

half _Radius;
half _Falloff;
half _Power;

float4 _Center;
half _Intensity;
float4 _Center2;
half _Intensity2;

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 worldPos : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert(appdata v)
{
    v2f o;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_OUTPUT(v2f, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.pos = UnityObjectToClipPos(v.vertex);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.uv = v.uv;

    return o;
}

// Single-spotlight radial gradient sampled in world space. Output is in [0, 1] before
// the layer-specific intensity multiplier is applied.
half ComputeRadialGradient(v2f i, float3 center)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    // Compute the offset and its length in float precision: half (fp16) on Quest only
    // carries ~3 decimal digits, so calling distance() on raw world-space coords would
    // quantize to ~1.5 cm at 15 m from the world origin, banding the spotlight edge.
    // Subtracting first keeps the difference vector small (within mesh local extent),
    // so the cast-to-half after length() loses no perceptible precision.
    float3 delta = i.worldPos - center;
    half dist = (half)length(delta);

    // Early-out for pixels outside the spotlight's effective range. Since smoothstep
    // saturates to 1.0 above its upper edge, (1 - smoothstep(...)) is exactly 0 there
    // and pow(0, *) is 0 too — so the result is 0 either way.
    if (dist > _Radius + _Falloff)
    {
        return 0.0;
    }

    half gradientFactor = smoothstep(_Radius + _Falloff, _Radius, dist);
    gradientFactor = pow(gradientFactor, _Power);

    return gradientFactor;
}

// Interleaved gradient noise produces a blue-noise-like distribution that hides
// banding in low-contrast gradients. We add a temporal offset so the noise pattern
// does not appear locked to screen space.
half OculusGazeHover3DInterleavedGradientNoise(float2 screenPos)
{
    half3 magic = half3(0.06711056, 0.00583715, 52.9829189);
    return frac(magic.z * frac(dot(screenPos, magic.xy)));
}

// Cheap centered uniform dither in [-0.5/255, +0.5/255] * intensity. Sufficient to
// break up the very faint gradient bands at the edge of the spotlight.
half OculusGazeHover3DUniformDither(float2 screenPos, float intensity)
{
    half temporalOffset = frac(_Time.y * 0.1) * 256.0;
    screenPos += temporalOffset;

    half noise = OculusGazeHover3DInterleavedGradientNoise(screenPos);
    half ditherAmount = (noise - 0.5) * (1.0 / 255.0) * intensity;

    return ditherAmount;
}

#endif // OCULUS_GAZE_HOVER_3D_UTILITY_CGINC
