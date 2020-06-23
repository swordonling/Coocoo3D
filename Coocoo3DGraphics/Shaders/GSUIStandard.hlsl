cbuffer cb0 : register(b0)
{
	float4x4 g_mWorld;
};
cbuffer cb3 : register(b3)
{
	float4x4 g_mWorldToProj;
	float3   g_vCamPos;
	float g_aspectRatio;
};

struct GeometryShaderInput
{
	float4 pos : SV_POSITION;
	uint3 rci  : RCI;
	float2 size: SIZE;
};

struct PixelShaderInput
{
	float4 pos : SV_POSITION;
	float2 tex : TEXCOORD;
};

[maxvertexcount(4)]
void main(point GeometryShaderInput input[1], inout TriangleStream<PixelShaderInput> SpriteStream)
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

	for (int i = 0; i < 4; i++)
	{
		float4 position = input[0].pos;
		position = mul(mul(float4(position.xyz, 1.0f), g_mWorld), g_mWorldToProj);

		output.pos = position + float4(g_positions[i].x / g_aspectRatio * input[0].size.x, g_positions[i].y * input[0].size.y, 0, 0)*position.w*0.5f;

		output.tex = g_texcoords[i];
		SpriteStream.Append(output);
	}
	SpriteStream.RestartStrip();
}