Shader "InformationString/FloatingLines"
{
    Properties
    {
        // Wave toggles
        [Toggle] _EnableTop     ("Enable Top Wave",    Float) = 1
        [Toggle] _EnableMiddle  ("Enable Middle Wave", Float) = 1
        [Toggle] _EnableBottom  ("Enable Bottom Wave", Float) = 1

        // Per-wave line counts
        _TopLineCount    ("Top Line Count",    Int)   = 6
        _MiddleLineCount ("Middle Line Count", Int)   = 6
        _BottomLineCount ("Bottom Line Count", Int)   = 6

        // Per-wave line spacing (pre-scaled by 0.01 on the C# side)
        _TopLineDistance    ("Top Line Distance",    Float) = 0.05
        _MiddleLineDistance ("Middle Line Distance", Float) = 0.05
        _BottomLineDistance ("Bottom Line Distance", Float) = 0.05

        // Wave position/rotation  (x, y, rotate)
        _TopWavePosition    ("Top Wave Position",    Vector) = (10.0, 0.5,  -0.4, 0)
        _MiddleWavePosition ("Middle Wave Position", Vector) = (5.0,  0.0,   0.2, 0)
        _BottomWavePosition ("Bottom Wave Position", Vector) = (2.0, -0.7,  -1.0, 0)

        // Animation
        _AnimationSpeed ("Animation Speed", Float) = 1.0

        // Mouse / interaction
        [Toggle] _Interactive ("Interactive", Float) = 1
        _BendRadius   ("Bend Radius",   Float) = 5.0
        _BendStrength ("Bend Strength", Float) = -0.5
        _BendInfluence ("Bend Influence", Float) = 0.0   // driven by C# lerp

        // Parallax
        [Toggle] _Parallax     ("Parallax",          Float) = 1
        _ParallaxOffset ("Parallax Offset", Vector) = (0, 0, 0, 0)

        // Gradient (up to 8 stops, driven from C#)
        _LineGradientCount ("Gradient Stop Count", Int) = 0
        _LineGradient0 ("Gradient 0", Color) = (1,1,1,1)
        _LineGradient1 ("Gradient 1", Color) = (1,1,1,1)
        _LineGradient2 ("Gradient 2", Color) = (1,1,1,1)
        _LineGradient3 ("Gradient 3", Color) = (1,1,1,1)
        _LineGradient4 ("Gradient 4", Color) = (1,1,1,1)
        _LineGradient5 ("Gradient 5", Color) = (1,1,1,1)
        _LineGradient6 ("Gradient 6", Color) = (1,1,1,1)
        _LineGradient7 ("Gradient 7", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0
            #include "UnityCG.cginc"

            // ---- uniforms ----
            float  _EnableTop;
            float  _EnableMiddle;
            float  _EnableBottom;

            int    _TopLineCount;
            int    _MiddleLineCount;
            int    _BottomLineCount;

            float  _TopLineDistance;
            float  _MiddleLineDistance;
            float  _BottomLineDistance;

            float4 _TopWavePosition;
            float4 _MiddleWavePosition;
            float4 _BottomWavePosition;

            float  _AnimationSpeed;

            float  _Interactive;
            float  _BendRadius;
            float  _BendStrength;
            float  _BendInfluence;

            float  _Parallax;
            float4 _ParallaxOffset;

            int    _LineGradientCount;
            float4 _LineGradient0;
            float4 _LineGradient1;
            float4 _LineGradient2;
            float4 _LineGradient3;
            float4 _LineGradient4;
            float4 _LineGradient5;
            float4 _LineGradient6;
            float4 _LineGradient7;

            // Resolution + mouse are set each frame by FloatingLinesRenderer
            float2 _Resolution;
            float2 _MousePos;   // pixel coords, y-flipped to match GLSL convention

            // ---- vertex ----
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            // ---- helpers ----
            float2x2 Rotate(float r)
            {
                float c = cos(r), s = sin(r);
                return float2x2(c, s, -s, c);
            }

            static const float3 PINK  = float3(233.0/255.0, 71.0/255.0, 245.0/255.0);
            static const float3 BLUE  = float3( 47.0/255.0, 75.0/255.0, 162.0/255.0);

            float3 BackgroundColor(float2 uv)
            {
                float3 col = float3(0, 0, 0);
                float y = sin(uv.x - 0.2) * 0.3 - 0.1;
                float m = uv.y - y;
                col += lerp(BLUE, float3(0,0,0), smoothstep(0.0, 1.0, abs(m)));
                col += lerp(PINK, float3(0,0,0), smoothstep(0.0, 1.0, abs(m - 0.8)));
                return col * 0.5;
            }

            float3 GradientColor(float t)
            {
                // Sample from the 8-slot gradient array
                float4 stops[8];
                stops[0] = _LineGradient0;
                stops[1] = _LineGradient1;
                stops[2] = _LineGradient2;
                stops[3] = _LineGradient3;
                stops[4] = _LineGradient4;
                stops[5] = _LineGradient5;
                stops[6] = _LineGradient6;
                stops[7] = _LineGradient7;

                if (_LineGradientCount <= 0) return float3(1,1,1);
                if (_LineGradientCount == 1) return stops[0].rgb * 0.5;

                float clampedT = clamp(t, 0.0, 0.9999);
                float scaled   = clampedT * float(_LineGradientCount - 1);
                int   idx      = (int)floor(scaled);
                float f        = frac(scaled);
                int   idx2     = min(idx + 1, _LineGradientCount - 1);
                return lerp(stops[idx].rgb, stops[idx2].rgb, f) * 0.5;
            }

            float Wave(float2 uv, float offset, float2 screenUv, float2 mouseUv, bool shouldBend)
            {
                float time = _Time.y * _AnimationSpeed;

                float xOffset   = offset;
                float xMovement = time * 0.1;
                float amp       = sin(offset + time * 0.2) * 0.3;
                float y         = sin(uv.x + xOffset + xMovement) * amp;

                if (shouldBend)
                {
                    float2 d = screenUv - mouseUv;
                    float influence = exp(-dot(d, d) * _BendRadius);
                    float bendOffset = (mouseUv.y - screenUv.y) * influence * _BendStrength * _BendInfluence;
                    y += bendOffset;
                }

                float m = uv.y - y;
                return 0.0175 / max(abs(m) + 0.01, 1e-3) + 0.01;
            }

            // ---- fragment ----
            fixed4 frag(v2f i) : SV_Target
            {
                // Reconstruct fragCoord from UV + resolution
                float2 fragCoord = i.uv * _Resolution;
                // GLSL-style NDC: (2*fragCoord - res) / res.y, y flipped
                float2 baseUv = (2.0 * fragCoord - _Resolution) / _Resolution.y;
                baseUv.y *= -1.0;

                if (_Parallax > 0.5)
                    baseUv += _ParallaxOffset.xy;

                float3 col = float3(0, 0, 0);
                float3 b   = (_LineGradientCount > 0)
                             ? float3(0,0,0)
                             : BackgroundColor(baseUv);

                float2 mouseUv = float2(0, 0);
                if (_Interactive > 0.5)
                {
                    mouseUv = (2.0 * _MousePos - _Resolution) / _Resolution.y;
                    mouseUv.y *= -1.0;
                }

                bool interactive = (_Interactive > 0.5);

                // --- Bottom wave ---
                if (_EnableBottom > 0.5)
                {
                    for (int i2 = 0; i2 < _BottomLineCount; ++i2)
                    {
                        float fi = float(i2);
                        float t  = fi / max(float(_BottomLineCount - 1), 1.0);
                        float3 lineCol = (_LineGradientCount > 0) ? GradientColor(t) : b;

                        float angle = _BottomWavePosition.z * log(length(baseUv) + 1.0);
                        float2 ruv  = mul(Rotate(angle), baseUv);
                        col += lineCol * Wave(
                            ruv + float2(_BottomLineDistance * fi + _BottomWavePosition.x, _BottomWavePosition.y),
                            1.5 + 0.2 * fi,
                            baseUv, mouseUv, interactive
                        ) * 0.2;
                    }
                }

                // --- Middle wave ---
                if (_EnableMiddle > 0.5)
                {
                    for (int i3 = 0; i3 < _MiddleLineCount; ++i3)
                    {
                        float fi = float(i3);
                        float t  = fi / max(float(_MiddleLineCount - 1), 1.0);
                        float3 lineCol = (_LineGradientCount > 0) ? GradientColor(t) : b;

                        float angle = _MiddleWavePosition.z * log(length(baseUv) + 1.0);
                        float2 ruv  = mul(Rotate(angle), baseUv);
                        col += lineCol * Wave(
                            ruv + float2(_MiddleLineDistance * fi + _MiddleWavePosition.x, _MiddleWavePosition.y),
                            2.0 + 0.15 * fi,
                            baseUv, mouseUv, interactive
                        );
                    }
                }

                // --- Top wave ---
                if (_EnableTop > 0.5)
                {
                    for (int i4 = 0; i4 < _TopLineCount; ++i4)
                    {
                        float fi = float(i4);
                        float t  = fi / max(float(_TopLineCount - 1), 1.0);
                        float3 lineCol = (_LineGradientCount > 0) ? GradientColor(t) : b;

                        float angle = _TopWavePosition.z * log(length(baseUv) + 1.0);
                        float2 ruv  = mul(Rotate(angle), baseUv);
                        ruv.x *= -1.0;
                        col += lineCol * Wave(
                            ruv + float2(_TopLineDistance * fi + _TopWavePosition.x, _TopWavePosition.y),
                            1.0 + 0.2 * fi,
                            baseUv, mouseUv, interactive
                        ) * 0.1;
                    }
                }

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }

    FallBack Off
}
