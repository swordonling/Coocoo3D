#pragma once

namespace Coocoo3DNativeInteroperable
{
	using namespace Windows::Foundation::Numerics;
	public enum struct NMMDE_BoneFlag
	{
		ChildUseId = 0x1,
		Rotatable = 0x2,
		Movable = 0x4,
		Visible = 0x8,
		Controllable = 0x10,
		HasIK = 0x20,
		AcquireRotate = 0x100,
		AcquireTranslate = 0x200,
		RotAxisFixed = 0x400,
		UseLocalAxis = 0x800,
		PostPhysics = 0x1000,
		ReceiveTransform = 0x2000,
	};
	public enum struct NMMDE_DrawFlag
	{
		DrawDoubleFace = 0x1,
		DrawGroundShadow = 0x2,
		CastSelfShadow = 0x4,
		DrawSelfShadow = 0x8,
		DrawEdge = 0x10,
	};
	public enum struct NMMDE_MorphCategory
	{
		System = 0,
		Eyebrow = 1,
		Eye = 2,
		Mouth = 3,
		Other = 4,
	};
	public enum struct NMMDE_MorphType
	{
		Group = 0,
		Vertex = 1,
		Bone = 2,
		UV = 3,
		ExtUV1 = 4,
		ExtUV2 = 5,
		ExtUV3 = 6,
		ExtUV4 = 7,
		Material = 8,
	};
	public enum struct NMMDE_MorphMaterialMethon
	{
		Mul = 0,
		Add = 1,
	};
	public enum struct NMMDE_RigidBodyShape
	{
		Sphere = 0,
		Box = 1,
		Capsule = 2,
	};
	public enum struct NMMDE_RigidBodyType
	{
		Kinematic = 0,
		Physics = 1,
		PhysicsStrict = 2,
		PhysicsGhost = 3,
	};
}