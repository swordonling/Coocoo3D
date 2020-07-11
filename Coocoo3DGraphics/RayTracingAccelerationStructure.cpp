#include "pch.h"
#include "RayTracingAccelerationStructure.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;

void RayTracingAccelerationStructure::Reload(DeviceResources^ deviceResources, UINT size, UINT instanceCount)
{
	lastUpdateIndex = 0;
	auto device = deviceResources->GetD3DDevice();
	CD3DX12_HEAP_PROPERTIES defaultHeapProperties(D3D12_HEAP_TYPE_DEFAULT);
	CD3DX12_HEAP_PROPERTIES uploadHeapProperties(D3D12_HEAP_TYPE_UPLOAD);
	for (int i = 0; i < c_frameCount; i++)
	{
		DX::ThrowIfFailed(device->CreateCommittedResource(
			&defaultHeapProperties,
			D3D12_HEAP_FLAG_NONE,
			&CD3DX12_RESOURCE_DESC::Buffer(size, D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS),
			D3D12_RESOURCE_STATE_UNORDERED_ACCESS,
			nullptr,
			IID_PPV_ARGS(&m_scratchResource[i])));
		NAME_D3D12_OBJECT(m_scratchResource[i]);

		DX::ThrowIfFailed(device->CreateCommittedResource(
			&uploadHeapProperties,
			D3D12_HEAP_FLAG_NONE,
			&CD3DX12_RESOURCE_DESC::Buffer(sizeof(D3D12_RAYTRACING_INSTANCE_DESC) * instanceCount),
			D3D12_RESOURCE_STATE_GENERIC_READ,
			nullptr,
			IID_PPV_ARGS(&instanceDescs[i])));
		NAME_D3D12_OBJECT(instanceDescs[i]);

	}
}

void RayTracingAccelerationStructure::AddMeshToThisFrameRayTracingList(MMDMesh^ mesh)
{
	m_rayTracingMeshes.push_back(mesh);
}
