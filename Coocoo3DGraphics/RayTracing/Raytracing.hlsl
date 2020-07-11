//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
//*********************************************************
// Copyright (c) sselecirPyM. All rights reserved.
// BSD-3 License
//*********************************************************

#ifndef RAYTRACING_HLSL
#define RAYTRACING_HLSL

#include "../Shaders/CameraDataDefine.hlsli"

struct Ray
{
	float3 origin;
	float3 direction;
};
RaytracingAccelerationStructure Scene : register(t0, space0);
RWTexture2D<float4> g_renderTarget : register(u0);
cbuffer cb0 : register(b0)
{
	CAMERA_DATA_DEFINE//is a macro
};

typedef BuiltInTriangleIntersectionAttributes MyAttributes;
struct RayPayload
{
	float4 color;
};

// Generate a ray in world space for a camera pixel corresponding to an index from the dispatched 2D grid.
inline Ray GenerateCameraRay(uint2 index, in float3 cameraPosition, in float4x4 projectionToWorld)
{
	float2 xy = index + 0.5f; // center in the middle of the pixel.
	float2 screenPos = xy / DispatchRaysDimensions().xy * 2.0 - 1.0;

	// Invert Y for DirectX-style coordinates.
	screenPos.y = -screenPos.y;

	// Unproject the pixel coordinate into a world positon.
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

	// Generate a ray for a camera pixel corresponding to an index from the dispatched 2D grid.
	Ray ray = GenerateCameraRay(DispatchRaysIndex().xy, g_vCamPos.xyz, g_mProjToWorld);

	// Cast a ray into the scene and retrieve a shaded color.
	uint currentRecursionDepth = 0;


	RayDesc ray2;
	ray2.Origin = ray.origin;
	ray2.Direction = ray.direction;
	ray2.TMin = 0.001;
	ray2.TMax = 10000.0;
	RayPayload payload = { float4(1, 0, 1, 1) };
	TraceRay(Scene, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, ~0, 0, 1, 0, ray2, payload);
	g_renderTarget[DispatchRaysIndex().xy] =  payload.color;
}

[shader("closesthit")]
void MyClosestHitShader(inout RayPayload payload, in MyAttributes attr)
{
	float3 barycentrics = float3(1 - attr.barycentrics.x - attr.barycentrics.y, attr.barycentrics.x, attr.barycentrics.y);
	payload.color = float4(barycentrics, 1);
}

[shader("miss")]
void MyMissShader(inout RayPayload payload)
{
	payload.color = float4(0, 0, 0, 1);
}

#endif // RAYTRACING_HLSL