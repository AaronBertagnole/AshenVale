// Animated lights over painted map pictures (MapArt effects), drawn on a plain full-rectangle quad and added on
// top of the picture; the sprite's pixels are not used. _Mode picks the look:
//   0  portal: soft ripples inside an ellipse with a bright rim (waystones, glowing gates)
//   1  flowing water: bright bands running down (waterfalls, streams, river glints)
//   2  mist: a faint, slowly drifting haze
//   3  fire: a warm flicker (hearths, forges, flames)
// Adapted from the owner's Cinderford and "All Maps" packs, without their SRP-only pass tag so it also draws in
// the Built-in pipeline. Lives in Resources so builds include it.
Shader "AshenVale/MapEffect"
{
    Properties
    {
        _MainTex ("Sprite (unused)", 2D) = "white" {}
        _Color ("Glow", Color) = (.1, 1, .8, 1)
        _Active ("Strength", Range(0, 1)) = 1
        _Mode ("Mode", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One
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
                float a;
                if (_Mode < .5)
                {
                    float mask = 1 - smoothstep(.78, 1, r);
                    float wave = .5 + .5 * sin(r * 24 - t * 2.5 + sin(p.y * 8 + t));
                    float edge = exp(-abs(r - .78) * 16);
                    a = mask * (.08 + wave * .13 + edge * .22);
                }
                else
                {
                    float mask = 1 - smoothstep(.65, 1, r);
                    if (_Mode < 1.5) { float s = sin(p.y * 18 + t * 6); a = mask * (.09 + .12 * s * s); }
                    else if (_Mode < 2.5) a = mask * (.035 + .025 * sin(p.x * 6 + t * .3 + sin(p.y * 5)));
                    else a = mask * (.14 + .08 * sin(t * 6) + .04 * sin(t * 13));
                }
                return float4(_Color.rgb, a * _Active);
            }
            ENDCG
        }
    }
}
