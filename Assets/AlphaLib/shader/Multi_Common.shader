Shader "AlphaLib/Multi_Common"
{
    Properties
    {
        _maintex ("MainTexture (ARGB)", 2D) = "white" {}
		_subtex ("SubTexture (ARGB)", 2D) = "white" {}
		_color("Color", Color) = (1,1,1,1)
		_tintcolor("TintColor + alpha2", Color) = (0,0,0,0)
		_option("Option", Range(0.0, 1.0)) = 0.0

		_transcolor("Transparent color" , Color) = (0,0,0,0)
		[Enum(Off, 0, On, 1)]
		_nonalpha("Non alpha" , Float) = 0.0
		_invsubtex("Invision sub texture", Range(0.0,1.0)) = 0.0

		// SubShader setting. unsupport materialpropertyblock.
		[Enum(UnityEngine.Rendering.CullMode)]
		_cull("Cull", Int) = 0        // Off
		[Enum(Off, 0, On, 1)]
		_zwrite("Z write", Int) = 0   // Off
		[Enum(UnityEngine.Rendering.CompareFunction)]
		_ztest("Z test", Int) = 4     // LEqual

		[Enum(UnityEngine.Rendering.BlendMode)]
		_srcblend("Src blend factor", Int) = 1  // One
		[Enum(UnityEngine.Rendering.BlendMode)]
		_destblend("Dest blend factor", Int) = 10 // OneMinusSrcAlpha
		[Enum(Add,0 , Sub,1 , RevSub,2 , Min,3 , Max,4)]  //* unity unsupport now.
		_blendop("Blend op factor", Int) = 0 // Add

		[KeywordEnum(PASS, LINEARLIGHT)] //, COLORBURN, COLORDODGE)]
		_CALC("Switch blending calc", Int) = 0
		[KeywordEnum(BLEND, MASK, FADE, OVERLAP, EXTRACT)]
		_MULTI("Switch multi texture blend", Int) = 0
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
		//LOD 200
		Cull [_cull] 
		ZTest [_ztest]
		ZWrite [_zwrite]
		Blend [_srcblend] [_destblend]
		BlendOP [_blendop]

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			// make fog work
			//#pragma multi_compile_fog

			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0

			// * progma code can not move include file. raise not defined error with property.
			#pragma multi_compile _ _MULTI_MASK _MULTI_FADE _MULTI_OVERLAP _MULTI_EXTRACT
			#pragma multi_compile _ _CALC_LINEARLIGHT //_CALC_COLORBURN _CALC_COLORDODGE 
			#include "BlendSwitch_func.cginc"

			sampler2D _maintex;
			sampler2D _subtex;
CBUFFER_START(UnityPerMaterial)				// support SRP batcher
			float4 _maintex_ST;
			float4 _subtex_ST;
			half4 _color;
			half4 _tintcolor;

			float1 _option;

			half4 _transcolor;
			float _nonalpha;
			float _invsubtex;
CBUFFER_END

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
				half4 tex = tex2D(_maintex, i.uv0);
				tex.a = max(tex.a, _nonalpha);

#ifdef _MULTI_MASK
				half4 tex2 = tex2D(_subtex, i.uv1);
				half4 inv = half4(_invsubtex, _invsubtex, _invsubtex, 0);
				tex2 = abs(tex2 - inv);
				float v = (tex2.r + tex2.g + tex2.b) * 0.3334;
				v = min(v, 1);
				v = lerp(1.0, v, _tintcolor.a);
				tex.a = tex.a * v;
#elif _MULTI_FADE
				half4 tex2 = tex2D(_subtex, i.uv1);
				half4 inv = half4(_invsubtex, _invsubtex, _invsubtex, 0);
				tex2 = abs(tex2 - inv);
				//tex2.rgb = abs(tex2.rgb - _subinv);
				float a = lerp(1.0, 1 / 256, _option);
				float m = 1.0 / a;
				float s = lerp(1.0, -a, _tintcolor.a);	// 1.0 to -a
				float v = (tex2.r + tex2.g + tex2.b) * 0.3334;
				v = (v - s) * m;
				v = clamp(v, 0, 1);
				tex.a = tex.a * v;
#elif _MULTI_OVERLAP
				half4 tex2 = tex2D(_subtex, i.uv1);
				half4 inv = half4(_invsubtex, _invsubtex, _invsubtex, 0);
				tex2 = abs(tex2 - inv);
				tex.rgb = lerp(tex.rgb, tex2, _tintcolor.a);
#elif _MULTI_EXTRACT
				half4 tex2 = tex2D(_subtex, i.uv1);
				half4 inv = half4(_invsubtex, _invsubtex, _invsubtex, 0);
				tex2 = abs(tex2 - inv);
				float a = lerp(1.0, 1 / 256, _option);
				float m = 1.0 / a;
				float s = lerp(1.0 + a, -a, _tintcolor.a);	// 1.0+a to -a
				float v = (tex2.r + tex2.g + tex2.b) * 0.3334;
				v = (v - s) * m * 2;
				v = v > 1.0 ? 2.0 - v : v;
				v = clamp(v, 0, 1);
				tex.a = tex.a * v;
#else
				// blend
				half4 tex2 = tex2D(_subtex, i.uv1);
				half4 inv = half4(_invsubtex, _invsubtex, _invsubtex, 0);
				tex2 = abs(tex2 - inv);
				tex = lerp(tex, tex2, _tintcolor.a);
#endif

				half4 col = tex * _color;
				half4 tint = (1.0 - tex) * _tintcolor;
				col.rgb = col.rgb + tint.rgb;
				// transpalent color
				col.rgb = lerp(_transcolor.rgb, col.rgb, col.a);
				// blending calc
				col.rgb = _calc(col);
				//UNITY_APPLY_FOG(i.fogCoord, col);

				return col;
			}
				
			ENDCG
		}
	}
}
