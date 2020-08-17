#define UNITY_BRDF_GGX 1
#include "FromDisney/DisneyBRDF.hlsli"
#include "RandomNumberGenerator.hlsli"
#include "CameraDataDefine.hlsli"
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
	float _Subsurface;
	float _Specular;
	float _SpecularTint;
	float _Anisotropic;
	float _Sheen;
	float _SheenTint;
	float _Clearcoat;
	float _ClearcoatGloss;
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
	float4 wPos	: POSWORLD;			//world space Pos
	float3 Norm : NORMAL;			//Normal
	float2 TexCoord	: TEXCOORD;		//Texture coordinate
	float3 Tangent : TANGENT;		//Normalized Tangent vector
};
float3 brdf_s(float3 L, float3 V, float3 N, float3 X, float3 Y, float3 baseColor)
{
	return BRDF(L, V, N, X, Y, baseColor, _Metallic, _Roughness, _Subsurface, _Specular, _SpecularTint, _Anisotropic, _Sheen, _SheenTint, _Clearcoat, _ClearcoatGloss);
}
float4 main(PSSkinnedIn input) : SV_TARGET
{
	float3 strength = float3(0,0,0);
	float3 viewDir = normalize(g_vCamPos - input.wPos);
	float3 norm = normalize(input.Norm);
	float4 texColor = texture0.Sample(s0, input.TexCoord) * _DiffuseColor;
	clip(texColor.a - 0.01f);
	float3 diff = texColor.rgb;
	uint randomState = (uint)(input.Pos.x * input.Pos.w + (uint)(input.Pos.y * input.Pos.w) * 8192) * 8192 + g_camera_randomValue;
	float3 randomDir = normalize(float3(RNG::Random01(randomState) * 2 - 1, RNG::Random01(randomState) * 2 - 1, RNG::Random01(randomState) * 2 - 1));
	if (dot(randomDir, norm) < 0)
	{
		randomDir = -randomDir;
	}
	float3 X = normalize(input.Tangent);
	float3 Y = cross(input.Norm,input.Tangent);
	//float3 inDirect = _AmbientColor;
	float3 inDirect = float3(0,0,0);
	for (int i = 0; i < 1; i++)
	{
		if (Lightings[i].LightColor.a != 0)
		if (Lightings[i].LightType == 0)
		{
			float inShadow = 1.0f;
			float4 sPos = mul(input.wPos, Lightings[0].LightSpaceMatrix);
			float2 shadowTexCoords;
			shadowTexCoords.x = 0.5f + (sPos.x / sPos.w * 0.5f);
			shadowTexCoords.y = 0.5f - (sPos.y / sPos.w * 0.5f);
			if (saturate(shadowTexCoords.x) - shadowTexCoords.x == 0 && saturate(shadowTexCoords.y) - shadowTexCoords.y == 0)
				inShadow = (ShadowMap0.Sample(sampleShadowMap0, shadowTexCoords).r - sPos.z / sPos.w) > 0 ? 1 : 0;

			float3 lightDir = normalize(Lightings[i].LightDir);
			float3 lightStrength = max(Lightings[i].LightColor.rgb * Lightings[i].LightColor.a,0);

			strength += brdf_s(lightDir,viewDir,norm,X,Y,diff) * lightStrength * inShadow;
			inDirect += lightStrength * 0.0628f;
		}
		else if (Lightings[i].LightType == 1)
		{
			float inShadow = 1.0f;
			float3 lightDir = normalize(Lightings[i].LightDir - input.wPos);
			float3 lightStrength = Lightings[i].LightColor.rgb * Lightings[i].LightColor.a / pow(distance(Lightings[i].LightDir, input.wPos), 2);
			strength += brdf_s(lightDir, viewDir, norm, X, Y, diff) * lightStrength;
		}
	}
	for (int i = 1; i < 4; i++)
	{
		if (Lightings[i].LightColor.a != 0)
		if (Lightings[i].LightType == 0)
		{
			float inShadow = 1.0f;
			float3 lightDir = normalize(Lightings[i].LightDir);
			float3 lightStrength = Lightings[i].LightColor.rgb * Lightings[i].LightColor.a;
			strength += brdf_s(lightDir, viewDir, norm, X, Y, diff) * lightStrength;
			inDirect += lightStrength * 0.0628f;
		}
		else if (Lightings[i].LightType == 1)
		{
			float inShadow = 1.0f;
			float3 lightDir = normalize(Lightings[i].LightDir - input.wPos);
			float3 lightStrength = Lightings[i].LightColor.rgb * Lightings[i].LightColor.a / pow(distance(Lightings[i].LightDir ,input.wPos),2);
			strength += brdf_s(lightDir, viewDir, norm, X, Y, diff) * lightStrength;
		}
	}
	//strength += _AmbientColor * diff;
	int sampleCount = pow(2, g_quality) * 2;
	if (dot(viewDir, norm) < 0)
	{
		norm = -norm;
	}
	for (int i = 0; i < sampleCount; i++)
	{
		randomDir = normalize(float3(RNG::Random01(randomState) * 2 - 1, RNG::Random01(randomState) * 2 - 1, RNG::Random01(randomState) * 2 - 1));
		if (dot(randomDir, norm) < 0)
		{
			randomDir = -randomDir;
		}
		strength += brdf_s(randomDir, viewDir, norm, X, Y, diff) * (EnvCube.Sample(s1, randomDir)* g_skyBoxMultiple + inDirect) / sampleCount * dot(randomDir, norm);
	}
	return float4(strength, texColor.a);
}