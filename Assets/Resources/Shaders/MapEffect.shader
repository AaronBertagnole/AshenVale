// Animated lights over painted map pictures (MapArt effects), drawn on a plain full-rectangle quad; the sprite's
// pixels are not used. _Mode picks the look, and MapScenery sets the blend to match:
//   0  portal: ripples inside an ellipse with a bright rim (waystones, glowing gates)      added light
//   1  flowing water: bands of deep and pale water running down (falls, streams, rivers)   painted over
//   2  mist: a pale, slowly drifting haze                                                  painted over
//   3  fire: a warm flicker (hearths, forges, flames)                                      added light
// Added light (Blend SrcAlpha One) only shows over darker paint, so the water and mist are painted over the picture
// instead (Blend SrcAlpha OneMinusSrcAlpha) with bands darker and lighter than the tint, which read on bright paint.
// Adapted from the owner's Cinderford and "All Maps" packs, without their SRP-only pass tag so it also draws in
// the Built-in pipeline. Lives in Resources so builds include it.
Shader "AshenVale/MapEffect"
{
    Properties
    {
        _MainTex ("Sprite (unused)", 2D) = "white" {}
        _Color ("Tint", Color) = (.1, 1, .8, 1)
        _Active ("Strength", Range(0, 3)) = 1
        _Mode ("Mode", Float) = 0
        _SrcBlend ("Source blend", Float) = 5      // SrcAlpha
        _DstBlend ("Destination blend", Float) = 1 // One (added light); 10 = OneMinusSrcAlpha (painted over)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Cull Off
        ZWrite Off
        Blend [_SrcBlend] [_DstBlend]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            float4 _Color;
            float _Active, _Mode;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = (i.uv - .5) * 2;
                float r = length(p);
                float t = _Time.y;
                float3 col = _Color.rgb;
                float a;
                if (_Mode < .5)
                {
                    float mask = 1 - smoothstep(.78, 1, r);
                    float wave = .5 + .5 * sin(r * 24 - t * 2.5 + sin(p.y * 8 + t));
                    float edge = exp(-abs(r - .78) * 16);
                    a = mask * (.16 + wave * .26 + edge * .45);
                    col = lerp(col, 1, edge * .4);              // the rim goes nearly white
                }
                else if (_Mode < 1.5)
                {
                    float mask = (1 - smoothstep(.55, 1, r));
                    float band = .5 + .5 * sin(p.y * 14 + t * 5 + sin(p.x * 5 + t * .7) * .8);
                    float3 deep = col * .55, pale = lerp(col, 1, .85);
                    col = lerp(deep, pale, band);
                    a = mask * (.35 + .3 * (1 - band));         // the deep bands show most
                }
                else if (_Mode < 2.5)
                {
                    float mask = (1 - smoothstep(.5, 1, r)) * (1 - smoothstep(.3, 1, abs(p.y)));
                    float drift = .5 + .5 * sin(p.x * 5 + t * .3 + sin(p.y * 4 + t * .2));
                    col = lerp(col, 1, .55);
                    a = mask * (.14 + .22 * drift);
                }
                else
                {
                    float mask = 1 - smoothstep(.5, 1, r);
                    a = mask * (.3 + .18 * sin(t * 6) + .1 * sin(t * 13) + .08 * sin(t * 21 + p.y * 4));
                }
                return float4(col, saturate(a * _Active));
            }
            ENDCG
        }
    }
}
