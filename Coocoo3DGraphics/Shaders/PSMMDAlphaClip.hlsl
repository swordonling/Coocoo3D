Texture2D texture0 :register(t0);
SamplerState s0 : register(s0);
struct PSSkinnedIn
{
	float4 Pos	: SV_POSITION;		//Position
	float4 wPos	: POSWORLD;			//world space Pos
	float3 Norm : NORMAL;			//Normal
	float2 TexCoord	: TEXCOORD;		//Texture coordinate
	float3 Tangent : TANGENT;		//Normalized Tangent vector
};

struct PSOutput
{
	float4 color : SV_TARGET;
};
PSOutput main(PSSkinnedIn input)
{
	clip(texture0.Sample(s0,input.TexCoord).a - 0.01f);
	PSOutput output;
	output.color = float4(1, 0, 1, 1);
	return output;
}