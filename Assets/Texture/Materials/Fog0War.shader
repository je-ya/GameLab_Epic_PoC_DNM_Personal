Shader "Custom/FogOfWar"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _UnitPositions ("Unit Positions", Vector) = (0,0,0,0)
        _SightRadius ("Sight Radius", Float) = 0.1
        _FogAlpha ("Fog Alpha", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _UnitPositions;
            float _SightRadius;
            float _FogAlpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float dist = distance(uv, _UnitPositions.xy);
                float visibility = dist < _SightRadius ? 1.0 : _FogAlpha;
                return fixed4(0, 0, 0, 1.0 - visibility);
            }
            ENDCG
        }
    }
}