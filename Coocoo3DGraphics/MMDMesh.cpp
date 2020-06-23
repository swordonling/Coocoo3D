#include "pch.h"
#include "MMDMesh.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;
using namespace DirectX;

MMDMesh ^ MMDMesh::Load(DeviceResources ^ deviceResources, const Platform::Array<byte>^ verticeData, const Platform::Array<byte>^ indexData, int vertexStride, int indexStride)
{
	MMDMesh^ mmdMesh = ref new MMDMesh();
	mmdMesh->Reload(deviceResources, verticeData, indexData, vertexStride, indexStride);
	return mmdMesh;
}

void MMDMesh::Reload(DeviceResources ^ deviceResources, const Platform::Array<byte>^ verticeData, const Platform::Array<byte>^ indexData, int vertexStride, int indexStride)
{
	Reload1(deviceResources, verticeData, nullptr, indexData, vertexStride, 0, indexStride, PrimitiveTopology::_TRIANGLELIST);
}

MMDMesh^ MMDMesh::Load(DeviceResources ^ deviceResources, const Platform::Array<byte>^ verticeData, const Platform::Array<byte>^ indexData, int vertexStride, int indexStride, PrimitiveTopology pt)
{
	MMDMesh^ mmdMesh = ref new MMDMesh();
	mmdMesh->Reload(deviceResources, verticeData, indexData, vertexStride, indexStride, pt);
	return mmdMesh;
}

void MMDMesh::Reload(DeviceResources ^ deviceResources, const Platform::Array<byte>^ verticeData, const Platform::Array<byte>^ indexData, int vertexStride, int indexStride, PrimitiveTopology pt)
{
	Reload1(deviceResources, verticeData, nullptr, indexData, vertexStride, 0, indexStride, pt);
}

MMDMesh^ MMDMesh::Load1(DeviceResources ^ deviceResources, const Platform::Array<byte>^ verticeData, const Platform::Array<byte>^ verticeData2, const Platform::Array<byte>^ indexData, int vertexStride, int vertexStride2, int indexStride, PrimitiveTopology pt)
{
	MMDMesh^ mmdMesh = ref new MMDMesh();
	mmdMesh->Reload1(deviceResources, verticeData, verticeData2, indexData, vertexStride, vertexStride2, indexStride, pt);
	return mmdMesh;
}

void MMDMesh::Reload1(DeviceResources ^ deviceResources, const Platform::Array<byte>^ verticeData, const Platform::Array<byte>^ verticeData2, const Platform::Array<byte>^ indexData, int vertexStride, int vertexStride2, int indexStride, PrimitiveTopology pt)
{
	m_vertexStride = vertexStride;
	m_vertexStride2 = vertexStride2;
	m_indexStride = indexStride;
	m_vertexCount = verticeData->Length / vertexStride;
	m_indexCount = indexData->Length / indexStride;
	m_primitiveTopology = (D3D11_PRIMITIVE_TOPOLOGY)pt;

	D3D11_SUBRESOURCE_DATA vertexBufferData = { 0 };
	vertexBufferData.pSysMem = verticeData->begin();
	vertexBufferData.SysMemPitch = 0;
	vertexBufferData.SysMemSlicePitch = 0;
	CD3D11_BUFFER_DESC vertexBufferDesc(verticeData->Length, D3D11_BIND_VERTEX_BUFFER);

	DX::ThrowIfFailed(
		deviceResources->GetD3DDevice()->CreateBuffer(
			&vertexBufferDesc,
			&vertexBufferData,
			&m_vertexBuffer
		)
	);
	if (verticeData2 != nullptr) {
		D3D11_SUBRESOURCE_DATA vertexBufferData2 = { 0 };
		vertexBufferData2.pSysMem = verticeData2->begin();
		vertexBufferData2.SysMemPitch = 0;
		vertexBufferData2.SysMemSlicePitch = 0;
		CD3D11_BUFFER_DESC vertexBufferDesc1(verticeData2->Length, D3D11_BIND_VERTEX_BUFFER);

		DX::ThrowIfFailed(
			deviceResources->GetD3DDevice()->CreateBuffer(
				&vertexBufferDesc1,
				&vertexBufferData2,
				&m_vertexBuffer2
			)
		);
	}

	D3D11_SUBRESOURCE_DATA indexBufferData = { 0 };
	indexBufferData.pSysMem = indexData->begin();
	indexBufferData.SysMemPitch = 0;
	indexBufferData.SysMemSlicePitch = 0;
	CD3D11_BUFFER_DESC indexBufferDesc(indexData->Length, D3D11_BIND_INDEX_BUFFER);

	DX::ThrowIfFailed(
		deviceResources->GetD3DDevice()->CreateBuffer(
			&indexBufferDesc,
			&indexBufferData,
			&m_indexBuffer
		)
	);
}

MMDMesh::~MMDMesh()
{
}
