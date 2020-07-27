#include "pch.h"
#include "PhysXAPI.h"
#include "PhysX/PhysX.h"
using namespace Coocoo3DPhysics;

void PhysXAPI::SetAPIUsed(Physics3D^ physics3D)
{
	physics3D->m_sdkRef = std::make_shared<PhysX>();
}
