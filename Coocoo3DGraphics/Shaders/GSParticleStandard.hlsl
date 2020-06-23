cbuffer cb0 : register(b0)
{
	float4x4 g_mWorld;
};
cbuffer cb3 : register(b3)
{
	float4x4 g_mWorldToProj;
	float3   g_vCamPos2;
	float g_aspectRatio;
};

struct PixelShaderInput
{
	float4 pos : SV_POSITION;
	float2 tex : TEXCOORD;
	uint index : INDEX;
};

[maxvertexcount(4)]
void main(point PixelShaderInput input[1], inout TriangleStream<PixelShaderInput> SpriteStream)
{

static float3 g_positions[4] =
{
	float3(-1, 1, 0),
	float3(1, 1, 0),
	float3(-1, -1, 0),
	float3(1, -1, 0),
};
static float2 g_texcoords[4] =
{
	float2(0,0),
	float2(1,0),
	float2(0,1),
	float2(1,1),
};
	PixelShaderInput output;

	//
	// Emit two new triangles
	//
	for (int i = 0; i < 4; i++)
	{
		float4 position = input[0].pos;
		position = mul(mul(float4(position.xyz, 1.0f), g_mWorld),g_mWorldToProj);

		//output.pos = position + float4(input.tex.x - 0.5f, (input.tex.y - 0.5f)*g_aspectRatio, 0, 0)*0.02f*pos1.w;
		output.pos = position + float4(g_positions[i].x/g_aspectRatio, g_positions[i].y, 0, 0)*0.4f;
		output.index = input[0].index;

		output.tex = g_texcoords[i];
		SpriteStream.Append(output);
	}
	SpriteStream.RestartStrip();
}