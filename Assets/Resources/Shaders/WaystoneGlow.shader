// Animated shimmer for a painted waystone (MapDef.sceneGlow): soft ripples inside an ellipse with a bright rim,
// added on top of the picture. Drawn on a plain full-rectangle quad; the sprite's pixels are not used.
// Adapted from the "Cinderford Waystone Plaza" pack (AshenVale/Cinderford/Waystone): same effect, with no
// SRP-only LightMode tag so it also draws in the Built-in pipeline. Lives in Resources so builds include it.
Shader "AshenVale/WaystoneGlow"
{
    Properties
    {
        _MainTex ("Sprite (unused)", 2D) = "white" {}
        _Color ("Glow", Color) = (.1, 1, .8, 1)
        _Active ("Strength", Range(0, 1)) = 1
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
            float _Active;

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
                float mask = 1 - smoothstep(.78, 1, r);
                float wave = .5 + .5 * sin(r * 24 - _Time.y * 2.5 + sin(p.y * 8 + _Time.y));
                float edge = exp(-abs(r - .78) * 16);
                float a = mask * (.08 + wave * .13 + edge * .22) * _Active;
                return float4(_Color.rgb, a);
            }
            ENDCG
        }
    }
}
