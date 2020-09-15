#include "pch.h"
#include "MMDMesh.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;
using namespace DirectX;

MMDMesh^ MMDMesh::Load1(const Platform::Array<byte>^ verticeData, const Platform::Array<byte>^ verticeData2, const Platform::Array<byte>^ indexData, int vertexStride, int vertexStride2, int indexStride, PrimitiveTopology pt)
{
	MMDMesh^ mmdMesh = ref new MMDMesh();
	mmdMesh->Reload1(verticeData, verticeData2, indexData, vertexStride, vertexStride2, indexStride, pt);
	return mmdMesh;
}

void MMDMesh::Reload1(const Platform::Array<byte>^ verticeData, const Platform::Array<byte>^ verticeData2, const Platform::Array<byte>^ indexData, int vertexStride, int vertexStride2, int indexStride, PrimitiveTopology pt)
{
	m_vertexStride = vertexStride;
	m_vertexStride2 = vertexStride2;
	m_indexStride = indexStride;
	m_vertexCount = verticeData->Length / vertexStride;
	m_indexCount = indexData->Length / indexStride;
	m_primitiveTopology = (D3D_PRIMITIVE_TOPOLOGY)pt;
	m_verticeData = verticeData;
	m_verticeDataPos = verticeData2;
	m_indexData = indexData;
}

struct OnlyPosition
{
	DirectX::XMFLOAT4 pos;
};
void MMDMesh::ReloadNDCQuad()
{
	OnlyPosition positions[]
	{
		XMFLOAT4(-1,-1,0,1),
		XMFLOAT4(-1, 1,0,1),
		XMFLOAT4( 1,-1,0,1),
		XMFLOAT4( 1, 1,0,1),
	};
	unsigned int indices[] =
	{
		0, 1, 2,
		2, 1, 3,
	};
	m_vertexStride = sizeof(OnlyPosition);
	m_vertexStride2 = 0;
	m_indexStride = 4;
	m_vertexCount = _countof(positions);
	m_indexCount = _countof(indices);
	m_primitiveTopology = D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST;
	m_verticeData = ref new Platform::Array<byte, 1>(sizeof(positions));
	m_verticeDataPos = nullptr;
	m_indexData = ref new Platform::Array<byte, 1>(sizeof(indices));

	memcpy(m_verticeData->begin(), positions, sizeof(positions));
	memcpy(m_indexData->begin(), indices, sizeof(indices));
}

void MMDMesh::ReleaseUploadHeapResource()
{
	m_vertexBufferUpload.Reset();
	m_indexBufferUpload.Reset();
}

void MMDMesh::CopyPosData(Platform::WriteOnlyArray<Windows::Foundation::Numerics::float3>^ Target)
{
	memcpy(Target->begin(), m_verticeDataPos->begin(), m_verticeDataPos->Length);
}

MMDMesh::~MMDMesh()
{
}
