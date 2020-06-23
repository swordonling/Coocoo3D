#include "pch.h"
#include "Physics3DRigidBody.h"
using namespace physx;
using namespace Windows::Foundation::Numerics;
using namespace Coocoo3DPhysics;

float3 Physics3DRigidBody::GetPos()
{
	PxTransform transformM = m_rigidBody->getGlobalPose();
	auto vec1 = float3();
	vec1.x = transformM.p.x;
	vec1.y = transformM.p.y;
	vec1.z = transformM.p.z;
	return vec1;
}

quaternion Physics3DRigidBody::GetRot()
{
	PxTransform transformM = m_rigidBody->getGlobalPose();
	auto rot1 = quaternion();
	rot1.x = transformM.q.x;
	rot1.y = transformM.q.y;
	rot1.z = transformM.q.z;
	rot1.w = transformM.q.w;
	return rot1;
}
