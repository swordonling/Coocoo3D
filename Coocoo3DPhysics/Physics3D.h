#pragma once
#include "PxPhysicsAPI.h"
#include "Physics3DRigidBody.h"
#include "Physics3DMaterial.h"
namespace Coocoo3DPhysics
{
	public ref class Physics3D sealed
	{
	public:
		void Init();
		Physics3DRigidBody^ CreateRigidBody(Physics3DMaterial^ physicsMaterial);
		void release();
		virtual ~Physics3D();
	internal:
		bool m_loaded = false;

		physx::PxDefaultAllocator		gAllocator;
		physx::PxDefaultErrorCallback	gErrorCallback;

		physx::PxFoundation*			m_foundation = NULL;
		physx::PxPhysics*				m_physics = NULL;

		physx::PxDefaultCpuDispatcher*	m_dispatcher = NULL;

		physx::PxMaterial*				gMaterial = NULL;

		physx::PxCudaContextManager* gCudaContextManager = NULL;
	};
}
