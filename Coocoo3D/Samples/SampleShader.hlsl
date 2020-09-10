//Coocoo3D会自动加载VS、GS、PS、VS1这四个函数
//VS和GS能在光线追踪中使用
//随着版本更新，这个文件的内容需要相应改动。register也可能会改动。不能保证兼容性。
//使用shader model 5.0
//更多信息请在Coocoo3D源码中的Shaders文件夹里找
#define MAX_BONE_MATRICES 1024

struct LightInfo
{
	float3 LightDir;
	uint LightType;
	float4 LightColor;
	float4x4 LightSpaceMatrix;
};

cbuffer cbAnimMatrices : register(b0)
{
	float4x4 g_mConstBoneWorld[MAX_BONE_MATRICES];
};
cbuffer cb1 : register(b1)
{
	float4x4 g_mWorld;
	float g_posAmount1;
	float3 cb1_preserved1;
	float4 cb1_preserved2[3];
	LightInfo Lightings[4];
};
cbuffer cb2 : register(b2)
{
	float4x4 g_mWorldToProj;
	float4x4 g_mProjToWorld;
	float3   g_vCamPos;
	float g_aspectRatio;
	float g_time;
	float g_deltaTime;
	uint2 g_camera_randomValue;
	float g_skyBoxMultiple;
	float3 g_camera_preserved;
	float4 g_camera_preserved2[5];
};

struct VSSkinnedIn
{
	float3 Pos	: POSITION;			//Position
	float3 PosX	: POSITIONX;		//Position
	float4 Weights : WEIGHTS;		//Bone weights
	uint4  Bones : BONES;			//Bone indices
	float3 Norm : NORMAL;			//Normal
	float2 Tex	: TEXCOORD;		    //Texture coordinate
	float3 Tan : TANGENT;		    //Normalized Tangent vector
	float EdgeScale : EDGESCALE;
};

struct VSSkinnedOut
{
	float4 Pos	: SV_POSITION;		//Position
	float3 Norm : NORMAL;			//Normal
	float2 Tex	: TEXCOORD;		    //Texture coordinate
	float3 Tangent : TANGENT;		//Normalized Tangent vector
	float EdgeScale : EDGESCALE;
};


struct SkinnedInfo
{
	float4 Pos;
	float3 Norm;
	float3 Tan;
};

matrix FetchBoneTransform(uint iBone)
{
	return g_mConstBoneWorld[iBone];
}

SkinnedInfo SkinVert(VSSkinnedIn Input)
{
	SkinnedInfo Output = (SkinnedInfo)0;

	float4 Pos = float4(Input.Pos * (1 - g_posAmount1) + Input.PosX * g_posAmount1, 1);
	float3 Norm = Input.Norm;
	float3 Tan = Input.Tan;

	//Bone0
	uint iBone = Input.Bones.x;
	float fWeight = Input.Weights.x;
	matrix m = FetchBoneTransform(iBone);
	Output.Pos += fWeight * mul(Pos, m);
	Output.Norm += fWeight * mul(float4(Norm, 0), m).xyz;
	Output.Tan += fWeight * mul(float4(Tan, 0), m).xyz;

	//Bone1
	iBone = Input.Bones.y;
	fWeight = Input.Weights.y;
	m = FetchBoneTransform(iBone);
	Output.Pos += fWeight * mul(Pos, m);
	Output.Norm += fWeight * mul(float4(Norm, 0), m).xyz;
	Output.Tan += fWeight * mul(float4(Tan, 0), m).xyz;

	//Bone2
	iBone = Input.Bones.z;
	fWeight = Input.Weights.z;
	m = FetchBoneTransform(iBone);
	Output.Pos += fWeight * mul(Pos, m);
	Output.Norm += fWeight * mul(float4(Norm, 0), m).xyz;
	Output.Tan += fWeight * mul(float4(Tan, 0), m).xyz;

	//Bone3
	iBone = Input.Bones.w;
	fWeight = Input.Weights.w;
	m = FetchBoneTransform(iBone);
	Output.Pos += fWeight * mul(Pos, m);
	Output.Norm += fWeight * mul(float4(Norm, 0), m).xyz;
	Output.Tan += fWeight * mul(float4(Tan, 0), m).xyz;

	return Output;
}


VSSkinnedOut VS(VSSkinnedIn input)
{
	VSSkinnedOut output;

	SkinnedInfo vSkinned = SkinVert(input);
	output.Pos = mul(vSkinned.Pos, g_mWorld);
	output.Norm = normalize(mul(vSkinned.Norm, (float3x3)g_mWorld));
	output.Tangent = normalize(mul(vSkinned.Tan, (float3x3)g_mWorld));
	output.Tex = input.Tex;
	output.EdgeScale = input.EdgeScale;

	return output;
}

[maxvertexcount(3)]
void GS(
	triangle VSSkinnedOut input[3],
	inout TriangleStream< VSSkinnedOut > triStream
)
{
	VSSkinnedOut output;
	float3 norm = normalize(cross(input[0].Pos.xyz - input[1].Pos.xyz, input[1].Pos.xyz - input[2].Pos.xyz));
	for (int i = 0; i < 3; i++)
	{
		output = input[i];
		output.Pos = input[i].Pos + float4((abs(g_time % 4 - 2) / 4) * norm, 0);

		triStream.Append(output);
	}
	triStream.RestartStrip();
}

//Copyright(c) 2016 Unity Technologies

//Permission is hereby granted, free of charge, to any person obtaining a copy of
//this software and associated documentation files(the "Software"), to deal in
//the Software without restriction, including without limitation the rights to
//use, copy, modify, merge, publish, distribute, sublicense, and / or sell copies
//of the Software, and to permit persons to whom the Software is furnished to do
//so, subject to the following conditions :

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
//FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR
//COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
//IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
//CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#define UNITY_PI            3.14159265359f
#define UNITY_TWO_PI        6.28318530718f
#define UNITY_FOUR_PI       12.56637061436f
#define UNITY_INV_PI        0.31830988618f
#define UNITY_INV_TWO_PI    0.15915494309f
#define UNITY_INV_FOUR_PI   0.07957747155f
#define UNITY_HALF_PI       1.57079632679f
#define UNITY_INV_HALF_PI   0.636619772367f

struct UnityLight
{
	half3 color;
	half3 dir;
};

struct UnityIndirect
{
	half3 diffuse;
	half3 specular;
};

float PerceptualRoughnessToRoughness(float perceptualRoughness)
{
	return perceptualRoughness * perceptualRoughness;
}

half RoughnessToPerceptualRoughness(half roughness)
{
	return sqrt(roughness);
}

// Smoothness is the user facing name
// it should be perceptualSmoothness but we don't want the user to have to deal with this name
half SmoothnessToRoughness(half smoothness)
{
	return (1 - smoothness) * (1 - smoothness);
}

float SmoothnessToPerceptualRoughness(float smoothness)
{
	return (1 - smoothness);
}

//-------------------------------------------------------------------------------------

inline half Pow4(half x)
{
	return x * x * x * x;
}

inline float2 Pow4(float2 x)
{
	return x * x * x * x;
}

inline half3 Pow4(half3 x)
{
	return x * x * x * x;
}

inline half4 Pow4(half4 x)
{
	return x * x * x * x;
}

// Pow5 uses the same amount of instructions as generic pow(), but has 2 advantages:
// 1) better instruction pipelining
// 2) no need to worry about NaNs
inline half Pow5(half x)
{
	return x * x * x * x * x;
}

inline half2 Pow5(half2 x)
{
	return x * x * x * x * x;
}

inline half3 Pow5(half3 x)
{
	return x * x * x * x * x;
}

inline half4 Pow5(half4 x)
{
	return x * x * x * x * x;
}

inline half3 FresnelTerm(half3 F0, half cosA)
{
	half t = Pow5(1 - cosA);   // ala Schlick interpoliation
	return F0 + (1 - F0) * t;
}
inline half3 FresnelLerp(half3 F0, half3 F90, half cosA)
{
	half t = Pow5(1 - cosA);   // ala Schlick interpoliation
	return lerp(F0, F90, t);
}
// approximage Schlick with ^4 instead of ^5
inline half3 FresnelLerpFast(half3 F0, half3 F90, half cosA)
{
	half t = Pow4(1 - cosA);
	return lerp(F0, F90, t);
}

// Note: Disney diffuse must be multiply by diffuseAlbedo / PI. This is done outside of this function.
half DisneyDiffuse(half NdotV, half NdotL, half LdotH, half perceptualRoughness)
{
	half fd90 = 0.5 + 2 * LdotH * LdotH * perceptualRoughness;
	// Two schlick fresnel term
	half lightScatter = (1 + (fd90 - 1) * Pow5(1 - NdotL));
	half viewScatter = (1 + (fd90 - 1) * Pow5(1 - NdotV));

	return lightScatter * viewScatter;
}

// NOTE: Visibility term here is the full form from Torrance-Sparrow model, it includes Geometric term: V = G / (N.L * N.V)
// This way it is easier to swap Geometric terms and more room for optimizations (except maybe in case of CookTorrance geom term)

// Generic Smith-Schlick visibility term
inline half SmithVisibilityTerm(half NdotL, half NdotV, half k)
{
	half gL = NdotL * (1 - k) + k;
	half gV = NdotV * (1 - k) + k;
	return 1.0 / (gL * gV + 1e-5f); // This function is not intended to be running on Mobile,
									// therefore epsilon is smaller than can be represented by half
}

// Smith-Schlick derived for Beckmann
inline half SmithBeckmannVisibilityTerm(half NdotL, half NdotV, half roughness)
{
	half c = 0.797884560802865h; // c = sqrt(2 / Pi)
	half k = roughness * c;
	return SmithVisibilityTerm(NdotL, NdotV, k) * 0.25f; // * 0.25 is the 1/4 of the visibility term
}

// Ref: http://jcgt.org/published/0003/02/03/paper.pdf
inline float SmithJointGGXVisibilityTerm(float NdotL, float NdotV, float roughness)
{
#if 0
	// Original formulation:
	//  lambda_v    = (-1 + sqrt(a2 * (1 - NdotL2) / NdotL2 + 1)) * 0.5f;
	//  lambda_l    = (-1 + sqrt(a2 * (1 - NdotV2) / NdotV2 + 1)) * 0.5f;
	//  G           = 1 / (1 + lambda_v + lambda_l);

	// Reorder code to be more optimal
	half a = roughness;
	half a2 = a * a;

	half lambdaV = NdotL * sqrt((-NdotV * a2 + NdotV) * NdotV + a2);
	half lambdaL = NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);

	// Simplify visibility term: (2.0f * NdotL * NdotV) /  ((4.0f * NdotL * NdotV) * (lambda_v + lambda_l + 1e-5f));
	return 0.5f / (lambdaV + lambdaL + 1e-5f);  // This function is not intended to be running on Mobile,
												// therefore epsilon is smaller than can be represented by half
#else
	// Approximation of the above formulation (simplify the sqrt, not mathematically correct but close enough)
	float a = roughness;
	float lambdaV = NdotL * (NdotV * (1 - a) + a);
	float lambdaL = NdotV * (NdotL * (1 - a) + a);

#if defined(SHADER_API_SWITCH)
	return 0.5f / (lambdaV + lambdaL + 1e-4f); // work-around against hlslcc rounding error
#else
	return 0.5f / (lambdaV + lambdaL + 1e-5f);
#endif

#endif
}

inline float GGXTerm(float NdotH, float roughness)
{
	float a2 = roughness * roughness;
	float d = (NdotH * a2 - NdotH) * NdotH + 1.0f; // 2 mad
	return UNITY_INV_PI * a2 / (d * d + 1e-7f); // This function is not intended to be running on Mobile,
											// therefore epsilon is smaller than what can be represented by half
}

inline half PerceptualRoughnessToSpecPower(half perceptualRoughness)
{
	half m = PerceptualRoughnessToRoughness(perceptualRoughness);   // m is the true academic roughness.
	half sq = max(1e-4f, m * m);
	half n = (2.0 / sq) - 2.0;                          // https://dl.dropboxusercontent.com/u/55891920/papers/mm_brdf.pdf
	n = max(n, 1e-4f);                                  // prevent possible cases of pow(0,0), which could happen when roughness is 1.0 and NdotH is zero
	return n;
}

// BlinnPhong normalized as normal distribution function (NDF)
// for use in micro-facet model: spec=D*G*F
// eq. 19 in https://dl.dropboxusercontent.com/u/55891920/papers/mm_brdf.pdf
inline half NDFBlinnPhongNormalizedTerm(half NdotH, half n)
{
	// norm = (n+2)/(2*pi)
	half normTerm = (n + 2.0) * (0.5 / UNITY_PI);

	half specTerm = pow(NdotH, n);
	return specTerm * normTerm;
}

//-------------------------------------------------------------------------------------
/*
// https://s3.amazonaws.com/docs.knaldtech.com/knald/1.0.0/lys_power_drops.html

const float k0 = 0.00098, k1 = 0.9921;
// pass this as a constant for optimization
const float fUserMaxSPow = 100000; // sqrt(12M)
const float g_fMaxT = ( exp2(-10.0/fUserMaxSPow) - k0)/k1;
float GetSpecPowToMip(float fSpecPow, int nMips)
{
   // Default curve - Inverse of TB2 curve with adjusted constants
   float fSmulMaxT = ( exp2(-10.0/sqrt( fSpecPow )) - k0)/k1;
   return float(nMips-1)*(1.0 - clamp( fSmulMaxT/g_fMaxT, 0.0, 1.0 ));
}

	//float specPower = PerceptualRoughnessToSpecPower(perceptualRoughness);
	//float mip = GetSpecPowToMip (specPower, 7);
*/

inline float3 Unity_SafeNormalize(float3 inVec)
{
	float dp3 = max(0.001f, dot(inVec, inVec));
	return inVec * rsqrt(dp3);
}

//-------------------------------------------------------------------------------------

// Note: BRDF entry points use smoothness and oneMinusReflectivity for optimization
// purposes, mostly for DX9 SM2.0 level. Most of the math is being done on these (1-x) values, and that saves
// a few precious ALU slots.


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
half4 BRDF1_Unity_PBS(half3 diffColor, half3 specColor, half oneMinusReflectivity, half smoothness,
	float3 normal, float3 viewDir,
	UnityLight light, UnityIndirect gi)
{
	float perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness);
	float3 halfDir = Unity_SafeNormalize(float3(light.dir) + viewDir);

	// NdotV should not be negative for visible pixels, but it can happen due to perspective projection and normal mapping
	// In this case normal should be modified to become valid (i.e facing camera) and not cause weird artifacts.
	// but this operation adds few ALU and users may not want it. Alternative is to simply take the abs of NdotV (less correct but works too).
	// Following define allow to control this. Set it to 0 if ALU is critical on your platform.
	// This correction is interesting for GGX with SmithJoint visibility function because artifacts are more visible in this case due to highlight edge of rough surface
	// Edit: Disable this code by default for now as it is not compatible with two sided lighting used in SpeedTree.
#define UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV 0

#if UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV
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
	half diffuseTerm = DisneyDiffuse(nv, nl, lh, perceptualRoughness) * nl;

	// Specular term
	// HACK: theoretically we should divide diffuseTerm by Pi and not multiply specularTerm!
	// BUT 1) that will make shader look significantly darker than Legacy ones
	// and 2) on engine side "Non-important" lights have to be divided by Pi too in cases when they are injected into ambient SH
	float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
#if UNITY_BRDF_GGX
	// GGX with roughtness to 0 would mean no specular at all, using max(roughness, 0.002) here to match HDrenderloop roughtness remapping.
	roughness = max(roughness, 0.002);
	float V = SmithJointGGXVisibilityTerm(nl, nv, roughness);
	float D = GGXTerm(nh, roughness);
#else
	// Legacy
	half V = SmithBeckmannVisibilityTerm(nl, nv, roughness);
	half D = NDFBlinnPhongNormalizedTerm(nh, PerceptualRoughnessToSpecPower(perceptualRoughness));
#endif

	float specularTerm = V * D * UNITY_PI; // Torrance-Sparrow model, Fresnel is applied later

#   ifdef UNITY_COLORSPACE_GAMMA
	specularTerm = sqrt(max(1e-4h, specularTerm));
#   endif

	// specularTerm * nl can be NaN on Metal in some cases, use max() to make sure it's a sane value
	specularTerm = max(0, specularTerm * nl);
#if defined(_SPECULARHIGHLIGHTS_OFF)
	specularTerm = 0.0;
#endif

	// surfaceReduction = Int D(NdotH) * NdotH * Id(NdotL>0) dH = 1/(roughness^2+1)
	half surfaceReduction;
#   ifdef UNITY_COLORSPACE_GAMMA
	surfaceReduction = 1.0 - 0.28 * roughness * perceptualRoughness;      // 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
#   else
	surfaceReduction = 1.0 / (roughness * roughness + 1.0);           // fade \in [0.5;1]
#   endif

	// To provide true Lambert lighting, we need to be able to kill specular completely.
	specularTerm *= any(specColor) ? 1.0 : 0.0;

	half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));
	half3 color = diffColor * (gi.diffuse + light.color * diffuseTerm)
		+ specularTerm * light.color * FresnelTerm(specColor, lh)
		+ surfaceReduction * gi.specular * FresnelLerp(specColor, grazingTerm, nv);

	return half4(color, 1);
}


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
Texture2D texture0 :register(t0);
SamplerState s0 : register(s0);
Texture2D texture1 :register(t1);
SamplerState s1 : register(s1);
Texture2D ShadowMap0:register(t2);
SamplerState sampleShadowMap0 : register(s2);
TextureCube EnvCube : register (t3);
TextureCube IrradianceCube : register (t4);
struct PSIn
{
	float4 Pos	: SV_POSITION;		//Position
	float4 wPos	: POSITION;			//world space Pos
	float3 Norm : NORMAL;			//Normal
	float2 TexCoord	: TEXCOORD;		//Texture coordinate
	float3 Tangent : TANGENT;		//Normalized Tangent vector
};
float4 PS(PSIn input) : SV_TARGET
{
	float3 strength = float3(0,0,0);
	float3 viewDir = normalize(g_vCamPos - input.wPos);
	float3 norm = normalize(input.Norm);
	float4 texColor = texture0.Sample(s0, input.TexCoord) * _DiffuseColor;
	clip(texColor.a - 0.01f);
	float3 diff = texColor.rgb;
	float3 specCol = clamp(_SpecularColor.rgb, 0.0001, 1);
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
			UnityLight light;
			light.color = lightStrength * inShadow;
			light.dir = lightDir;
			UnityIndirect indirect;
			indirect.diffuse = lightStrength * 0.001f * diff;
			indirect.specular = lightStrength * 0.001f;
			strength += BRDF1_Unity_PBS(diff, specCol, _Metallic,1 - _Roughness, norm, viewDir, light, indirect);
		}
		else if (Lightings[i].LightType == 1)
		{
			float inShadow = 1.0f;
			float3 lightDir = normalize(Lightings[i].LightDir - input.wPos);
			float3 lightStrength = Lightings[i].LightColor.rgb * Lightings[i].LightColor.a / pow(distance(Lightings[i].LightDir, input.wPos), 2);
			UnityLight light;
			light.color = lightStrength;
			light.dir = lightDir;
			UnityIndirect indirect;
			indirect.diffuse = 0;
			indirect.specular = 0;
			strength += BRDF1_Unity_PBS(diff, specCol, _Metallic,1 - _Roughness, norm, viewDir, light, indirect);
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
			UnityLight light;
			light.color = lightStrength;
			light.dir = lightDir;
			UnityIndirect indirect;
			indirect.diffuse = lightStrength * 0.001f * diff;
			indirect.specular = lightStrength * 0.001f;
			strength += BRDF1_Unity_PBS(diff, specCol, _Metallic,1 - _Roughness, norm, viewDir, light, indirect);
		}
		else if (Lightings[i].LightType == 1)
		{
			float inShadow = 1.0f;
			float3 lightDir = normalize(Lightings[i].LightDir - input.wPos);
			float3 lightStrength = Lightings[i].LightColor.rgb * Lightings[i].LightColor.a / pow(distance(Lightings[i].LightDir ,input.wPos),2);
			UnityLight light;
			light.color = lightStrength;
			light.dir = lightDir;
			UnityIndirect indirect;
			indirect.diffuse = 0;
			indirect.specular = 0;
			strength += BRDF1_Unity_PBS(diff, specCol, _Metallic, 1 - _Roughness, norm, viewDir, light, indirect);
		}
	}
	UnityLight light1;
	light1.color = float4(0,0,0,1);
	light1.dir = float3(0,1,0);
	UnityIndirect indirect1;
	indirect1.diffuse = IrradianceCube.Sample(s1, norm) * g_skyBoxMultiple;
	indirect1.specular = EnvCube.Sample(s1, reflect(-viewDir, norm)) * g_skyBoxMultiple;
	strength += BRDF1_Unity_PBS(diff, specCol, _Metallic, 1 - _Roughness, norm, viewDir, light1, indirect1);

	return float4(strength * float3(0.5f,1,1), texColor.a);
	//return float4(input.Tangent*0.5+0.5, texColor.a);
}