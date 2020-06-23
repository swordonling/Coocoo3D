Texture2D texture0 :register(t0);
SamplerState s0 : register(s0);

struct PixelShaderInput
{
	float4 pos : SV_POSITION;
	float2 tex : TEXCOORD;
};

float4 main(PixelShaderInput input) : SV_TARGET
{
	float4 color = texture0.Sample(s0,input.tex);
	clip(color.a - 1/255.0f);
	color.rgb = pow(color.rgb, 1 / 2.2f);
	return color;
}
