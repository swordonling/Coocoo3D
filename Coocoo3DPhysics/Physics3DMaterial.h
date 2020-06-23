#pragma once
#include "PxPhysicsAPI.h"
namespace Coocoo3DPhysics
{
	public ref class Physics3DMaterial sealed
	{
	internal:
		physx::PxMaterial*				m_Material = NULL;
	};
}
