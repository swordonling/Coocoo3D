#pragma once
#include "DeviceResources.h"
namespace Coocoo3DGraphics
{
	public ref class DynamicMesh sealed
	{
	public:
		void Reload(int size, int stride);
		void Initilize(DeviceResources^ deviceResources);
	internal:
		Microsoft::WRL::ComPtr<ID3D12Resource>				m_vertice;
		D3D12_VERTEX_BUFFER_VIEW m_vertexBufferView;
		D3D12_RESOURCE_STATES m_prevState;
		UINT m_stride;
		UINT m_size;
	};
}
