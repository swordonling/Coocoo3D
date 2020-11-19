#include "CameraDataDefine.hlsli"
cbuffer cb2 : register(b2)
{
	CAMERA_DATA_DEFINE;//is a macro
};
Texture2D texture0 :register(t0);
Texture2D texture1 :register(t1);

TextureCube EnvCube : register (t3);
TextureCube IrradianceCube : register (t4);
Texture2D BRDFLut : register(t5);
SamplerState s0 : register(s0);

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
	float4 vx = mul(float4(input.uv,0,1),g_mProjToWorld);
	float3 V1 = vx.xyz / vx.w - g_vCamPos;
	float3 V = normalize(-V1);
	float2 uv = input.uv * 0.5 + 0.5;
	uv.y = 1 - uv.y;

	float4 buffer0Color = texture0.Sample(s0, uv);
	float4 buffer1Color = texture1.Sample(s0, uv);

	if (buffer0Color.a > 0)
	{
		float3 N = normalize(NormalDecode(buffer1Color.rg));
		float NdotV = saturate(dot(N, V));
		float3 albedo = buffer0Color.rgb;
		float roughness = buffer1Color.b;
		float metallic = buffer1Color.a;
		float3 c_diffuse = lerp(albedo * (1 - 0.04), 0, metallic);
		float3 c_specular = lerp(0.04f, albedo, metallic);
		float2 AB = BRDFLut.SampleLevel(s0, float2(NdotV, 1 - roughness), 0).rg;
		float3 GF = c_specular * AB.x + AB.y;
		float3 outputColor = float3(0, 0, 0);
		outputColor += IrradianceCube.Sample(s0, N) * g_skyBoxMultiple * c_diffuse;
		outputColor += EnvCube.SampleLevel(s0, reflect(-V, N), sqrt(max(roughness, 1e-5)) * 6) * g_skyBoxMultiple * GF;

		return float4(outputColor, 1);
	}
	else
	{
		float3 EnvColor = EnvCube.Sample(s0, V1).rgb * g_skyBoxMultiple;
		return float4(EnvColor, 0);
	}
}