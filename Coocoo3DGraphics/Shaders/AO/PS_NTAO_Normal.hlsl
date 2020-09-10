
#include "../CameraDataDefine.hlsli"
struct LightInfo
{
	float3 LightDir;
	uint LightType;
	float4 LightColor;
	float4x4 LightSpaceMatrix;
};
cbuffer cb1 : register(b1)
{
	float4x4 g_mWorld;
	float4x4 g_mWorld1;
	LightInfo Lightings[4];
};
cbuffer cb3 : register(b3)
{
	float4 _DiffuseColor;
	float4 _SpecularColor;
	float3 _AmbientColor;
	float _EdgeScale;
	float4 _EdgeColor;

	float4 _Texture;
	float4 _SubTexture;
	float4 _ToonTexture;
	uint notUse;
	float _Metallic;
	float _Roughness;
	float _Emission;
};
cbuffer cb2 : register(b2)
{
	CAMERA_DATA_DEFINE//is a macro
		uint g_enableAO;
	uint g_enableShadow;
	uint g_quality;
};
Texture2D texture0 :register(t0);
SamplerState s0 : register(s0);
Texture2D texture1 :register(t1);
SamplerState s1 : register(s1);
Texture2D ShadowMap0:register(t2);
SamplerState sampleShadowMap0 : register(s2);
TextureCube EnvCube : register (t3);
TextureCube IrradianceCube : register (t4);
struct PSSkinnedIn
{
	float4 Pos	: SV_POSITION;		//Position
	float4 wPos	: POSITION;			//world space Pos
	float3 Norm : NORMAL;			//Normal
	float2 TexCoord	: TEXCOORD;		//Texture coordinate
	float3 Tangent : TANGENT;		//Normalized Tangent vector
};
float4 main(PSSkinnedIn input) : SV_TARGET
{
	float4 texColor = texture0.Sample(s0, input.TexCoord) * _DiffuseColor;
	clip(texColor.a - 0.01f);
	return float4(normalize(input.Norm),1);
}