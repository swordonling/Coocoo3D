cbuffer cb0 : register(b0)
{
	float4x4 g_mWorld;
};
struct LightInfo
{
	float3 LightDir;
	uint LightType;
	float4 LightColor;
	float4x4 LightSpaceMatrix;
};
cbuffer cb1 : register(b1)
{
	LightInfo Lightings[4];
};
cbuffer cb2 : register(b2)
{
	float4 _DiffuseColor;
	float4 _SpecularColor;
	float3 _AmbientColor;
	float _EdgeScale;
	float4 _EdgeColor;

	float4 _Texture;
	float4 _SubTexture;
	float4 _ToonTexture;
};
cbuffer cb3 : register(b3)
{
	float4x4 g_mWorldToProj;
	float3   g_vCamPos;
	float g_aspectRatio;
	float _Time;
	float _DeltaTime;
};

Texture2D texture0 :register(t0);
SamplerState s0 : register(s0);
Texture2D texture1 :register(t1);
SamplerState s1 : register(s1);
Texture2D ShadowMap0:register(t3);
SamplerComparisonState sampleShadowMap0 : register(s3);

struct PSSkinnedIn
{
	float4 Pos	: SV_POSITION;		//Position
	float4 wPos	: POSWORLD;			//world space Pos
	float3 Norm : NORMAL;			//Normal
	float2 TexCoord	: TEXCOORD;		//Texture coordinate
	float3 Tangent : TANGENT;		//Normalized Tangent vector
};
float4 main(PSSkinnedIn input) : SV_TARGET
{
	float3 strength = float3(0,0,0);
	float3 specularStrength = float3(0, 0, 0);
	float3 viewDir = normalize(g_vCamPos - input.wPos);
	float3 norm = normalize(input.Norm);

	for (int i = 0; i < 1; i++)
	{
		float inShadow = 1.0f;
		float4 sPos = mul(input.wPos, Lightings[0].LightSpaceMatrix);
		float2 shadowTexCoords;
		shadowTexCoords.x = 0.5f + (sPos.x / sPos.w * 0.5f);
		shadowTexCoords.y = 0.5f - (sPos.y / sPos.w * 0.5f);
		if (saturate(shadowTexCoords.x) - shadowTexCoords.x == 0 && saturate(shadowTexCoords.y) - shadowTexCoords.y == 0)
			inShadow = ShadowMap0.SampleCmpLevelZero(sampleShadowMap0, shadowTexCoords, sPos.z / sPos.w - 0.001f);

		float3 lightDir = normalize(Lightings[i].LightDir);
		float3 lightStrength = Lightings[i].LightColor.rgb*Lightings[i].LightColor.a;
		float3 halfAngle = normalize(viewDir + lightDir);
		specularStrength += saturate(pow(saturate(dot(halfAngle, norm)), 64)*lightStrength*_SpecularColor.rgb*inShadow)*_SpecularColor.a;
		strength += lightStrength * (0.12f + saturate(dot(norm, lightDir)*0.88f)*inShadow);
	}
	for (int i = 1; i < 4; i++)
	{
		float3 lightDir = normalize(Lightings[i].LightDir);
		float3 lightStrength = Lightings[i].LightColor.rgb*Lightings[i].LightColor.a;
		float3 halfAngle = normalize(viewDir + lightDir);
		specularStrength += saturate(pow(saturate(dot(halfAngle, norm)), 64)*lightStrength*_SpecularColor.rgb)*_SpecularColor.a;
		strength += lightStrength * (0.12f + saturate(dot(norm, lightDir)*0.88f));
	}
	strength += _AmbientColor;
	return pow(texture0.Sample(s0,input.TexCoord)*float4(strength,1)*_DiffuseColor + float4(specularStrength,0),1 / 2.2f);
}