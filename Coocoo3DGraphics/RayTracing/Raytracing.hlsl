#ifndef RAYTRACING_HLSL
#define RAYTRACING_HLSL
#define UNITY_BRDF_GGX 1
#include "../Shaders/FromUnity/UnityStandardBRDF.hlsli"
#include "../Shaders/CameraDataDefine.hlsli"
#include "../Shaders/RandomNumberGenerator.hlsli"

typedef BuiltInTriangleIntersectionAttributes MyAttributes;
struct RayPayload
{
	float4 color;
	float3 direction;
	uint depth;
};
static const int c_testRayIndex = 1;
struct TestRayPayload
{
	bool miss;
	float3 hitPos;
	float4 color;
};

struct LightInfo
{
	float3 dir;
	uint LightType;
	float4 LightColor;
};

struct PSSkinnedIn
{
	float3 Pos;
	float3 Norm;
	float2 Tex;
	float3 Tangent;
	float EdgeScale;
	float4 preserved1;
};

struct Ray
{
	float3 origin;
	float3 direction;
};

RaytracingAccelerationStructure Scene : register(t0);
TextureCube EnvCube : register (t1);
TextureCube IrradianceCube : register (t2);
RWTexture2D<float4> g_renderTarget : register(u0);
cbuffer cb0 : register(b0)
{
	CAMERA_DATA_DEFINE//is a macro
};
//local
StructuredBuffer<PSSkinnedIn> Vertices : register(t1, space1);
Texture2D diffuseMap :register(t2, space1);
SamplerState s0 : register(s0);
//
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
	float4 preserved1[6];
	LightInfo _LightInfo[8];
};

inline Ray GenerateCameraRay(uint2 index, in float3 cameraPosition, in float4x4 projectionToWorld)
{
	float2 xy = index + 0.5f; // center in the middle of the pixel.
	float2 screenPos = xy / DispatchRaysDimensions().xy * 2.0 - 1.0;

	screenPos.y = -screenPos.y;

	float4 world = mul(float4(screenPos, 0, 1), projectionToWorld);
	world.xyz /= world.w;

	Ray ray;
	ray.origin = cameraPosition;
	ray.direction = normalize(world.xyz - ray.origin);

	return ray;
}

[shader("raygeneration")]
void MyRaygenShader()
{
	Ray ray = GenerateCameraRay(DispatchRaysIndex().xy, g_vCamPos.xyz, g_mProjToWorld);

	uint currentRecursionDepth = 0;


	RayDesc ray2;
	ray2.Origin = ray.origin;
	ray2.Direction = ray.direction;
	ray2.TMin = 0.001;
	ray2.TMax = 10000.0;
	RayPayload payload = { float4(0, 0, 0, 0),ray.direction,0 };
	TraceRay(Scene, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, ~0, 0, 2, 0, ray2, payload);
	g_renderTarget[DispatchRaysIndex().xy] = payload.color;
}

[shader("closesthit")]
void ClosestHitShaderColor(inout RayPayload payload, in MyAttributes attr)
{
	payload.depth++;
	PSSkinnedIn triVert[3];
	triVert[0] = Vertices[PrimitiveIndex() * 3];
	triVert[1] = Vertices[PrimitiveIndex() * 3 + 1];
	triVert[2] = Vertices[PrimitiveIndex() * 3 + 2];
	float3 triNorm = normalize(cross(triVert[0].Pos - triVert[1].Pos, triVert[1].Pos - triVert[2].Pos));
	float2 uv = triVert[0].Tex * (1 - attr.barycentrics.x - attr.barycentrics.y) +
		triVert[1].Tex * (attr.barycentrics.x) +
		triVert[2].Tex * (attr.barycentrics.y);
	float3 pos = (triVert[0].Pos * (1 - attr.barycentrics.x - attr.barycentrics.y) +
		triVert[1].Pos * (attr.barycentrics.x) +
		triVert[2].Pos * (attr.barycentrics.y)).xyz;
	float3 normal = triVert[0].Norm * (1 - attr.barycentrics.x - attr.barycentrics.y) +
		triVert[1].Norm * (attr.barycentrics.x) +
		triVert[2].Norm * (attr.barycentrics.y);

	float4 diffuseColor = diffuseMap.SampleLevel(s0, uv, 0) * _DiffuseColor;
	float3 diffMulA = diffuseColor.rgb * diffuseColor.a;
	float3 viewDir = payload.direction;
	float3 specCol = clamp(_SpecularColor.rgb, 0.0001, 1);

	normal = normalize(normal);
	uint randomState = DispatchRaysIndex().x + DispatchRaysIndex().y * 8192 + g_camera_randomValue;
	float3 AOFactor = 1.0f;
	if (payload.depth == 1 && g_enableAO != 0)
	{
		static const float c_AOMaxDist = 32;
		int aoSampleCount = pow(2, g_quality) * 8;
		for (int i = 0; i < aoSampleCount; i++)
		{
			RayDesc ray2;
			float startPosOffsetN = 0;
			for (int j = 0; j < 3; j++)
			{
				startPosOffsetN = max(dot(triVert[j].Pos - pos, normal), startPosOffsetN);
			}
			ray2.Origin = pos + normal * startPosOffsetN;
			ray2.Direction = normalize(float3(RNG::Random01(randomState) * 2 - 1, RNG::Random01(randomState) * 2 - 1, RNG::Random01(randomState) * 2 - 1));
			if (dot(ray2.Direction, normal) < 0)
			{
				ray2.Direction = -ray2.Direction;
			}
			ray2.TMin = 0.001;
			ray2.TMax = c_AOMaxDist;
			TestRayPayload payload2 = { false,float3(0,0,0),float4(0,0,0,0) };
			TraceRay(Scene, RAY_FLAG_NONE, ~0, c_testRayIndex, 2, c_testRayIndex, ray2, payload2);
			if (!payload2.miss)
			{
				AOFactor -= (c_AOMaxDist - distance(payload2.hitPos, pos)) * (1 - payload2.color * 0.3) / c_AOMaxDist / aoSampleCount;
			}
		}
	}
	if (g_enableShadow != 0)
	{
		[loop]
		for (int i = 0; i < 8; i++)
		{
			if (_LightInfo[i].LightColor.r > 0 || _LightInfo[i].LightColor.g > 0 || _LightInfo[i].LightColor.b > 0)
			{
				if (_LightInfo[i].LightType == 0)
				{

					RayDesc ray2;
					ray2.Origin = pos;
					ray2.Direction = _LightInfo[i].dir;
					ray2.TMin = 0.0001;
					ray2.TMax = 10000.0;
					TestRayPayload payload2 = { false, float3(0,0,0), float4(0,0,0,0) };
					float3 lightStrength = max(_LightInfo[i].LightColor.rgb * _LightInfo[i].LightColor.a, 0);
					if (payload.depth < 3 && dot(lightStrength, lightStrength)>1e-6)
						TraceRay(Scene, RAY_FLAG_NONE, ~0, c_testRayIndex, 2, c_testRayIndex, ray2, payload2);
					float inShadow = 1.0f;
					if (!payload2.miss)
						inShadow = 0;

					UnityLight unitylight;
					unitylight.color = lightStrength * inShadow;
					unitylight.dir = _LightInfo[i].dir;
					UnityIndirect unityindirect;
					unityindirect.diffuse = lightStrength * 0.001f * AOFactor;
					unityindirect.specular = lightStrength * 0.001f * AOFactor;
					payload.color += float4(BRDF1_Unity_PBS(diffMulA, specCol, _Metallic, 1 - _Roughness, normal, viewDir, unitylight, unityindirect).xyz, 0);
				}
				else if (_LightInfo[i].LightType == 1)
				{
					RayDesc ray2;
					ray2.Origin = pos;
					ray2.Direction = normalize(_LightInfo[i].dir - pos);
					ray2.TMin = 0.0001;
					ray2.TMax = distance(_LightInfo[i].dir, pos);
					TestRayPayload payload2 = { false,float3(0,0,0),float4(0,0,0,0) };
					float3 lightStrength = _LightInfo[i].LightColor.rgb * _LightInfo[i].LightColor.a / pow(distance(_LightInfo[i].dir, pos), 2);
					if (payload.depth < 3 && dot(lightStrength, lightStrength)>1e-6)
						TraceRay(Scene, RAY_FLAG_NONE, ~0, c_testRayIndex, 2, c_testRayIndex, ray2, payload2);
					float inShadow = 1.0f;
					if (!payload2.miss)
						inShadow = 0.0f;

					float3 lightDir = normalize(_LightInfo[i].dir - pos);
					UnityLight light;
					light.color = lightStrength * inShadow;
					light.dir = lightDir;
					UnityIndirect indirect;
					indirect.diffuse = 0;
					indirect.specular = 0;
					payload.color += float4(BRDF1_Unity_PBS(diffMulA, specCol, _Metallic, 1 - _Roughness, normal, viewDir, light, indirect).rgb, 0);
				}
			}
		}
	}
	else
	{
		[loop]
		for (int i = 0; i < 8; i++)
		{
			if (_LightInfo[i].LightColor.r > 0 || _LightInfo[i].LightColor.g > 0 || _LightInfo[i].LightColor.b > 0)
			{
				if (_LightInfo[i].LightType == 0)
				{
					float inShadow = 1.0f;

					float3 lightStrength = max(_LightInfo[i].LightColor.rgb * _LightInfo[i].LightColor.a, 0);
					UnityLight unitylight;
					unitylight.color = lightStrength * inShadow;
					unitylight.dir = _LightInfo[i].dir;
					UnityIndirect unityindirect;
					unityindirect.diffuse = lightStrength * 0.001f * AOFactor;
					unityindirect.specular = lightStrength * 0.001f * AOFactor;
					payload.color += float4(BRDF1_Unity_PBS(diffMulA, specCol, _Metallic, 1 - _Roughness, normal, viewDir, unitylight, unityindirect).xyz, 0);
				}
				else if (_LightInfo[i].LightType == 1)
				{
					float inShadow = 1.0f;

					float3 lightDir = normalize(_LightInfo[i].dir - pos);
					float3 lightStrength = _LightInfo[i].LightColor.rgb * _LightInfo[i].LightColor.a / pow(distance(_LightInfo[i].dir, pos), 2);
					UnityLight light;
					light.color = lightStrength * inShadow;
					light.dir = lightDir;
					UnityIndirect indirect;
					indirect.diffuse = 0;
					indirect.specular = 0;
					payload.color += float4(BRDF1_Unity_PBS(diffMulA, specCol, _Metallic, 1 - _Roughness, normal, viewDir, light, indirect).rgb, 0);
				}
			}
		}
	}
	UnityLight lightx;
	lightx.color = float4(0, 0, 0, 1);
	lightx.dir = float3(0, 1, 0);
	UnityIndirect indirect1;
	indirect1.diffuse = IrradianceCube.SampleLevel(s0, normal, 0).rgb * g_skyBoxMultiple * AOFactor;


	if (payload.depth < 3)
	{
		RayPayload payloadX;
		payloadX.direction = reflect(viewDir, normal);
		payloadX.color = float4(0, 0, 0, 0);
		payloadX.depth = payload.depth;
		RayDesc rayX;
		rayX.Origin = pos;
		rayX.Direction = payloadX.direction;
		rayX.TMin = 1e-6f;
		rayX.TMax = 10000.0;
		TraceRay(Scene, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, ~0, 0, 2, 0, rayX, payloadX);
		indirect1.specular = payloadX.color.rgb;
	}
	else
	{
		indirect1.specular = EnvCube.SampleLevel(s0, reflect(-viewDir, normal), 0).rgb * g_skyBoxMultiple;
	}
	float3 ambientColor = BRDF1_Unity_PBS(diffMulA, specCol, _Metallic, 1 - _Roughness, normal, viewDir, lightx, indirect1).rgb;

	payload.color = float4(payload.color.rgb + (1 - payload.color.a) * ambientColor, payload.color.a + diffuseColor.a - payload.color.a * diffuseColor.a);

	RayDesc rayNext;
	rayNext.Origin = pos;
	rayNext.Direction = payload.direction;
	rayNext.TMin = 1e-6f;
	rayNext.TMax = 10000.0;
	if (payload.depth < 3 && payload.color.a < 1 - 1e-4f)
		TraceRay(Scene, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, ~0, 0, 2, 0, rayNext, payload);
}

[shader("miss")]
void MissShaderColor(inout RayPayload payload)
{
	payload.color += EnvCube.SampleLevel(s0, payload.direction, payload.depth) * g_skyBoxMultiple;
}

[shader("closesthit")]
void ClosestHitShaderTest(inout TestRayPayload payload, in MyAttributes attr)
{
	payload.miss = false;
	float3 pos = (Vertices[PrimitiveIndex() * 3].Pos * (1 - attr.barycentrics.x - attr.barycentrics.y) +
		Vertices[PrimitiveIndex() * 3 + 1].Pos * (attr.barycentrics.x) +
		Vertices[PrimitiveIndex() * 3 + 2].Pos * (attr.barycentrics.y)).xyz;
	float2 uv = (Vertices[PrimitiveIndex() * 3].Tex * (1 - attr.barycentrics.x - attr.barycentrics.y) +
		Vertices[PrimitiveIndex() * 3 + 1].Tex * (attr.barycentrics.x) +
		Vertices[PrimitiveIndex() * 3 + 2].Tex * (attr.barycentrics.y));

	payload.hitPos = pos;
	payload.color = diffuseMap.SampleLevel(s0, uv, 0) * _DiffuseColor;
}

[shader("miss")]
void MissShaderTest(inout TestRayPayload payload)
{
	payload.miss = true;
}
#endif // RAYTRACING_HLSL