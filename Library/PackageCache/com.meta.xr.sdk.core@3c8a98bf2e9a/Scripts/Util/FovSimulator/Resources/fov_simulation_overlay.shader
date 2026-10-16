Shader "qxr/fov_simulation_overlay"
{
    SubShader
    {
        Tags
        {
            "IgnoreProjector" = "True"
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "FOV_SIMULATION_OVERLAY"
            Blend One Zero
            ZWrite Off
            ZTest Always
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            float4 _InnerBounds;
            float4 _OuterBounds;
            float _Alpha;
            float _IsRight;
            float _EnableOuterFrame;
            float _EyeOffsetX;
            float _UvScale;
            float _CornerRadius;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            float RoundedRectangleDistance(float2 position, float4 bounds, float radiusRatio)
            {
                float2 center = float2(
                    (bounds.x + bounds.y) * 0.5,
                    (bounds.z + bounds.w) * 0.5);
                float2 halfSize = float2(
                    (bounds.y - bounds.x) * 0.5,
                    (bounds.w - bounds.z) * 0.5);
                float radius = min(halfSize.x, halfSize.y) * clamp(radiusRatio, 0.0, 0.5);
                float2 offset = abs(position - center) - halfSize + radius;
                return length(max(offset, 0.0)) + min(max(offset.x, offset.y), 0.0) - radius;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 position = (input.uv - 0.5) * _UvScale;
                position.x += _EyeOffsetX;
                if (_IsRight > 0.5)
                {
                    position.x = -position.x;
                }

                float insideInner = step(
                    RoundedRectangleDistance(position, _InnerBounds, _CornerRadius),
                    0.0);
                float insideOuter = step(
                    RoundedRectangleDistance(position, _OuterBounds, _CornerRadius),
                    0.0);
                float visible = _EnableOuterFrame > 0.5
                    ? max(insideInner, 1.0 - insideOuter)
                    : insideInner;
                float alpha = (1.0 - visible) * saturate(_Alpha);
                return fixed4(0.0, 0.0, 0.0, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
