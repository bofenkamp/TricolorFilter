Shader "Custom/PostEffectShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
		_Color1("Black", Color) = (.5, 0, .5)
		_Color2("Grey", Color) = (.5, .5, 0)
		_Color3("White", Color) = (0, 0, .5)
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
			float4 _Color1;
			float4 _Color2;
			float4 _Color3;

			fixed4 frag(v2f IN) : SV_Target
			{
                fixed4 col = tex2D(_MainTex, IN.uv);
				//fixed4 col = tex2D(_MainTex, IN.uv + float2(0, sin( IN.vertex.x/40 + _Time[1] ) /10 ) );

				float lum = (col[0] + col[1] + col[2]) / 3.0;

				float color1amount = 0;
				float color2amount = 0;
				float color3amount = 0;

				if (lum < 0.5)
				{
					color1amount = -2.0 * lum + 1.0;
					color2amount = 2.0 * lum;
				}
				else if (lum == 0.5)
				{
					color2amount = 1.0;
				}
				else if (lum > 0.5)
				{
					color2amount = -2.0 * lum + 2.0;
					color3amount = 2.0 * lum - 1.0;
				}

				col = _Color1 * color1amount + _Color2 * color2amount + _Color3 * color3amount;

                return col;
            }
            ENDCG
        }
    }
}
