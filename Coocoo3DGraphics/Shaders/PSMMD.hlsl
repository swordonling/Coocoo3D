#include "BRDF/PBR.hlsli"
#include "CameraDataDefine.hlsli"
struct LightInfo
{
	float3 LightDir;
	uint LightType;
	float4 LightColor;
};

cbuffer cb1 : register(b1)
{
	float4x4 LightSpaceMatrices[1];
	LightInfo Lightings[4];
};
cbuffer cb2 : register(b2)
{
	CAMERA_DATA_DEFINE;//is a macro
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
SamplerState s0 : register(s0);
SamplerState s1 : register(s1);
SamplerComparisonState sampleShadowMap0 : register(s2);
Texture2D texture0 :register(t0);
Texture2D texture1 :register(t1);
Texture2D ShadowMap0:register(t2);
TextureCube EnvCube : register (t3);
TextureCube IrradianceCube : register (t4);
Texture2D BRDFLut : register(t5);
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
	float4 texColor = texture0.Sample(s1, input.TexCoord) * _DiffuseColor;
	clip(texColor.a - 0.01f);

	float3 V = normalize(g_vCamPos - input.wPos);
	float3 N = normalize(input.Norm);
	float NdotV = saturate(dot(N, V));

	// Burley roughness bias
	float roughness = max(_Roughness,0.002);
	float alpha = roughness * roughness;

	float3 albedo = texColor.rgb;

	float xxx = (_Specular * 0.08f + _Metallic * (1 - _Specular * 0.08f));

	float3 c_diffuse = lerp(albedo * (1 - _Specular * 0.08f), 0, _Metallic);
	float3 c_specular = lerp(_Specular * 0.08f, albedo, _Metallic);

	float3 outputColor = float3(0,0,0);
	for (int i = 0; i < 1; i++)
	{
		if (Lightings[i].LightColor.a == 0)continue;
		if (Lightings[i].LightType == 0)
		{
			float inShadow = 1.0f;
			float3 lightStrength = max(Lightings[i].LightColor.rgb * Lightings[i].LightColor.a, 0);

			float4 sPos = mul(input.wPos, LightSpaceMatrices[0]);
			float2 shadowTexCoords;
			shadowTexCoords.x = 0.5f + (sPos.x / sPos.w * 0.5f);
			shadowTexCoords.y = 0.5f - (sPos.y / sPos.w * 0.5f);
			if (saturate(shadowTexCoords.x) - shadowTexCoords.x == 0 && saturate(shadowTexCoords.y) - shadowTexCoords.y == 0 && g_enableShadow != 0)
				inShadow = ShadowMap0.SampleCmpLevelZero(sampleShadowMap0, shadowTexCoords,sPos.z / sPos.w).r;

			float3 L = normalize(Lightings[i].LightDir);
			float3 H = normalize(L + V);

			float3 NdotL = saturate(dot(N, L));
			float3 LdotH = saturate(dot(L, H));
			float3 NdotH = saturate(dot(N, H));

			float diffuse_factor = Diffuse_Burley(NdotL, NdotV, LdotH, roughness);
			float3 specular_factor = Specular_BRDF(alpha, c_specular, NdotV, NdotL, LdotH, NdotH);

			outputColor += NdotL * lightStrength * (((c_diffuse * diffuse_factor / COO_PI) + specular_factor)) * inShadow;
		}
		else if (Lightings[i].LightType == 1)
		{
			float inShadow = 1.0f;
			float3 lightStrength = Lightings[i].LightColor.rgb * Lightings[i].LightColor.a / pow(distance(Lightings[i].LightDir, input.wPos), 2);

			float3 L = normalize(Lightings[i].LightDir - input.wPos);
			float3 H = normalize(L + V);

			float3 NdotL = saturate(dot(N, L));
			float3 LdotH = saturate(dot(L, H));
			float3 NdotH = saturate(dot(N, H));

			float diffuse_factor = Diffuse_Burley(NdotL, NdotV, LdotH, roughness);
			float3 specular_factor = Specular_BRDF(alpha, c_specular, NdotV, NdotL, LdotH, NdotH);

			outputColor += NdotL * lightStrength * (((c_diffuse * diffuse_factor / COO_PI) + specular_factor)) * inShadow;
		}
	}
	for (int i = 1; i < 4; i++)
	{
		if (Lightings[i].LightColor.a == 0)continue;
		if (Lightings[i].LightType == 0)
		{
			float inShadow = 1.0f;
			float3 lightStrength = max(Lightings[i].LightColor.rgb * Lightings[i].LightColor.a, 0);

			float3 L = normalize(Lightings[i].LightDir);
			float3 H = normalize(L + V);

			float3 NdotL = saturate(dot(N, L));
			float3 LdotH = saturate(dot(L, H));
			float3 NdotH = saturate(dot(N, H));

			float diffuse_factor = Diffuse_Burley(NdotL, NdotV, LdotH, roughness);
			float3 specular_factor = Specular_BRDF(alpha, c_specular, NdotV, NdotL, LdotH, NdotH);

			outputColor += NdotL * lightStrength * (((c_diffuse * diffuse_factor / COO_PI) + specular_factor)) * inShadow;
		}
		else if (Lightings[i].LightType == 1)
		{
			float inShadow = 1.0f;
			float3 lightStrength = Lightings[i].LightColor.rgb * Lightings[i].LightColor.a / pow(distance(Lightings[i].LightDir, input.wPos), 2);

			float3 L = normalize(Lightings[i].LightDir - input.wPos);
			float3 H = normalize(L + V);

			float3 NdotL = saturate(dot(N, L));
			float3 LdotH = saturate(dot(L, H));
			float3 NdotH = saturate(dot(N, H));

			float diffuse_factor = Diffuse_Burley(NdotL, NdotV, LdotH, roughness);
			float3 specular_factor = Specular_BRDF(alpha, c_specular, NdotV, NdotL, LdotH, NdotH);

			outputColor += NdotL * lightStrength * (((c_diffuse * diffuse_factor / COO_PI) + specular_factor)) * inShadow;
		}
	}
	float surfaceReduction = 1.0f / (roughness * roughness + 1.0f);
	float grazingTerm = saturate(1 - sqrt(roughness) + xxx);
	//float2 AB = BRDFLut.SampleLevel(s0, float2(NdotV, roughness), 0).rg;
	float3 F = Fresnel_SchlickRoughness(saturate(NdotV), c_specular, roughness);
	float3 kS = F;
	float3 kD = 1.0 - kS;
	kD *= 1.0 - _Metallic;

	outputColor += IrradianceCube.Sample(s0, N) * g_skyBoxMultiple * c_diffuse;
	outputColor += EnvCube.SampleLevel(s0, reflect(-V, N), roughness * 6) * g_skyBoxMultiple * surfaceReduction * Fresnel_Shlick(c_specular, grazingTerm, NdotV);
	outputColor += _Emission * _AmbientColor;

	return float4(outputColor, texColor.a);
}