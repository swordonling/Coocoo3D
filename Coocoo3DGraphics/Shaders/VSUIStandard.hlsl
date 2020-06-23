struct VertexShaderInput
{
	float3 pos : POSITION;
	uint3 rci  : RCI;
	float2 size: SIZE;
};


struct GeometryShaderInput
{
	float4 pos : SV_POSITION;
	uint3 rci  : RCI;
	float2 size: SIZE;
};

GeometryShaderInput main(VertexShaderInput input)
{
	GeometryShaderInput output;
	output.pos = float4(input.pos, 1);
	output.rci = input.rci;
	output.size = input.size;

	return output;
}
