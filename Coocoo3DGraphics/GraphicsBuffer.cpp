#include "pch.h"
#include "GraphicsBuffer.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;

GraphicsBuffer ^ GraphicsBuffer::Load(DeviceResources ^ deviceResources, int count, int stride)
{
	GraphicsBuffer^ graphicsBuffer = ref new GraphicsBuffer();
	graphicsBuffer->Reload(deviceResources, count, stride);
	return graphicsBuffer;
}

void GraphicsBuffer::Reload(DeviceResources ^ deviceResources, int count, int stride)
{
	auto device = deviceResources->GetD3DDevice();
	D3D11_BUFFER_DESC bufferDesc = {};
	bufferDesc.ByteWidth = count * stride;
	bufferDesc.BindFlags = D3D11_BIND_UNORDERED_ACCESS | D3D11_BIND_SHADER_RESOURCE;
	bufferDesc.MiscFlags = D3D11_RESOURCE_MISC_BUFFER_STRUCTURED;
	bufferDesc.StructureByteStride = stride;

	DX::ThrowIfFailed(device->CreateBuffer(&bufferDesc, nullptr, &m_buffer));

	D3D11_SHADER_RESOURCE_VIEW_DESC shaderResourceViewDesc =
		CD3D11_SHADER_RESOURCE_VIEW_DESC(m_buffer.Get(), DXGI_FORMAT_UNKNOWN, 0, count);
	DX::ThrowIfFailed(device->CreateShaderResourceView(
		m_buffer.Get(), &shaderResourceViewDesc, &m_shaderResourceView
	));

	D3D11_UNORDERED_ACCESS_VIEW_DESC uavDesc
		= CD3D11_UNORDERED_ACCESS_VIEW_DESC(m_buffer.Get(), DXGI_FORMAT_UNKNOWN, 0, count);
	DX::ThrowIfFailed(device->CreateUnorderedAccessView(m_buffer.Get(), &uavDesc, &m_unorderedAccessView));
}
