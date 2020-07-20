#ifndef RAYTRACING_HLSL
#define RAYTRACING_HLSL

#include "../Shaders/CameraDataDefine.hlsli"
#include "../Shaders/RandomNumberGenerator.hlsli"

static const int c_testRayIndex = 1;
struct PSSkinnedIn
{
	float4 Pos;
	float3 Norm;
	float2 Tex;
	float3 Tangent;
	float EdgeScale;
	float3 preserved1;
};

struct Ray
{
	float3 origin;
	float3 direction;
};

struct LightInfo
{
	float3 dir;
	uint LightType;
	float4 LightColor;
};

RaytracingAccelerationStructure Scene : register(t0, space0);
StructuredBuffer<PSSkinnedIn> Vertices : register(t1, space1);
RWTexture2D<float4> g_renderTarget : register(u0);
cbuffer cb0 : register(b0)
{
	CAMERA_DATA_DEFINE//is a macro
};

Texture2D diffuseMap :register(t2, space1);
SamplerState s0 : register(s0);

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
	float _Smoothness;
	float _Emission;
	float4 preserved1[8];
	LightInfo _LightInfo[8];
};

typedef BuiltInTriangleIntersectionAttributes MyAttributes;
struct RayPayload
{
	float4 color;
	float3 direction;
	uint depth;
};

struct TestRayPayload
{
	bool miss;
	float3 hitPos;
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

	normal = normalize(normal);
	uint randomState = DispatchRaysIndex().x + DispatchRaysIndex().y * 8192 + g_camera_randomValue;
	float AOFactor = 1.0f;
	static const int c_AOITTime = 64;
	static const float c_AOMaxDist = 10;
	if (payload.depth == 1)
		for (int i = 0; i < c_AOITTime; i++)
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
			TestRayPayload payload2 = { false,float3(0,0,0) };
			TraceRay(Scene, RAY_FLAG_NONE, ~0, c_testRayIndex, 2, c_testRayIndex, ray2, payload2);
			if (!payload2.miss)
			{
				AOFactor -= (c_AOMaxDist - distance(payload2.hitPos, pos)) / c_AOMaxDist / c_AOITTime;
			}
		}
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
				TestRayPayload payload2 = { false,float3(0,0,0) };
				if (payload.depth < 3)
					TraceRay(Scene, RAY_FLAG_NONE, ~0, c_testRayIndex, 2, c_testRayIndex, ray2, payload2);
				if (payload2.miss)
					payload.color += float4(_LightInfo[i].LightColor.rgb * diffMulA * saturate(dot(_LightInfo[i].dir, normal)) / 6.28318530718f, 0);
				payload.color += float4(_LightInfo[i].LightColor.rgb * diffMulA * 0.02f * AOFactor, 0);
			}
			else if (_LightInfo[i].LightType == 1)
			{
				RayDesc ray2;
				ray2.Origin = pos;
				ray2.Direction = normalize(_LightInfo[i].dir - pos);
				ray2.TMin = 0.0001;
				ray2.TMax = distance(_LightInfo[i].dir, pos);
				TestRayPayload payload2 = { false,float3(0,0,0) };
				if (payload.depth < 3)
					TraceRay(Scene, RAY_FLAG_NONE, ~0, c_testRayIndex, 2, c_testRayIndex, ray2, payload2);
				if (payload2.miss)
					payload.color += float4(_LightInfo[i].LightColor.rgb * diffMulA * saturate(dot(_LightInfo[i].dir, normal)) / (6.28318530718f * pow(distance(_LightInfo[i].dir, pos), 2)), 0);
			}
		}
	}
	payload.color = float4(payload.color.rgb + (1 - payload.color.a) * diffMulA * _AmbientColor * AOFactor, payload.color.a + diffuseColor.a - payload.color.a * diffuseColor.a);



	RayDesc ray2;
	ray2.Origin = pos;
	ray2.Direction = payload.direction;
	ray2.TMin = 1e-6f;
	ray2.TMax = 10000.0;
	if (payload.depth < 3 && payload.color.a < 1 - 1e-4f)
		TraceRay(Scene, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, ~0, 0, 2, 0, ray2, payload);
}

[shader("miss")]
void MissShaderColor(inout RayPayload payload)
{
	payload.color += float4(0, 0, 0, 1);
}

[shader("closesthit")]
void ClosestHitShaderTest(inout TestRayPayload payload, in MyAttributes attr)
{
	payload.miss = false;
	float3 pos = (Vertices[PrimitiveIndex() * 3].Pos * (1 - attr.barycentrics.x - attr.barycentrics.y) +
		Vertices[PrimitiveIndex() * 3 + 1].Pos * (attr.barycentrics.x) +
		Vertices[PrimitiveIndex() * 3 + 2].Pos * (attr.barycentrics.y)).xyz;
	payload.hitPos = pos;
}

[shader("miss")]
void MissShaderTest(inout TestRayPayload payload)
{
	payload.miss = true;
}
#endif // RAYTRACING_HLSL