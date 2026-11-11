Shader "SkyHopper/GradientSky"
{
    Properties
    {
        _TopColor ("Zenith Color", Color) = (0.13, 0.40, 0.85, 1)
        _HorizonColor ("Horizon Color", Color) = (0.78, 0.91, 1, 1)
        _BottomColor ("Deep Sky Color", Color) = (0.42, 0.70, 0.94, 1)
        _CloudColor ("Cloud Color", Color) = (1, 0.99, 0.97, 1)
        _CloudShadow ("Cloud Shadow", Color) = (0.72, 0.83, 0.94, 1)
        _SunColor ("Sun Color", Color) = (1, 0.96, 0.84, 1)
        _SunDirection ("Sun Direction", Vector) = (0.25, 0.32, 0.9, 0)
        _SunSize ("Sun Size", Range(0.001, 0.3)) = 0.03
        _SunGlow ("Sun Glow", Range(0, 1)) = 0.35
        _CloudCover ("Cloud Cover", Range(0, 1)) = 0.55
        _CloudScale ("Cloud Scale", Range(0.2, 8)) = 1.6
        _CloudSpeed ("Cloud Drift", Range(0, 0.2)) = 0.01
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _TopColor;
            fixed4 _HorizonColor;
            fixed4 _BottomColor;
            fixed4 _CloudColor;
            fixed4 _CloudShadow;
            fixed4 _SunColor;
            float4 _SunDirection;
            float _SunSize;
            float _SunGlow;
            float _CloudCover;
            float _CloudScale;
            float _CloudSpeed;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float hash2(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash2(i);
                float b = hash2(i + float2(1.0, 0.0));
                float c = hash2(i + float2(0.0, 1.0));
                float d = hash2(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float amp = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    v += amp * valueNoise(p);
                    p = p * 2.02 + 7.3;
                    amp *= 0.5;
                }
                return v;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float h = dir.y;

                // Sky dome: pale at the horizon fading to a deep blue zenith.
                float upT = pow(saturate(h), 0.5);
                float3 col = lerp(_HorizonColor.rgb, _TopColor.rgb, upT);

                // Everything below eye level opens onto open sky and a
                // cloud deck, so the floating platforms sit high above
                // the world instead of over a solid ground colour.
                float down = saturate(-h);

                // Bright atmospheric haze in a soft band just below the
                // horizon line (not above it).
                float haze = smoothstep(0.0, 0.10, down) * (1.0 - smoothstep(0.10, 0.45, down));
                col = lerp(col, _HorizonColor.rgb, haze * 0.65);

                // Cloud deck projected onto a plane far below the camera, so
                // the level reads as floating high above a world of clouds.
                float2 cuv = (dir.xz / max(down, 0.10)) * _CloudScale + 37.0;
                cuv += float2(_Time.x * _CloudSpeed, _Time.x * _CloudSpeed * 0.37);

                float n  = fbm(cuv * 0.30 + 5.0);      // large cloud masses
                float d  = fbm(cuv * 1.00 + 21.0);     // puffy edges
                float density = n + (d - 0.47) * 0.16;

                float lo = 0.42 - _CloudCover * 0.10;
                float hi = 0.58 - _CloudCover * 0.06;
                float mask = smoothstep(lo, hi, density);
                float cloud = mask
                            * smoothstep(0.07, 0.26, down)          // fade in below the horizon
                            * lerp(0.55, 1.0, saturate(fbm(cuv * 0.12 + 13.7))); // break up the deck

                // Deeper blue in the gaps so the white clouds pop.
                float3 baseBelow = lerp(col, _BottomColor.rgb, smoothstep(0.10, 0.80, down) * 0.9);
                float3 cloudCol = lerp(_CloudShadow.rgb, _CloudColor.rgb, mask);
                col = lerp(baseBelow, cloudCol, cloud * 0.95);

                // Soft sun with a wide glow.
                float3 sunDir = normalize(_SunDirection.xyz);
                float sunDot = saturate(dot(dir, sunDir));
                float disc = smoothstep(1.0 - _SunSize, 1.0 - _SunSize * 0.35, sunDot);
                float glow = pow(sunDot, 24.0);
                col += _SunColor.rgb * (disc * 1.1 + glow * _SunGlow);

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
