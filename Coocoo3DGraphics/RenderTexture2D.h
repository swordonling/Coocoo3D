#pragma once
#include "DeviceResources.h"
namespace Coocoo3DGraphics
{
	public ref class RenderTexture2D sealed
	{
	public:
		void ReloadAsDepthStencil(DeviceResources^ deviceResources, int width, int height);
	internal:

		Microsoft::WRL::ComPtr<ID3D12Resource>				m_texture;
		UINT m_heapRefIndex;
		UINT m_dsvHeapRefIndex;
		UINT m_width;
		UINT m_height;
		DXGI_FORMAT m_format;
		DXGI_FORMAT m_dsvFormat;
		D3D12_RESOURCE_FLAGS m_resourceFlags;

	};
}
