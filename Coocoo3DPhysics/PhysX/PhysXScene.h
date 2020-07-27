#pragma once
#include "PxPhysicsAPI.h"
namespace Coocoo3DNative
{
	class PhysXScene
	{
	public:
		physx::PxScene* m_scene = nullptr;
		physx::PxMaterial* m_material = nullptr;
	};
}

