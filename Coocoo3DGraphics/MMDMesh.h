#pragma once
#include "DeviceResources.h"
namespace Coocoo3DGraphics
{
	public enum struct PrimitiveTopology
	{
		_UNDEFINED = 0,
		_POINTLIST = 1,
		_LINELIST = 2,
		_LINESTRIP = 3,
		_TRIANGLELIST = 4,
		_TRIANGLESTRIP = 5,
		_LINELIST_ADJ = 10,
		_LINESTRIP_ADJ = 11,
		_TRIANGLELIST_ADJ = 12,
		_TRIANGLESTRIP_ADJ = 13,
		_1_CONTROL_POINT_PATCHLIST = 33,
		_2_CONTROL_POINT_PATCHLIST = 34,
		_3_CONTROL_POINT_PATCHLIST = 35,
		_4_CONTROL_POINT_PATCHLIST = 36,
		_5_CONTROL_POINT_PATCHLIST = 37,
		_6_CONTROL_POINT_PATCHLIST = 38,
		_7_CONTROL_POINT_PATCHLIST = 39,
		_8_CONTROL_POINT_PATCHLIST = 40,
		_9_CONTROL_POINT_PATCHLIST = 41,
		_10_CONTROL_POINT_PATCHLIST = 42,
		_11_CONTROL_POINT_PATCHLIST = 43,
		_12_CONTROL_POINT_PATCHLIST = 44,
		_13_CONTROL_POINT_PATCHLIST = 45,
		_14_CONTROL_POINT_PATCHLIST = 46,
		_15_CONTROL_POINT_PATCHLIST = 47,
		_16_CONTROL_POINT_PATCHLIST = 48,
		_17_CONTROL_POINT_PATCHLIST = 49,
		_18_CONTROL_POINT_PATCHLIST = 50,
		_19_CONTROL_POINT_PATCHLIST = 51,
		_20_CONTROL_POINT_PATCHLIST = 52,
		_21_CONTROL_POINT_PATCHLIST = 53,
		_22_CONTROL_POINT_PATCHLIST = 54,
		_23_CONTROL_POINT_PATCHLIST = 55,
		_24_CONTROL_POINT_PATCHLIST = 56,
		_25_CONTROL_POINT_PATCHLIST = 57,
		_26_CONTROL_POINT_PATCHLIST = 58,
		_27_CONTROL_POINT_PATCHLIST = 59,
		_28_CONTROL_POINT_PATCHLIST = 60,
		_29_CONTROL_POINT_PATCHLIST = 61,
		_30_CONTROL_POINT_PATCHLIST = 62,
		_31_CONTROL_POINT_PATCHLIST = 63,
		_32_CONTROL_POINT_PATCHLIST = 64,
	};
	public ref class MMDMesh sealed
	{
	public:
		static MMDMesh^ Load1(const Platform::Array<byte>^ verticeData, const Platform::Array<byte>^ verticeData2, const Platform::Array<byte>^ indexData, int vertexStride, int vertexStride2, int indexStride, PrimitiveTopology pt);
		//���ϴ�GPU֮ǰ���޷�ʹ�õġ�ʹ��GraphicsContext::void UploadMesh(MMDMesh^ mesh)�ϴ���
		void Reload1(const Platform::Array<byte>^ verticeData, const Platform::Array<byte>^ verticeData2, const Platform::Array<byte>^ indexData, int vertexStride, int vertexStride2, int indexStride, PrimitiveTopology pt);
		void ReloadNDCQuad();
		void ReleaseUploadHeapResource();
		virtual ~MMDMesh();

		property Platform::Array<byte>^ m_verticeData;
		property Platform::Array<byte>^ m_verticeData2;
		property Platform::Array<byte>^ m_indexData;
		property int m_indexCount;
		property int m_vertexCount;
	internal:
		UINT m_vertexStride;
		UINT m_indexStride;
		UINT m_vertexStride2;
		UINT m_vertexOffset = 0;
		D3D_PRIMITIVE_TOPOLOGY m_primitiveTopology = D3D_PRIMITIVE_TOPOLOGY_UNDEFINED;

		Microsoft::WRL::ComPtr<ID3D12Resource>				m_vertexBuffer;
		D3D12_VERTEX_BUFFER_VIEW m_vertexBufferView;
		Microsoft::WRL::ComPtr<ID3D12Resource>				m_vertexBuffer2[c_frameCount];
		D3D12_VERTEX_BUFFER_VIEW m_vertexBufferView2[c_frameCount];
		Microsoft::WRL::ComPtr<ID3D12Resource>				m_indexBuffer;
		D3D12_INDEX_BUFFER_VIEW m_indexBufferView;

		Microsoft::WRL::ComPtr<ID3D12Resource>				m_skinnedVertice;
		D3D12_VERTEX_BUFFER_VIEW m_skinnedVerticeVertexBufferView;
		D3D12_STREAM_OUTPUT_BUFFER_VIEW m_skinnedVerticeStreamOutputBufferView;
		D3D12_RESOURCE_STATES prevStateSkinnedVertice;

		Microsoft::WRL::ComPtr<ID3D12Resource> m_vertexBufferUpload;
		Microsoft::WRL::ComPtr<ID3D12Resource> m_vertexBufferUpload2[c_frameCount];
		Microsoft::WRL::ComPtr<ID3D12Resource> m_indexBufferUpload;
		int lastUpdateIndex = 0;

		static const UINT c_skinnedVerticeStride = 64u;
	};
}

