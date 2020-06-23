Texture2D texture1 :register(t0);
SamplerState s1 : register(s0);

struct PixelShaderInput
{
	float4 pos : SV_POSITION;
	float2 TexCoord : TEXCOORD;
	uint index : INDEX;
};

float4 main(PixelShaderInput input) : SV_TARGET
{
	float4 color = texture1.Sample(s1,input.TexCoord);
	clip(color.a - 1 / 255.0f);
	return color;
}
