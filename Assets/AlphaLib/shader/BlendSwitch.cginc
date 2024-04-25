



// texture * color + tintcolor

// mode switch macro. Only one of them.

#define TexColor_TintMode
//#define TexColor_MulScreen

inline float4 texcolor_calc(float4 src, float4 m, float4 t)
{
#ifdef TexColor_MulScreen
	// multiply + screen
	half4 col = src * m;
	half4 tint = (1.0 - col) * t;
	col.rgb = col.rgb + tint.rgb;
	return col;
#else
	// tint mode
	half4 col = src * m;
	half4 tint = (1.0 - src) * t;
	col.rgb = col.rgb + tint.rgb;
	return col;
#endif
}

