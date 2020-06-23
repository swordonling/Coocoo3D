cbuffer cb0 : register(b0)
{
	float4x4 g_mWorld;
};
cbuffer cb3 : register(b3)
{
	float4x4 g_mWorldToProj;
	float3   g_vCamPos;
	float g_aspectRatio;
	float _Time;
	float _DeltaTime;
};

struct PSSkinnedIn
{
	float4 pos	: SV_POSITION;		//Position
	float4 vPos	: POSWORLD;			//world space Pos
	float3 Norm : NORMAL;			//Normal
	float2 Tex	: TEXCOORD;		    //Texture coordinate
	float3 Tangent : TANGENT;		//Normalized Tangent vector
};

[maxvertexcount(3)]
void main(
	triangle PSSkinnedIn input[3],
	inout TriangleStream< PSSkinnedIn > triStream
)
{
	PSSkinnedIn output;
	float3 norm = normalize(cross(input[0].vPos.xyz - input[1].vPos.xyz, input[1].vPos.xyz - input[2].vPos.xyz));
	for (int i = 0; i < 3; i++)
	{
		output = input[i];
		output.pos = input[i].vPos;
		output.pos = output.pos + float4(-3.0f*norm, 0);
		output.pos = mul(output.pos, g_mWorldToProj);

		triStream.Append(output);
	}
	triStream.RestartStrip();
}