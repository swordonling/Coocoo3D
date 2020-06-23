#include "pch.h"
#include "Physics3DScene.h"
using namespace physx;
using namespace Coocoo3DPhysics;

Physics3DScene^ Physics3DScene::Load(Physics3D^ physics3D)
{
	Physics3DScene^ physics3DScene = ref new Physics3DScene();
	physics3DScene->Reload(physics3D);
	return physics3DScene;
}

void Physics3DScene::Reload(Physics3D ^ physics3D)
{
	PxSceneDesc sceneDesc(physics3D->m_physics->getTolerancesScale());
	sceneDesc.gravity = PxVec3(0.0f, -9.81f, 0.0f);
	sceneDesc.cpuDispatcher = physics3D->m_dispatcher;
	sceneDesc.filterShader = PxDefaultSimulationFilterShader;
	m_scene = physics3D->m_physics->createScene(sceneDesc);

	PxPvdSceneClient* pvdClient = m_scene->getScenePvdClient();
	if (pvdClient)
	{
		pvdClient->setScenePvdFlag(PxPvdSceneFlag::eTRANSMIT_CONSTRAINTS, true);
		pvdClient->setScenePvdFlag(PxPvdSceneFlag::eTRANSMIT_CONTACTS, true);
		pvdClient->setScenePvdFlag(PxPvdSceneFlag::eTRANSMIT_SCENEQUERIES, true);
	}
	PxRigidStatic* groundPlane = PxCreatePlane(*physics3D->m_physics, PxPlane(0, 1, 0, 0), *physics3D->gMaterial);
	m_scene->addActor(*groundPlane);
}

void Physics3DScene::StepPhysics(float time)
{
	m_scene->simulate(time);
	m_scene->fetchResults(true);
}

void Physics3DScene::AddRigidBody(Physics3DRigidBody^ rigidBody)
{
	m_scene->addActor(*rigidBody->m_rigidBody);
}
