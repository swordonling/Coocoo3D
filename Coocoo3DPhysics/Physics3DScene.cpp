#include "pch.h"
#include "Physics3DScene.h"
#include "MathUtility.h"
using namespace physx;
using namespace Coocoo3DPhysics;

Physics3DScene^ Physics3DScene::Load(Physics3D^ physics3D)
{
	Physics3DScene^ physics3DScene = ref new Physics3DScene();
	physics3DScene->Reload(physics3D);
	return physics3DScene;
}

void Physics3DScene::Reload(Physics3D^ physics3D)
{
	m_sdkRef = physics3D->m_sdkRef;
	//m_sceneDataRef = physics3D->m_sdkRef->InitScene();
	physics3D->m_sdkRef->InitScene(m_sceneData);
	//auto physics = physics3D->m_physics;
	//PxSceneDesc sceneDesc(physics->getTolerancesScale());
	//sceneDesc.gravity = PxVec3(0.0f, -9.81f, 0.0f);
	//sceneDesc.cpuDispatcher = physics3D->m_dispatcher;
	////sceneDesc.filterShader = PxDefaultSimulationFilterShader;
	//sceneDesc.filterShader = FilterShader1;
	//m_scene = physics->createScene(sceneDesc);

	//PxPvdSceneClient* pvdClient = m_scene->getScenePvdClient();
	//if (pvdClient)
	//{
	//	pvdClient->setScenePvdFlag(PxPvdSceneFlag::eTRANSMIT_CONSTRAINTS, true);
	//	pvdClient->setScenePvdFlag(PxPvdSceneFlag::eTRANSMIT_CONTACTS, true);
	//	pvdClient->setScenePvdFlag(PxPvdSceneFlag::eTRANSMIT_SCENEQUERIES, true);
	//}
	//m_material = physics->createMaterial(0.5f, 0.5f, 0.6f);
	//PxRigidStatic* groundPlane = PxCreatePlane(*physics, PxPlane(0, 1, 0, 0), *m_material);
	//m_scene->addActor(*groundPlane);
	//m_physics3D = physics3D;
}

void Physics3DScene::AddRigidBody(Physics3DRigidBody^ rigidBody, float3 position, quaternion rotation, float3 scale, float mass, float restitution, float friction, float movDampling, float rotDampling, UINT8 shape, UINT8 type, UINT8 collisionGroup, UINT collisionMask)
{
	m_sdkRef->SceneAddRigidBody(m_sceneData, rigidBody->m_rigidBodyData, position, rotation, scale, mass, restitution, friction, movDampling, rotDampling, shape, type, collisionGroup, collisionMask);
	//rigidBody->m_position = position;
	//rigidBody->m_rotation = rotation;
	//rigidBody->m_mass = mass;
	//rigidBody->m_shape = shape;
	//rigidBody->m_collisionGroup = collisionGroup;
	//rigidBody->m_collisionMask = collisionMask;
	//rigidBody->m_scale = scale;
	//rigidBody->m_movDampling = movDampling;
	//rigidBody->m_rotDampling = rotDampling;
	//rigidBody->m_restitution = restitution;
	//rigidBody->m_friction = friction;
	//rigidBody->m_type = type;

	//rigidBody->m_material = m_physics3D->m_physics->createMaterial(rigidBody->m_friction, rigidBody->m_friction, rigidBody->m_restitution);
	//PxTransform transform = Util::GetPxTransform(position, rotation);
	//PxTransform ofs = (PxTransform)PxIdentity;
	//float hx = scale.x;
	//float hy = scale.y;
	//float hz = scale.z;
	//PxGeometry* geometry = nullptr;

	//if (shape == 0)
	//{
	//	geometry = &PxSphereGeometry(hx);
	//}
	//else if (shape == 1)
	//{
	//	geometry = &PxBoxGeometry(hx, hy, hz);
	//}
	//else if (shape == 2)
	//{
	//	geometry = &PxCapsuleGeometry(hx, hy);
	//	ofs.q = PxQuat::PxQuat(-PxPi / 2, PxVec3(0, 0, 1));
	//}
	//PxShape* shape1 = shape1 = m_physics3D->m_physics->createShape(*geometry, *rigidBody->m_material);
	//shape1->setLocalPose(ofs);
	//PxFilterData filterData(1 << (collisionGroup - 1), collisionMask, 0, 0);
	//shape1->setSimulationFilterData(filterData);
	//shape1->setContactOffset(0.2f);
	//if (type == 0)
	//{
	//	//rigidBody->m_rigidBody = PxCreateKinematic(*m_physics3D->m_physics, transform, *geometry, *rigidBody->m_material, mass, ofs);
	//	rigidBody->m_rigidBody = PxCreateKinematic(*m_physics3D->m_physics, transform, *shape1, mass);
	//}
	//else
	//{
	//	//rigidBody->m_rigidBody = PxCreateDynamic(*m_physics3D->m_physics, transform, *geometry, *rigidBody->m_material, mass, ofs);
	//	rigidBody->m_rigidBody = PxCreateDynamic(*m_physics3D->m_physics, transform, *shape1, mass);
	//}
	//rigidBody->m_rigidBody->setAngularDamping(rotDampling);
	//rigidBody->m_rigidBody->setLinearDamping(movDampling);
	////rigidBody->m_rigidBody->setDominanceGroup(collisionGroup);
	//rigidBody->m_rigidBody->setMass(mass);
	//shape1->release();
	//m_scene->addActor(*rigidBody->m_rigidBody);
}

void Physics3DScene::AddJoint(Physics3DJoint^ joint, float3 position, quaternion rotation, Physics3DRigidBody^ r1, Physics3DRigidBody^ r2,
	float3 PositionMinimum,
	float3 PositionMaximum,
	float3 RotationMinimum,
	float3 RotationMaximum,
	float3 PositionSpring,
	float3 RotationSpring)
{
	m_sdkRef->SceneAddJoint(m_sceneData, joint->m_jointData, position, rotation, r1->m_rigidBodyData, r2->m_rigidBodyData, PositionMinimum, PositionMaximum, RotationMinimum, RotationMaximum, PositionSpring, RotationSpring);
	//auto rot = Util::GetPxQuat(rotation);

	//auto pos = Util::GetPxVec3(position);
	//auto pos1 = Util::GetPxVec3(r1->m_position);
	//auto pos2 = Util::GetPxVec3(r2->m_position);
	//auto rot1 = Util::GetPxQuat(r1->m_rotation);
	//auto rot2 = Util::GetPxQuat(r2->m_rotation);
	//PxTransform t1(pos - pos1, (rot1 * rot.getConjugate()).getNormalized());
	//PxTransform t2(pos - pos2, (rot2 * rot.getConjugate()).getNormalized());
	//auto j = PxD6JointCreate(*m_physics3D->m_physics, r1->m_rigidBody, t1, r2->m_rigidBody, t2);
	//j->setMotion(PxD6Axis::eX, PxD6Motion::eLIMITED);
	//j->setMotion(PxD6Axis::eY, PxD6Motion::eLIMITED);
	//j->setMotion(PxD6Axis::eZ, PxD6Motion::eLIMITED);
	//j->setMotion(PxD6Axis::eSWING1, PxD6Motion::eFREE);
	//j->setMotion(PxD6Axis::eSWING2, PxD6Motion::eFREE);
	//j->setMotion(PxD6Axis::eTWIST, PxD6Motion::eFREE);

	//PxJointLinearLimitPair limitPair1(PositionMinimum.x, PositionMaximum.x, PxSpring(PositionSpring.x, 1.0f));
	//PxJointLinearLimitPair limitPair2(PositionMinimum.y, PositionMaximum.y, PxSpring(PositionSpring.y, 1.0f));
	//PxJointLinearLimitPair limitPair3(PositionMinimum.z, PositionMaximum.z, PxSpring(PositionSpring.z, 1.0f));
	//if (PositionSpring.x == 0)
	//j->setMotion(PxD6Axis::eX, PxD6Motion::eLOCKED);
	//if (PositionSpring.y == 0)
	//j->setMotion(PxD6Axis::eY, PxD6Motion::eLOCKED);
	//if (PositionSpring.z == 0)
	//j->setMotion(PxD6Axis::eZ, PxD6Motion::eLOCKED);
	////PxJointAngularLimitPair limitPair4(RotationMinimum.x, RotationMaximum.x, PxSpring(RotationSpring.x, 1.0f));
	////PxJointAngularLimitPair limitPair5(RotationMinimum.y, RotationMaximum.y, PxSpring(RotationSpring.y, 1.0f));
	////PxJointAngularLimitPair limitPair6(RotationMinimum.z, RotationMaximum.z, PxSpring(RotationSpring.z, 1.0f));
	//j->setLinearLimit(PxD6Axis::eX, limitPair1);
	//j->setLinearLimit(PxD6Axis::eY, limitPair2);
	//j->setLinearLimit(PxD6Axis::eZ, limitPair3);
	////j->setDrive(PxD6Drive::eSLERP, PxD6JointDrive(0, 1000, FLT_MAX, true));
	//joint->m_joint = j;
}

void Physics3DScene::RemoveRigidBody(Physics3DRigidBody^ rigidBody)
{
	m_sdkRef->SceneRemoveRigidBody(m_sceneData, rigidBody->m_rigidBodyData);
	////m_scene->removeActor(*rigidBody->m_rigidBody);
	//rigidBody->m_rigidBody->release();
	//rigidBody->m_rigidBody = nullptr;
}

void Physics3DScene::RemoveJoint(Physics3DJoint^ joint)
{
	m_sdkRef->SceneRemoveJoint(m_sceneData, joint->m_jointData);
}

void Physics3DScene::SetGravitation(float3 gravitation)
{
	m_sdkRef->SceneSetGravitation(m_sceneData, gravitation);
	//m_scene->setGravity(Util::GetPxVec3(gravitation));
}

void Physics3DScene::MoveRigidBody(Physics3DRigidBody^ rigidBody, float3 position, quaternion rotation)
{
	m_sdkRef->SceneMoveRigidBody(m_sceneData, rigidBody->m_rigidBodyData,position,rotation);
	//rigidBody->m_rigidBody->setKinematicTarget(Util::GetPxTransform(position, rotation));
}

float3 Physics3DScene::GetRigidBodyPosition(Physics3DRigidBody^ rigidBody)
{
	return m_sdkRef->SceneGetRigidBodyPosition(m_sceneData, rigidBody->m_rigidBodyData);
	//PxTransform transformM = rigidBody->m_rigidBody->getGlobalPose();
	//auto vec1 = float3();
	//vec1.x = transformM.p.x;
	//vec1.y = transformM.p.y;
	//vec1.z = transformM.p.z;
	//return vec1;
}

quaternion Physics3DScene::GetRigidBodyRotation(Physics3DRigidBody^ rigidBody)
{
	return m_sdkRef->SceneGetRigidBodyRotation(m_sceneData, rigidBody->m_rigidBodyData);
	//PxTransform transformM = rigidBody->m_rigidBody->getGlobalPose();
	//auto rot1 = quaternion();
	//rot1.x = transformM.q.x;
	//rot1.y = transformM.q.y;
	//rot1.z = transformM.q.z;
	//rot1.w = transformM.q.w;
	//return rot1;
}

void Physics3DScene::Simulate(double time)
{
	m_sdkRef->SceneSimulate(m_sceneData, time);
	//m_scene->simulate(static_cast<float>(time));
	////m_scene->fetchResults(true);
}

void Physics3DScene::FetchResults()
{
	m_sdkRef->SceneFetchResults(m_sceneData);
	//m_scene->fetchResults(true);
}

//PxFilterFlags Physics3DScene::FilterShader1(PxFilterObjectAttributes attributes0, PxFilterData filterData0, PxFilterObjectAttributes attributes1, PxFilterData filterData1, PxPairFlags& pairFlags, const void* constantBlock, PxU32 constantBlockSize)
//{
//	if ((filterData0.word0 & filterData1.word1) && (filterData0.word1 & filterData1.word0))
//	{
//		pairFlags = PxPairFlag::eCONTACT_DEFAULT;
//		return PxFilterFlags(PxFilterFlag::eDEFAULT);
//	}
//	return PxFilterFlags(PxFilterFlag::eSUPPRESS);
//}
