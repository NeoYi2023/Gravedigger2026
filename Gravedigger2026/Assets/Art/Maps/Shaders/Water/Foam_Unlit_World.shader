Shader "Gravedigger/Maps/Foam_Unlit_World"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.14, 0.55, 0.78, 1)
        _DeepColor    ("Deep Color",    Color) = (0.02, 0.22, 0.40, 1)
        _DepthBlendStrength ("Depth Blend Strength (world-Y)", Range(0,2)) = 0.6

        _UVScale  ("Global UV Scale (1/units)", Float) = 0.25
        _DetailTex ("Detail/Base (repeatable)", 2D) = "white" {}
        _DetailInfluence ("Detail Influence", Range(0,1)) = 0.35
        _DetailPan ("Detail Flow (XY)", Vector) = (0.005, 0.004, 0, 0)

        _RippleTex  ("Ripple Noise (repeatable)", 2D) = "gray" {}
        _RipplePan  ("Ripple Flow (XY)", Vector) = (0.02, 0.015, 0, 0)
        _RippleTilingMul ("Ripple Tiling Multiplier", Float) = 1.0
        _RippleAmount ("Ripple Albedo Wobble", Range(0,0.2)) = 0.05
        _UVWobbleAmount ("UV Wobble (distort everything)", Range(0,0.1)) = 0.02

        _NormalA ("Normal A", 2D) = "bump" {}
        _NormalB ("Normal B", 2D) = "bump" {}
        _PanA    ("Normal A Flow (XY)", Vector) = (0.035, 0.010, 0, 0)
        _PanB    ("Normal B Flow (XY)", Vector) = (-0.015, 0.028, 0, 0)
        _NormalTilingMul ("Normals Tiling Multiplier", Float) = 1.0
        _NormalScale ("Distortion Strength", Range(0,2)) = 0.9

        _SheenStrength ("Sheen Strength", Range(0,1)) = 0.25
        _SheenSharpness ("Sheen Sharpness", Range(0.5,8)) = 2.2

        _OverallAlpha ("Overall Alpha", Range(0,1)) = 0.75
        _AlphaDepthStrength ("Alpha Depth Strength", Range(-1,1)) = 0.0

        _AlphaCutoff ("Alpha Cutoff (edge clip)", Range(0,0.5)) = 0.02

        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [PerRendererData] _MainTex ("Sprite Texture (mask only)", 2D) = "white" {}
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

        _FoamTint ("Foam Tint", Color) = (1,1,1,1)
        _RimWidthPixels  ("Rim Width (px)", Range(0,12)) = 4
        _RimSoftPixels   ("Rim Soft (px)",  Range(0,4))  = 1
        _RimPulseSpeed   ("Rim Pulse Speed", Range(0,6)) = 1.6
        _RimPulsePixels  ("Rim Pulse Amplitude (px)", Range(0,6)) = 1.0
        _RimNoiseTex     ("Rim Noise (repeat)", 2D) = "gray" {}
        _RimNoiseScale   ("Rim Noise Scale (1/units)", Float) = 0.8
        _RimNoiseInfluence ("Rim Noise Influence", Range(0,1)) = 0.6
        _FoamContrast    ("Foam Contrast", Range(0.5,2)) = 1.1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Sprite"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "Unlit2D"
            CGPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _ShallowColor;
            float4 _DeepColor;
            float  _DepthBlendStrength;

            float  _UVScale;
            float4 _DetailPan;
            float  _DetailInfluence;

            float4 _RipplePan;
            float  _RippleTilingMul;
            float  _RippleAmount;
            float  _UVWobbleAmount;

            float4 _PanA;
            float4 _PanB;
            float  _NormalTilingMul;
            float  _NormalScale;

            float  _SheenStrength;
            float  _SheenSharpness;

            float  _OverallAlpha;
            float  _AlphaDepthStrength;

            float  _AlphaCutoff;

            float4 _FoamTint;
            float  _RimWidthPixels;
            float  _RimSoftPixels;
            float  _RimPulseSpeed;
            float  _RimPulsePixels;
            float  _RimNoiseScale;
            float  _RimNoiseInfluence;
            float  _FoamContrast;

            float4 _RendererColor;
            float  _EnableExternalAlpha;

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _AlphaTex;
            sampler2D _DetailTex;
            sampler2D _RippleTex;
            sampler2D _NormalA;
            sampler2D _NormalB;
            sampler2D _RimNoiseTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 spriteUV : TEXCOORD0;
                float4 col      : COLOR;
                float3 worldPos : TEXCOORD1;
                float2 screenUV : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.spriteUV = v.uv;
                o.col = v.color * _RendererColor;
                o.worldPos = world;

                float4 clip = o.pos;
                o.screenUV = clip.xy / max(1e-5, clip.w);
                o.screenUV = o.screenUV * 0.5 + 0.5;
                return o;
            }

            float3 unpackRGToNormal(float2 rg)
            {
                float2 xy = rg * 2.0 - 1.0;
                float z = sqrt(saturate(1.0 - dot(xy, xy)));
                return normalize(float3(xy, z));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;

                float spriteA = tex2D(_MainTex, i.spriteUV).a;
                if (_EnableExternalAlpha > 0.5)
                {
                    float aExt = tex2D(_AlphaTex, i.spriteUV).r;
                    spriteA *= aExt;
                }
                clip(spriteA - _AlphaCutoff);

                // Grid RotX 90° → brick face on XZ; use xz (not vendor xy).
                float2 worldUV = i.worldPos.xz * _UVScale;

                float2 uvRipple = worldUV * _RippleTilingMul + _RipplePan.xy * t;
                float ripple = tex2D(_RippleTex, uvRipple).r;
                float2 wobble = (ripple - 0.5) * _UVWobbleAmount;

                float2 uvA = worldUV * _NormalTilingMul + _PanA.xy * t + wobble * 1.5;
                float2 uvB = worldUV * _NormalTilingMul + _PanB.xy * t - wobble * 1.2;
                float2 nA = tex2D(_NormalA, uvA).rg;
                float2 nB = tex2D(_NormalB, uvB).rg;
                float2 nXY = normalize((nA * 2 - 1) + (nB * 2 - 1));
                float3 n = unpackRGToNormal(nXY * 0.5 + 0.5);
                n.xy *= _NormalScale;

                float2 uvDetail = worldUV + _DetailPan.xy * t + n.xy * 0.02 + wobble;
                float3 detail = tex2D(_DetailTex, uvDetail).rgb;

                float depthLerp = saturate((i.worldPos.y * 0.25) * _DepthBlendStrength * 0.5 + 0.5);
                float3 baseColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthLerp);
                float3 albedo = lerp(baseColor, baseColor * detail, _DetailInfluence);
                albedo += (ripple - 0.5) * _RippleAmount;

                float2 toEdge = abs(i.screenUV - 0.5) * 2.0;
                float edgeFactorSheen = pow(saturate(max(toEdge.x, toEdge.y)), _SheenSharpness);
                float sheen = edgeFactorSheen * _SheenStrength * 0.5;
                float3 waterUnder = saturate(albedo + sheen);

                float2 texel = _MainTex_TexelSize.xy;
                float aR = tex2D(_MainTex, i.spriteUV + float2(texel.x, 0)).a;
                float aL = tex2D(_MainTex, i.spriteUV - float2(texel.x, 0)).a;
                float aU = tex2D(_MainTex, i.spriteUV + float2(0, texel.y)).a;
                float aD = tex2D(_MainTex, i.spriteUV - float2(0, texel.y)).a;
                float2 grad = float2(aR - aL, aU - aD);
                float2 nEdge = -normalize(grad + 1e-6);

                // Rim noise also samples continuous world XZ.
                float rimNoise = tex2D(_RimNoiseTex, i.worldPos.xz * _RimNoiseScale).r;
                float pulse = sin(t * _RimPulseSpeed + rimNoise * 6.28318) * _RimPulsePixels;
                float rimPx = max(0.0, _RimWidthPixels + pulse);

                float2 inwardUV = i.spriteUV - nEdge * texel * rimPx;
                float aInside = tex2D(_MainTex, inwardUV).a;
                if (_EnableExternalAlpha > 0.5)
                {
                    float aExtIn = tex2D(_AlphaTex, inwardUV).r;
                    aInside *= aExtIn;
                }

                float feather = max(1e-5, max(texel.x, texel.y) * _RimSoftPixels);
                float edgeSoft = smoothstep(_AlphaCutoff, _AlphaCutoff + feather, spriteA);
                float innerSoft = smoothstep(_AlphaCutoff, _AlphaCutoff + feather, aInside);
                float rimBand = saturate(innerSoft - edgeSoft);

                float boil = 0.85 + 0.15 * sin(t * 2.1 + ripple * 6.28318);
                float foamMask = pow(saturate(rimBand * boil), _FoamContrast);

                float3 foamCol = _FoamTint.rgb;
                float3 color = lerp(waterUnder, foamCol, foamMask);

                float alpha = spriteA;
                alpha = saturate(alpha + (depthLerp - 0.5) * _AlphaDepthStrength);
                alpha *= _OverallAlpha * i.col.a;
                alpha *= saturate(foamMask + 0.0001);

                return float4(color * i.col.rgb, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
