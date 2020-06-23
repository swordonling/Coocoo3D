#pragma once
#include "PxPhysicsAPI.h"
namespace Coocoo3DPhysics
{
	public ref class Physics3DRigidBody sealed
	{
	public:
		Windows::Foundation::Numerics::float3 GetPos();
		Windows::Foundation::Numerics::quaternion GetRot();
	internal:
		physx::PxRigidDynamic* m_rigidBody;
	};
}
