Shader "AlphaLib/TexNone_OverlayPass1"
{
	Properties
	{
		_maintex("MainTexture (ARGB)", 2D) = "white" {}
		_subtex("SubTexture (ARGB)", 2D) = "white" {}
		_color("Color", Color) = (1,1,1,1)
		_tintcolor("TintColor", Color) = (0,0,0,0)
		_alpha2("Alpha2", Range(0.0, 1.0)) = 0.0
		_option("Option", Range(0.0, 1.0)) = 0.0

		// SubShader setting. unsupport materialpropertyblock.
		[Enum(UnityEngine.Rendering.CullMode)]
		_cull("Cull", Int) = 0        // Off
		[Enum(Off, 0, On, 1)]
		_zwrite("Z write", Int) = 0   // Off
		[Enum(UnityEngine.Rendering.CompareFunction)]
		_ztest("Z test", Int) = 4     // LEqual

		//[Enum(UnityEngine.Rendering.BlendMode)]
		//_srcblend("Src blend factor", Int) = 1  // One
		//[Enum(UnityEngine.Rendering.BlendMode)]
		//_destblend("Dest blend factor", Int) = 10 // OneMinusSrcAlpha
		//[Enum(Add,0 , Sub,1 , RevSub,2 , Min,3 , Max,4)]  //* unity unsupport now.
		//_blendop("Blend op factor", Int) = 0 // Add

		//[KeywordEnum(BLEED, PMA)]
		//_TEXTURECOLOR("Texture color mode", Int) = 0
	}

	SubShader
	{
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
		//LOD 200
		Cull[_cull]
		ZTest[_ztest]
		ZWrite[_zwrite]
		Blend DstColor Zero
		BlendOP Add

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			// PMA
			//#pragma multi_compile _TEXTUREMODE_BLEED _TEXTUREMODE_PMA

			// make fog work
			//#pragma multi_compile_fog

			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0

			sampler2D _maintex;
			float4 _maintex_ST;
			sampler2D _subtex;
			float4 _subtex_ST;
			half4 _color;
			half4 _tintcolor;
			float1 alpha2;
			float1 option;

			struct appdata
			{
				half4 pos : POSITION;
				// half4 color : COLOR0; // color
				float2 uv0 : TEXCOORD0;	// texture1
				float2 uv1 : TEXCOORD1;	// texture2
				// half4 uv2 : TEXCOORD2; // tintcolor + alpha2
			};

			struct v2f
			{
				half4 pos : SV_POSITION;
				// half4 color : COLOR0; // color
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				// half4 uv2 : TEXCOORD2; // tintcolor + alpha2
			};

			// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
			// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
			// #pragma instancing_options assumeuniformscaling
			//UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
			//UNITY_INSTANCING_BUFFER_END(Props)

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.pos);
				o.uv0 = TRANSFORM_TEX(v.uv0, _maintex);
				o.uv1 = TRANSFORM_TEX(v.uv1, _subtex);
				//UNITY_TRANSFER_FOG(o, o.pos);
				return o;
			}
			half4 frag(v2f i) : SV_Target
			{
				// modulate
				// out = 2*src * dest : rgb < 0.5
				half4 tex = tex2D(_maintex, i.uv0);
				half4 col = tex * _color;
				half4 tint = (1.0 - tex) * _tintcolor;
				col.rgb = col.rgb + tint.rgb;
				// transpalent color (gray)
				col.rgb = 0.5 + (col.rgb - 0.5) * col.a;
				// 2*src
				col.rgb = min(col.rgb, 0.5);
				col.rgb = col.rgb * 2;

				// apply fog
				//UNITY_APPLY_FOG(i.fogCoord, col);

				return col;
			}
			ENDCG
		}
	}
}
