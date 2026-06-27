Shader "Custom/dither1"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ToGradient ("Gradient LUT", 2D) = "black" {}

        _ScaleResolution ("Scale Resolution", Float) = 0
        _TargetResolutionScale ("Target Resolution Scale", Int) = 3
        _ChangeColorDepth ("Change Color Depth", Float) = 0
        _TargetColorDepth ("Target Color Depth", Int) = 5
        _Dithering ("Dithering", Float) = 0
        _EnableRecolor ("Enable Recolor", Float) = 0
        _FlipY ("Flip Y", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "RetroEffectGodotLike"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_ToGradient);
            SAMPLER(sampler_ToGradient);

            float4 _MainTex_TexelSize;

            float _ScaleResolution;
            int _TargetResolutionScale;
            float _ChangeColorDepth;
            int _TargetColorDepth;
            float _Dithering;
            float _EnableRecolor;
            float _FlipY;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            int dithering_pattern(int2 fragcoord)
            {
                const int pattern[16] = {
                    -4, +0, -3, +1,
                    +2, -2, +3, -1,
                    -3, +1, -4, +0,
                    +3, -1, +2, -2
                };

                int x = fragcoord.x & 3;
                int y = fragcoord.y & 3;

                return pattern[y * 4 + x];
            }

            float3 rgb2hsv(float3 rgb)
            {
                float r = rgb.r;
                float g = rgb.g;
                float b = rgb.b;

                float cmax = max(r, max(g, b));
                float cmin = min(r, min(g, b));
                float delta = cmax - cmin;

                float h = 0.0;

                if (delta > 0.0)
                {
                    if (cmax == r)
                    {
                        h = fmod((g - b) / delta, 6.0);
                    }
                    else if (cmax == g)
                    {
                        h = ((b - r) / delta) + 2.0;
                    }
                    else
                    {
                        h = ((r - g) / delta) + 4.0;
                    }

                    h *= 60.0;

                    if (h < 0.0)
                    {
                        h += 360.0;
                    }
                }

                float s = 0.0;

                if (cmax > 0.0)
                {
                    s = delta / cmax;
                }

                return float3(h, s, cmax);
            }

            float3 hsv2rgb(float3 hsv)
            {
                float h = hsv.x;
                float s = hsv.y;
                float v = hsv.z;

                float c = v * s;

                float x = h / 60.0;
                x = fmod(x, 2.0);
                x = abs(x - 1.0);
                x = c * (1.0 - x);

                float m = v - c;

                float3 rgb = float3(0.0, 0.0, 0.0);

                if (h < 60.0)
                {
                    rgb = float3(c, x, 0.0);
                }
                else if (h < 120.0)
                {
                    rgb = float3(x, c, 0.0);
                }
                else if (h < 180.0)
                {
                    rgb = float3(0.0, c, x);
                }
                else if (h < 240.0)
                {
                    rgb = float3(0.0, x, c);
                }
                else if (h < 300.0)
                {
                    rgb = float3(x, 0.0, c);
                }
                else if (h < 360.0)
                {
                    rgb = float3(c, 0.0, x);
                }

                rgb += m;

                return rgb;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv01 = input.uv;

                if (_FlipY > 0.5)
                {
                    uv01.y = 1.0 - uv01.y;
                }

                int2 screenSize = int2(
                    round(1.0 / _MainTex_TexelSize.x),
                    round(1.0 / _MainTex_TexelSize.y)
                );

                int2 fragCoord = int2(uv01 * screenSize);

                fragCoord = clamp(fragCoord, int2(0, 0), screenSize - 1);

                int2 ditherCoord;
                int2 fetchCoord;

                if (_ScaleResolution > 0.5)
                {
                    int scale = max(_TargetResolutionScale, 1);

                    ditherCoord = fragCoord / scale;
                    fetchCoord = ditherCoord * scale;
                }
                else
                {
                    ditherCoord = fragCoord;
                    fetchCoord = fragCoord;
                }

                fetchCoord = clamp(fetchCoord, int2(0, 0), screenSize - 1);

                float3 color = _MainTex.Load(int3(fetchCoord, 0)).rgb;

                if (_EnableRecolor > 0.5)
                {
                    float3 hsv = rgb2hsv(color);

                    float colorPos = hsv.x / 360.0;
                    float3 newColor = SAMPLE_TEXTURE2D(_ToGradient, sampler_ToGradient, float2(colorPos, 0.5)).rgb;

                    float3 newHsv = rgb2hsv(newColor);

                    hsv.x = newHsv.x;

                    color = hsv2rgb(hsv);
                }

                float3 workColor = LinearToSRGB(saturate(color));

int3 c = int3(round(workColor * 255.0));

if (_Dithering > 0.5)
{
    int d = dithering_pattern(ditherCoord);
    c += int3(d, d, d);
}

// 先不要 clamp，尽量贴近 Godot 原版
// c = clamp(c, 0, 255);

float3 finalColor;

if (_ChangeColorDepth > 0.5)
{
    int depth = clamp(_TargetColorDepth, 1, 8);

    c >>= (8 - depth);

    finalColor = float3(c) / float(1 << depth);
}
            else
        {
                finalColor = float3(c) / 256.0;
        }

            finalColor = SRGBToLinear(saturate(finalColor));

            return half4(finalColor, 1.0);
            }

            ENDHLSL
        }
    }
}