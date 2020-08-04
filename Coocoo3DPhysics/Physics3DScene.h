#pragma once
#include "PxPhysicsAPI.h"
#include "Physics3D.h"
#include "Physics3DRigidBody.h"
#include "Physics3DJoint.h"
#include "UnionStructDefine.h"
namespace Coocoo3DPhysics
{
	using namespace Windows::Foundation::Numerics;
	public ref class Physics3DScene sealed
	{
	public:
		static Physics3DScene^ Load(Physics3D^ physics3D);
		void Reload(Physics3D^ physics3D);
		void AddRigidBody(Physics3DRigidBody^ rigidBody, float3 position, quaternion rotation, float3 scale, float mass, float restitution, float friction, float movDampling, float rotDampling, UINT8 shape, UINT8 type, UINT8 collisionGroup, UINT collisionMask);
		void AddJoint(Physics3DJoint^ joint, float3 position, quaternion rotation, Physics3DRigidBody^ r1, Physics3DRigidBody^ r2,
			float3 PositionMinimum,
			float3 PositionMaximum,
			float3 RotationMinimum,
			float3 RotationMaximum,
			float3 PositionSpring,
			float3 RotationSpring);
		void RemoveRigidBody(Physics3DRigidBody^ rigidBody);
		void RemoveJoint(Physics3DJoint^ joint);
		void SetGravitation(float3 gravitation);
		void MoveRigidBody(Physics3DRigidBody^ rigidBody,float3 position, quaternion rotation);
		float3 GetRigidBodyPosition(Physics3DRigidBody^ rigidBody);
		quaternion GetRigidBodyRotation(Physics3DRigidBody^ rigidBody);
		void Simulate(double time);
		void FetchResults();
	internal:
		std::shared_ptr<IPhysicsAPI> m_sdkRef;
		byte m_sceneData[MAX_UNION_SCENE_STRUCTURE_SIZE] = {};
		//Physics3D^ m_physics3D;
		//static physx::PxFilterFlags FilterShader1(
		//	physx::PxFilterObjectAttributes attributes0,
		//	physx::PxFilterData filterData0,
		//	physx::PxFilterObjectAttributes attributes1,
		//	physx::PxFilterData filterData1,
		//	physx::PxPairFlags& pairFlags,
		//	const void* constantBlock,
		//	physx::PxU32 constantBlockSize);
		//physx::PxScene* m_scene = nullptr;
		//physx::PxMaterial* m_material = nullptr;
	};
}
