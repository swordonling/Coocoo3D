#pragma once
#include "PxPhysicsAPI.h"
#include "UnionStructDefine.h"
namespace Coocoo3DPhysics
{
	using namespace Windows::Foundation::Numerics;
	public ref class Physics3DRigidBody sealed
	{
	public:
	internal:
		byte m_rigidBodyData[MAX_UNION_RIGID_BODY_STRUCTURE_SIZE];
		//physx::PxRigidDynamic* m_rigidBody = nullptr;
		//physx::PxMaterial* m_material = nullptr;

		//float3 m_position;
		//quaternion m_rotation;
		//float m_mass;
		//UINT8 m_shape;
		//UINT8 m_collisionGroup;
		//UINT16 m_collisionMask;
		//float3 m_scale;
		//float m_movDampling;
		//float m_rotDampling;
		//float m_restitution;
		//float m_friction;
		//UINT m_type;
	};
}
