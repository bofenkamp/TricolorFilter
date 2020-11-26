Shader "Custom/ColorPickShader"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)

		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255

		_ColorMask("Color Mask", Float) = 15

		[HideInInspector] _Left("Left Edge", Float) = -1
		[HideInInspector] _Right("Right Edge", Float) = -1
		[HideInInspector] _Top("Top Edge", Float) = -1
		[HideInInspector] _Bottom("Bottom Edge", Float) = -1
		[HideInInspector] _ThirdVal("Slider Value", Float) = 0.5

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
	}

		SubShader
		{
			Tags
			{
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
				"PreviewType" = "Plane"
				"CanUseSpriteAtlas" = "True"
			}

			Stencil
			{
				Ref[_Stencil]
				Comp[_StencilComp]
				Pass[_StencilOp]
				ReadMask[_StencilReadMask]
				WriteMask[_StencilWriteMask]
			}

			Cull Off
			Lighting Off
			ZWrite Off
			ZTest[unity_GUIZTestMode]
			Blend SrcAlpha OneMinusSrcAlpha
			ColorMask[_ColorMask]

			Pass
			{
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"
				#include "UnityUI.cginc"

				#pragma multi_compile __ UNITY_UI_ALPHACLIP

				struct appdata_t
				{
					float4 vertex   : POSITION;
					float4 color    : COLOR;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f
				{
					float4 vertex   : SV_POSITION;
					fixed4 color : COLOR;
					half2 texcoord  : TEXCOORD0;
					float4 worldPosition : TEXCOORD1;
				};

				fixed4 _Color;
				fixed4 _TextureSampleAdd;
				float4 _ClipRect;

				v2f vert(appdata_t IN)
				{
					v2f OUT;
					OUT.worldPosition = IN.vertex;
					OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

					OUT.texcoord = IN.texcoord;

					#ifdef UNITY_HALF_TEXEL_OFFSET
					OUT.vertex.xy += (_ScreenParams.zw - 1.0) * float2(-1,1);
					#endif

					OUT.color = IN.color * _Color;
					return OUT;
				}

				sampler2D _MainTex;
				float _Left;
				float _Right;
				float _Top;
				float _Bottom;
				float _ThirdVal;

				fixed4 frag(v2f IN) : SV_Target
				{
					half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

					float2 size = float2(_Right - _Left, _Top - _Bottom);
					float h = (IN.worldPosition.x - _Left) / size.x;
					float l = (IN.worldPosition.y - _Bottom) / size.y;
					float s = _ThirdVal;

					h = h * 360.0;
					float C = (1.0 - abs(2.0 * l - 1.0)) * s;
					float X = C * (1.0 - abs((h / 60.0) % 2 - 1));
					float m = l - C / 2.0;
					float r = 0;
					float g = 0;
					float b = 0;
					if (0 <= h && h < 60)
					{
						r = C;
						g = X;
					}
					else if (60 <= h && h < 120)
					{
						r = X;
						g = C;
					}
					else if (120 <= h && h < 180)
					{
						g = C;
						b = X;
					}
					else if (180 <= h && h < 240)
					{
						g = X;
						b = C;
					}
					else if (240 <= h && h < 300)
					{
						r = X;
						b = C;
					}
					else
					{
						r = C;
						b = X;
					}

					float4 rgb = float4(r + m, g + m, b + m, 1);

					color.r = rgb.r;
					color.g = rgb.g;
					color.b = rgb.b;

					color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

					#ifdef UNITY_UI_ALPHACLIP
					clip(color.a - 0.001);
					#endif

					return color;
				}
			ENDCG
			}
		}
}