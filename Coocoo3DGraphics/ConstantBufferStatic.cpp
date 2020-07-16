#include "pch.h"
#include "ConstantBufferStatic.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;
using namespace Microsoft::WRL;

ConstantBufferStatic^ ConstantBufferStatic::Load(DeviceResources^ deviceResources, int size)
{
	ConstantBufferStatic^ constBuffer = ref new ConstantBufferStatic();
	constBuffer->Initialize(deviceResources, size);
	return constBuffer;
}

void ConstantBufferStatic::Reload(DeviceResources^ deviceResources, int size)
{
	Initialize(deviceResources, size);
}

void ConstantBufferStatic::Unload()
{
	m_constantBufferUpload.Reset();
	lastUpdateIndex = 0;
}

void ConstantBufferStatic::Initialize(DeviceResources^ deviceResources, int size)
{
	Size = (size + 255) & ~255;

	auto d3dDevice = deviceResources->GetD3DDevice();

	for (int i = 0; i < c_frameCount; i++)
	{
		DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
			&CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_DEFAULT),
			D3D12_HEAP_FLAG_NONE,
			&CD3DX12_RESOURCE_DESC::Buffer(Size),
			D3D12_RESOURCE_STATE_GENERIC_READ,
			nullptr,
			IID_PPV_ARGS(&m_constantBuffers[i])));
		NAME_D3D12_OBJECT(m_constantBuffers[i]);
	}

	DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
		&CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_UPLOAD),
		D3D12_HEAP_FLAG_NONE,
		&CD3DX12_RESOURCE_DESC::Buffer(/*c_frameCount **/ Size),
		D3D12_RESOURCE_STATE_GENERIC_READ,
		nullptr,
		IID_PPV_ARGS(&m_constantBufferUpload)));
	NAME_D3D12_OBJECT(m_constantBufferUpload);


	DX::ThrowIfFailed(m_constantBufferUpload->Map(0, &CD3DX12_RANGE(0, 0), reinterpret_cast<void**>(&m_mappedConstantBuffer)));
	ZeroMemory(m_mappedConstantBuffer, /*c_frameCount **/ Size);
}

D3D12_GPU_VIRTUAL_ADDRESS ConstantBufferStatic::GetCurrentVirtualAddress()
{
	return m_constantBuffers[lastUpdateIndex]->GetGPUVirtualAddress();
}
