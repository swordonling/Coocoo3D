#include "pch.h"
#include "DynamicMesh.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;

void DynamicMesh::Reload(int size, int stride)
{
	m_size = size;
	m_stride = stride;
}

void DynamicMesh::Initilize(DeviceResources^ deviceResources)
{
	auto d3dDevice = deviceResources->GetD3DDevice();
	CD3DX12_HEAP_PROPERTIES defaultHeapProperties(D3D12_HEAP_TYPE_DEFAULT);
	CD3DX12_RESOURCE_DESC vertexBufferDesc = CD3DX12_RESOURCE_DESC::Buffer(m_size, D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS);
	DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
		&defaultHeapProperties,
		D3D12_HEAP_FLAG_NONE,
		&vertexBufferDesc,
		D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER,
		nullptr,
		IID_PPV_ARGS(&m_vertice)));
	NAME_D3D12_OBJECT(m_vertice);

	m_vertexBufferView.BufferLocation = m_vertice->GetGPUVirtualAddress();
	m_vertexBufferView.StrideInBytes = m_stride;
	m_vertexBufferView.SizeInBytes = m_size;
	m_prevState = D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER;
}
