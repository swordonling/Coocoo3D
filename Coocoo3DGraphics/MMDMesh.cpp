#include "pch.h"
#include "MMDMesh.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;
using namespace DirectX;

MMDMesh^ MMDMesh::Load1( const Platform::Array<byte>^ verticeData, const Platform::Array<byte>^ verticeData2, const Platform::Array<byte>^ indexData, int vertexStride, int vertexStride2, int indexStride, PrimitiveTopology pt)
{
	MMDMesh^ mmdMesh = ref new MMDMesh();
	mmdMesh->Reload1( verticeData, verticeData2, indexData, vertexStride, vertexStride2, indexStride, pt);
	return mmdMesh;
}

void MMDMesh::Reload1( const Platform::Array<byte>^ verticeData, const Platform::Array<byte>^ verticeData2, const Platform::Array<byte>^ indexData, int vertexStride, int vertexStride2, int indexStride, PrimitiveTopology pt)
{
	m_vertexStride = vertexStride;
	m_vertexStride2 = vertexStride2;
	m_indexStride = indexStride;
	m_vertexCount = verticeData->Length / vertexStride;
	m_indexCount = indexData->Length / indexStride;
	m_primitiveTopology = (D3D11_PRIMITIVE_TOPOLOGY)pt;
	m_verticeData = verticeData;
	m_verticeData2 = verticeData2;
	m_indexData = indexData;
}

MMDMesh::~MMDMesh()
{
}
