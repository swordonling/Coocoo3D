#define UNITY_BRDF_GGX 1
#include "FromUnity/UnityStandardBRDF.hlsli"
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


// Main Physically Based BRDF
// Derived from Disney work and based on Torrance-Sparrow micro-facet model
//
//   BRDF = kD / pi + kS * (D * V * F) / 4
//   I = BRDF * NdotL
//
// * NDF (depending on UNITY_BRDF_GGX):
//  a) Normalized BlinnPhong
//  b) GGX
// * Smith for Visiblity term
// * Schlick approximation for Fresnel
half4 Toon_Shadering1(half3 diffColor, half3 specColor, half oneMinusReflectivity, half smoothness,
	float3 normal, float3 viewDir,
	UnityLight light, UnityIndirect gi)
{
	float perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness);
	float3 halfDir = Unity_SafeNormalize(float3(light.dir) + viewDir);

	// nv should not be negative for visible pixels, but it can happen due to perspective projection and normal mapping
	// In this case normal should be modified to become valid (i.e facing camera) and not cause weird artifacts.
	// but this operation adds few ALU and users may not want it. Alternative is to simply take the abs of nv (less correct but works too).
	// Following define allow to control this. Set it to 0 if ALU is critical on your platform.
	// This correction is interesting for GGX with SmithJoint visibility function because artifacts are more visible in this case due to highlight edge of rough surface
	// Edit: Disable this code by default for now as it is not compatible with two sided lighting used in SpeedTree.
#define UNITY_HANDLE_CORRECTLY_NEGATIVE_nv 0

#if UNITY_HANDLE_CORRECTLY_NEGATIVE_nv
	// The amount we shift the normal toward the view vector is defined by the dot product.
	half shiftAmount = dot(normal, viewDir);
	normal = shiftAmount < 0.0f ? normal + viewDir * (-shiftAmount + 1e-5f) : normal;
	// A re-normalization should be applied here but as the shift is small we don't do it to save ALU.
	//normal = normalize(normal);

	float nv = saturate(dot(normal, viewDir)); // TODO: this saturate should no be necessary here
#else
	half nv = abs(dot(normal, viewDir));    // This abs allow to limit artifact
#endif

	float nl = saturate(dot(normal, light.dir));
	float nh = saturate(dot(normal, halfDir));

	half lv = saturate(dot(light.dir, viewDir));
	half lh = saturate(dot(light.dir, halfDir));

	// Diffuse term
	//half diffuseTerm = DisneyDiffuse(nv, nl, lh, perceptualRoughness) * nl;
	//float diffuseTerm1 = DisneyDiffuse(nv, nl, lh, perceptualRoughness) * nl;
	half diffuseTerm = smoothstep(0.1, 0.4, DisneyDiffuse(nv, nl, lh, perceptualRoughness) * nl);

	// Specular term
	// HACK: theoretically we should divide diffuseTerm by Pi and not multiply specularTerm!
	// BUT 1) that will make shader look significantly darker than Legacy ones
	// and 2) on engine side "Non-important" lights have to be divided by Pi too in cases when they are injected into ambient SH
	float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
	// Legacy
	half V = SmithBeckmannVisibilityTerm(nl, nv, roughness);
	half D = NDFBlinnPhongNormalizedTerm(nh, PerceptualRoughnessToSpecPower(perceptualRoughness));

	float specularTerm = V * D * UNITY_PI; // Torrance-Sparrow model, Fresnel is applied later

	// specularTerm * nl can be NaN on Metal in some cases, use max() to make sure it's a sane value
	specularTerm = max(0, specularTerm * nl);
#if defined(_SPECULARHIGHLIGHTS_OFF)
	specularTerm = 0.0;
#endif

	// surfaceReduction = Int D(nh) * nh * Id(NdotL>0) dH = 1/(roughness^2+1)
	half surfaceReduction;
	surfaceReduction = 1.0 / (roughness * roughness + 1.0);           // fade \in [0.5;1]

	// To provide true Lambert lighting, we need to be able to kill specular completely.
	specularTerm *= any(specColor) ? 1.0 : 0.0;

	half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));
	//half3 color = diffColor * (gi.diffuse + light.color * diffuseTerm)
	//	+ specularTerm * light.color * FresnelTerm(specColor, lh)
	//	+ surfaceReduction * gi.specular * FresnelLerp(specColor, grazingTerm, nv);

	float3 diffuseResult = diffColor * (gi.diffuse + light.color * diffuseTerm);
	//float3 diffuseResult = float3(0,0,0);
	float3 diffSpecResult = lerp(diffuseResult, diffuseResult + specularTerm * light.color, smoothstep(0.1, 0.5, FresnelTerm(specColor, lh)));
	float3 diffSpecRigResult = lerp(diffSpecResult, diffSpecResult + surfaceReduction * gi.specular, smoothstep(0.1, 0.3, FresnelLerp(specColor, grazingTerm, nv)));
	float3 color = diffSpecRigResult;
	//float3 color = diffSpecResult;

	return half4(color, 1);
}



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
	float3 strength = float3(0,0,0);
	float3 viewDir = normalize(g_vCamPos - input.wPos);
	float3 norm = normalize(input.Norm);
	float4 texColor = texture0.Sample(s0, input.TexCoord) * _DiffuseColor;
	clip(texColor.a - 0.01f);
	float3 diff = texColor.rgb;
	float3 specCol = clamp(_SpecularColor.rgb, 0.0001, 1);
	UnityIndirect indirect1;
	indirect1.diffuse = IrradianceCube.Sample(s1, norm) * g_skyBoxMultiple;
	indirect1.specular = EnvCube.Sample(s1, reflect(-viewDir, norm)) * g_skyBoxMultiple;
	for (int i = 0; i < 1; i++)
	{
		if (Lightings[i].LightColor.a != 0)
		{
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
				UnityLight light;
				light.color = lightStrength * inShadow;
				light.dir = lightDir;
				strength += Toon_Shadering1(diff, specCol, _Metallic,1 - _Roughness, norm, viewDir, light, indirect1);
			}
			else if (Lightings[i].LightType == 1)
			{
				float inShadow = 1.0f;
				float3 lightDir = normalize(Lightings[i].LightDir - input.wPos);
				float3 lightStrength = Lightings[i].LightColor.rgb * Lightings[i].LightColor.a / pow(distance(Lightings[i].LightDir, input.wPos), 2);
				UnityLight light;
				light.color = lightStrength;
				light.dir = lightDir;
				strength += Toon_Shadering1(diff, specCol, _Metallic,1 - _Roughness, norm, viewDir, light, indirect1);
			}
		}
		else
		{
			UnityLight light;
			light.color = float4(0,0,0,0);
			light.dir = float3(0,1,0);
			strength += Toon_Shadering1(diff, specCol, _Metallic, 1 - _Roughness, norm, viewDir, light, indirect1);
		}
	}

	return float4(strength, texColor.a);
	//return float4(input.Tangent*0.5+0.5, texColor.a);
}