#include "../RandomNumberGenerator.hlsli"
cbuffer cb0 : register(b0)
{
	uint2 imageSize;
	int quality;
	uint batch;
}


const static float4x4 _xproj=
{ 0,0,-1,0,
0,-1,0,0,
0,0,0,-100,
1,0,0,100, };
const static float4x4 _nxproj=
{ 0,0,1,0,
0,-1,0,0,
0,0,0,-100,
-1,0,0,100, };
const static float4x4 _yproj=
{ 1,0,0,0,
0,0,1,0,
0,0,0,-100,
0,1,0,100, };
const static float4x4 _nyproj=
{ 1,0,0,0,
0,0,-1,0,
0,0,0,-100,
0,-1,0,100, };
const static float4x4 _zproj=
{ 1,0,0,0,
0,-1,0,0,
0,0,0,-100,
0,0,1,100, };
const static float4x4 _nzproj=
{ -1,0,0,0,
0,-1,0,0,
0,0,0,-100,
0,0,-1,100, };

RWTexture2DArray<float4> IrradianceMap : register(u0);
TextureCube Image : register(t0);
SamplerState s0 : register(s0);
[numthreads(8, 8, 1)]
void main(uint3 dtid : SV_DispatchThreadID)
{
	float3 TexDir = float3(0, 0, 0);
	float4 dir1 = float4(0, 0, 0, 0);
	uint randomState = dtid.x + dtid.y * 2048 + dtid.z * 4194304 + batch * 67108864;
	float2 screenPos = ((float2)dtid.xy + 0.5f) / (float2)imageSize * 2 - 1;
	if (dtid.x > imageSize.x || dtid.y > imageSize.y)
	{
		return;
	}
	if (dtid.z == 0)
	{
		dir1 = mul(float4(screenPos, 0, 1), _xproj);
	}
	else if (dtid.z == 1)
	{
		dir1 = mul(float4(screenPos, 0, 1), _nxproj);
	}
	else if (dtid.z == 2)
	{
		dir1 = mul(float4(screenPos, 0, 1), _yproj);
	}
	else if (dtid.z == 3)
	{
		dir1 = mul(float4(screenPos, 0, 1), _nyproj);
	}
	else if (dtid.z == 4)
	{
		dir1 = mul(float4(screenPos, 0, 1), _zproj);
	}
	else
	{
		dir1 = mul(float4(screenPos, 0, 1), _nzproj);
	}
	TexDir = normalize(dir1.xyz / dir1.w);
	float3 col1 = float3(0, 0, 0);
	const int c_sampleCount = 1024;
	for (int i = 0; i < c_sampleCount; i++)
	{
		float3 vec1 = normalize(float3(RNG::Random01(randomState) * 2 - 1, RNG::Random01(randomState) * 2 - 1, RNG::Random01(randomState) * 2 - 1));
		float ndl = dot(vec1, TexDir);
		if (ndl < 0)
		{
			vec1 = -vec1;
			ndl = -ndl;
		}
		col1 += Image.SampleLevel(s0, vec1, 0) * ndl / c_sampleCount / 3.14159265359f;
	}
	float qsp = 1.0f / (quality+ 1.0f);
	IrradianceMap[dtid] = float4(col1 * qsp + IrradianceMap[dtid].rgb, 1);
	//IrradianceMap[dtid] = float4(TexDir, 1);
	//IrradianceMap[dtid] = float4(0.5,1,1,1);
}