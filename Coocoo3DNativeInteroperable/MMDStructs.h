#pragma once
#include "MMDEnum.h"
namespace Coocoo3DNativeInteroperable
{
	using namespace Windows::Foundation::Numerics;
	public value struct NMMD_INT4
	{
		int _0;
		int _1;
		int _2;
		int _3;
	};
	public value struct NMMD_JointDesc
	{
		byte Type;
		int AssociatedRigidBodyIndex1;
		int AssociatedRigidBodyIndex2;
		float3 Position;
		float3 Rotation;
		float3 PositionMinimum;
		float3 PositionMaximum;
		float3 RotationMinimum;
		float3 RotationMaximum;
		float3 PositionSpring;
		float3 RotationSpring;
	};
	public value struct NMMD_RigidBodyDesc
	{
		int AssociatedBoneIndex;
		byte CollisionGroup;
		USHORT CollisionMask;
		NMMDE_RigidBodyShape Shape;
		float3 Dimemsions;
		float3 Position;
		quaternion Rotation;
		float Mass;
		float TranslateDamp;
		float RotateDamp;
		float Restitution;
		float Friction;
		NMMDE_RigidBodyType Type;
	};
	public value struct NMMD_VertexDesc
	{
		float3 Position;
		float3 Normal;
		float2 UV;
		float EdgeScale;
		NMMD_INT4 BoneId;
		float4 Weight;
		float3 Tangent;
	};
	public value struct NMMD_MorphBoneDesc
	{
		int BoneIndex;
		float3 Translation;
		quaternion Rotation;
	};
	public value struct NMMD_MorphVertexDesc
	{
		int VertexIndex;
		float3 Offset;
	};
}
