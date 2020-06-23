#include "pch.h"
#include "PxPhysicsAPI.h"
#include "Physics3D.h"

using namespace physx;
using namespace Coocoo3DPhysics;

void Physics3D::Init()
{
	bool interactive = true;

	m_foundation = PxCreateFoundation(PX_PHYSICS_VERSION, gAllocator, gErrorCallback);
	m_physics = PxCreatePhysics(PX_PHYSICS_VERSION, *m_foundation, PxTolerancesScale(), true);
	m_dispatcher = PxDefaultCpuDispatcherCreate(2);

	gMaterial = m_physics->createMaterial(0.5f, 0.5f, 0.6f);

	m_loaded = true;
}

Physics3DRigidBody ^ Physics3D::CreateRigidBody(Physics3DMaterial^ physicsMaterial)
{
	PxVec3 pos(0, 0, 0);
	PxTransform localTm(pos);
	Physics3DRigidBody^ rigidBody = ref new Physics3DRigidBody();
	rigidBody->m_rigidBody = PxCreateDynamic(*m_physics, localTm, PxBoxGeometry(1.0f, 1.0f, 1.0f), *physicsMaterial->m_Material, 1.0f);
	return rigidBody;
}

void Physics3D::release()
{
	if (!m_loaded)return;
	m_physics->release();
	m_foundation->release();
}

Physics3D::~Physics3D()
{
	if (!m_loaded)return;
	m_physics->release();
	m_foundation->release();
}
