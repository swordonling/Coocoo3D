#include "../RandomNumberGenerator.hlsli"
cbuffer cb0 : register(b0)
{
	float4x4 xproj;
	float4x4 nxproj;
	float4x4 yproj;
	float4x4 nyproj;
	float4x4 zproj;
	float4x4 nzproj;
	uint2 imageSize;
}
RWTexture2DArray<float4> IrradianceMap : register(u0);
TextureCube Image : register(t0);
SamplerState s0 : register(s0);
[numthreads(8, 8, 1)]
void main(uint3 dtid : SV_DispatchThreadID)
{
	IrradianceMap[dtid] = float4(0, 0, 0, 1);
}