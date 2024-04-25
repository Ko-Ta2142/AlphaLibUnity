

// texture * color + tintcolor

// mode switch macro. Only one of them.

#define TexColor_TintMode
//#define TexColor_MulScreen

inline float4 texcolor_calc(float4 src, float4 m, float4 t)
{
#ifdef TexColor_MulScreen
	// multiply + screen
	half4 col = src * m;
	col.rgb = (col.rgb + t.rgb) - (col.rgb * tint.rgb);
	return col;
#else
	// tint mode
	half4 col = src * m;
	half4 tint = (1.0 - src) * t;
	col.rgb = col.rgb + tint.rgb;
	return col;
#endif
}


inline half3 _calc(half4 c)
{
#ifdef _CALC_INV
	// *repeal. not used it.
	return 1 - c.rgb;
#elif _CALC_LINEARLIGHT
	// dest + (2src - 1)
	// return 2 * c.rgb - 1.0;
	// op : rev sub
	return - (2 * c.rgb - 1.0);
#else
	return c.rgb;
#endif
}

