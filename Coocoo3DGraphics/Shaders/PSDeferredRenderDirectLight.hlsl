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
	float4x4 LightSpaceMatrices[2];
	LightInfo Lightings[1];
};
cbuffer cb2 : register(b2)
{
	CAMERA_DATA_DEFINE;//is a macro
};
Texture2D texture0 :register(t0);
Texture2D texture1 :register(t1);

Texture2D gbufferDepth : register (t3);
Texture2D ShadowMap0 : register (t4);
SamplerState s0 : register(s0);
SamplerComparisonState sampleShadowMap0 : register(s2);

half3 NormalDecode(half2 enc)
{
	half2 fenc = enc * 4 - 2;
	half f = dot(fenc, fenc);
	half g = sqrt(1 - f / 4);
	half3 n;
	n.xy = fenc * g;
	n.z = 1 - f / 2;
	return n;
}

struct PSIn
{
	float4 Pos	: SV_POSITION;		//Position
	float2 uv	: TEXCOORD;
};
float4 main(PSIn input) : SV_TARGET
{
	float2 uv = input.uv * 0.5 + 0.5;
	uv.y = 1 - uv.y;
	float4 buffer0Color = texture0.Sample(s0, uv);
	clip(buffer0Color.a - 0.99);
	float4 buffer1Color = texture1.Sample(s0, uv);

	float4 vx = mul(float4(input.uv,0,1),g_mProjToWorld);
	float3 V1 = vx.xyz / vx.w - g_vCamPos;
	float3 V = normalize(-V1);

	float3 N = normalize(NormalDecode(buffer1Color.rg));
	float NdotV = saturate(dot(N, V));
	float3 albedo = buffer0Color.rgb;
	float roughness = buffer1Color.b;
	float alpha = roughness * roughness;
	float metallic = buffer1Color.a;
	float3 c_diffuse = lerp(albedo * (1 - 0.04), 0, metallic);
	float3 c_specular = lerp(0.04f, albedo, metallic);

	float depth1 = gbufferDepth.SampleLevel(s0, uv, 0).r;
	float4 test1 = mul(float4(input.uv, depth1, 1), g_mProjToWorld);
	test1 /= test1.w;
	float4 wPos = float4(test1.xyz,1);

	float3 outputColor = float3(0, 0, 0);

	for (int i = 0; i < 1; i++)
	{
		if (Lightings[i].LightColor.a == 0)continue;
		if (Lightings[i].LightType == 0)
		{
			float inShadow = 1.0f;
			float3 lightStrength = max(Lightings[i].LightColor.rgb * Lightings[i].LightColor.a, 0);
			if (g_enableShadow != 0)
			{
				float4 sPos = mul(wPos, LightSpaceMatrices[0]);
				sPos = sPos / sPos.w;

				float2 shadowTexCoords;
				shadowTexCoords.x = 0.5f + (sPos.x * 0.5f);
				shadowTexCoords.y = 0.5f - (sPos.y * 0.5f);
				if (sPos.x >= -1 && sPos.x <= 1 && sPos.y >= -1 && sPos.y <= 1)
					inShadow = ShadowMap0.SampleCmpLevelZero(sampleShadowMap0, shadowTexCoords, sPos.z).r;
				//else
				//{
				//	sPos = mul(input.wPos, LightSpaceMatrices[1]);
				//	sPos = sPos / sPos.w;
				//	float2 shadowTexCoords1;
				//	shadowTexCoords1.x = 0.5f + (sPos.x * 0.5f);
				//	shadowTexCoords1.y = 0.5f - (sPos.y * 0.5f);

				//	if (sPos.x >= -1 && sPos.x <= 1 && sPos.y >= -1 && sPos.y <= 1)
				//		inShadow = ShadowMap1.SampleCmpLevelZero(sampleShadowMap0, shadowTexCoords1, sPos.z).r;
				//}
			}

			float3 L = normalize(Lightings[i].LightDir);
			float3 H = normalize(L + V);

			float3 NdotL = saturate(dot(N, L));
			float3 LdotH = saturate(dot(L, H));
			float3 NdotH = saturate(dot(N, H));

			float diffuse_factor = Diffuse_Burley(NdotL, NdotV, LdotH, roughness);
			float3 specular_factor = Specular_BRDF(alpha, c_specular, NdotV, NdotL, LdotH, NdotH);

			outputColor += NdotL * lightStrength * (((c_diffuse * diffuse_factor / COO_PI) + specular_factor)) * inShadow;
		}
	}
	return float4(outputColor, 1);
}