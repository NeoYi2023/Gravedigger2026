Shader "Gravedigger/Coc/FogSoft"
{
    Properties
    {
        [PerRendererData] _MainTex ("Cells", 2D) = "black" {}
        _GroupTex ("Groups", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="False"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _GroupTex;
            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float4 CellUv(float2 texelCenter)
            {
                return float4(texelCenter * _MainTex_TexelSize.xy, 0, 0);
            }

            int ReadGroup(float2 texelCenter)
            {
                float r = tex2Dlod(_GroupTex, CellUv(texelCenter)).r;
                return (int)(r * 255.0 + 0.5);
            }

            float ReadAlpha(float2 texelCenter)
            {
                return tex2Dlod(_MainTex, CellUv(texelCenter)).a;
            }

            // One texel is one 0.25 cell. Radius 2 texels is the 0.5 ground fade.
            // _GroupTex is linear 8-bit (0 = outside, FogGroupId+1 otherwise).
            fixed4 frag(v2f i) : SV_Target
            {
                float2 fragTexel = i.uv * _MainTex_TexelSize.zw;
                float2 homeCenter = float2(floor(fragTexel.x), floor(fragTexel.y)) + 0.5;
                int homeGroup = ReadGroup(homeCenter);

                float alphas[25];
                int groups[25];
                float weights[25];
                int n = 0;
                for (int y = -2; y <= 2; y++)
                {
                    for (int x = -2; x <= 2; x++)
                    {
                        float2 center = float2(floor(fragTexel.x) + x, floor(fragTexel.y) + y) + 0.5;
                        float dist = length(fragTexel - center);
                        alphas[n] = ReadAlpha(center);
                        groups[n] = ReadGroup(center);
                        weights[n] = dist > 2.0 ? 0 : (1.0 - dist * 0.5);
                        n++;
                    }
                }

                float outAlpha = 0;
                if (homeGroup > 0)
                {
                    float acc = 0;
                    float weightSum = 0;
                    for (int s = 0; s < 25; s++)
                    {
                        float a = groups[s] == homeGroup ? alphas[s] : 0;
                        acc += a * weights[s];
                        weightSum += weights[s];
                    }

                    outAlpha = weightSum > 0 ? acc / weightSum : 0;
                }
                else
                {
                    float best = 0;
                    for (int cand = 0; cand < 25; cand++)
                    {
                        int g = groups[cand];
                        if (g <= 0 || weights[cand] <= 0)
                        {
                            continue;
                        }

                        int seen = 0;
                        for (int prev = 0; prev < cand; prev++)
                        {
                            if (groups[prev] == g && weights[prev] > 0)
                            {
                                seen = 1;
                                break;
                            }
                        }

                        if (seen > 0)
                        {
                            continue;
                        }

                        float acc = 0;
                        float weightSum = 0;
                        for (int s = 0; s < 25; s++)
                        {
                            float a = groups[s] == g ? alphas[s] : 0;
                            acc += a * weights[s];
                            weightSum += weights[s];
                        }

                        float groupAlpha = weightSum > 0 ? acc / weightSum : 0;
                        best = max(best, groupAlpha);
                    }

                    outAlpha = best;
                }

                fixed4 c = fixed4(0, 0, 0, outAlpha) * i.color;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
