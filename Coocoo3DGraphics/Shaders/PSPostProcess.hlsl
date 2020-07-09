struct PSSkinnedIn
{
	float4 Pos	: SV_POSITION;		//Position
	float2 uv : TEXCOORD;
};
Texture2D texture0 :register(t0);
SamplerState s0 : register(s0);
cbuffer cb0 : register(b0)
{
	float _GammaCorrection;
};
float4 main(PSSkinnedIn input) : SV_TARGET
{
	float4 col = texture0.Sample(s0, input.uv);
	col.rgb = pow(col.rgb, 1 / _GammaCorrection);
	return col;
}