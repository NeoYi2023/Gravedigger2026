Shader "Gravedigger/Coc/FogSoft"
{
    Properties
    {
        [PerRendererData] _MainTex ("Cells", 2D) = "black" {}
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
            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            // One texel is one 0.25 cell. Radius 2 texels is the 0.5 ground fade.
            fixed4 frag(v2f i) : SV_Target
            {
                float2 fragTexel = i.uv * _MainTex_TexelSize.zw;
                float acc = 0;
                float weightSum = 0;
                for (int y = -2; y <= 2; y++)
                {
                    for (int x = -2; x <= 2; x++)
                    {
                        float2 center = float2(floor(fragTexel.x) + x, floor(fragTexel.y) + y) + 0.5;
                        float dist = length(fragTexel - center);
                        if (dist > 2.0)
                        {
                            continue;
                        }

                        float weight = 1.0 - dist * 0.5;
                        float alpha = tex2D(_MainTex, center * _MainTex_TexelSize.xy).a;
                        acc += alpha * weight;
                        weightSum += weight;
                    }
                }

                float outAlpha = weightSum > 0 ? acc / weightSum : 0;
                fixed4 c = fixed4(0, 0, 0, outAlpha) * i.color;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
