#include "pch.h"
#include "ConstantBuffer.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;
using namespace Microsoft::WRL;

ConstantBuffer ^ ConstantBuffer::Load(DeviceResources^ deviceResources, int size)
{
	ConstantBuffer^ constBuffer = ref new ConstantBuffer();
	constBuffer->Initialize(deviceResources, size, nullptr);
	return constBuffer;
}

void ConstantBuffer::Reload(DeviceResources ^ deviceResources, int size)
{
	Initialize(deviceResources, size, nullptr);
}

void ConstantBuffer::Initialize(DeviceResources ^ deviceResources, int size, void * initData)
{
	Size = size;
	D3D11_BUFFER_DESC bufferDesc1 = CD3D11_BUFFER_DESC(size, D3D11_BIND_CONSTANT_BUFFER);
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateBuffer(&bufferDesc1, nullptr, &m_buffer));
}
