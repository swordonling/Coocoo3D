#pragma once
#include "PxPhysicsAPI.h"
#include "Physics3D.h"
#include "Physics3DRigidBody.h"
namespace Coocoo3DPhysics
{
	public ref class Physics3DScene sealed
	{
	public:
		static Physics3DScene^ Load(Physics3D^ physics3D);
		void Reload(Physics3D^ physics3D);
		void StepPhysics(float time);
		void AddRigidBody(Physics3DRigidBody^ rigidBody);
	internal:
		physx::PxScene*				m_scene = NULL;
	};
}
