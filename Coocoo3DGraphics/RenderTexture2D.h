#pragma once
#include "DeviceResources.h"
namespace Coocoo3DGraphics
{
	public ref class RenderTexture2D sealed
	{
	public:
		void ReloadAsDepthStencil(DeviceResources^ deviceResources, int width, int height);
		void ReloadAsRenderTarget(DeviceResources^ deviceResources, int width, int height);
		void ReloadAsRenderTarget(DeviceResources^ deviceResources, int width, int height, DxgiFormat format);
	internal:

		Microsoft::WRL::ComPtr<ID3D12Resource>				m_texture;
		UINT m_heapRefIndex;
		UINT m_dsvHeapRefIndex;
		UINT m_rtvHeapRefIndex;
		UINT m_width;
		UINT m_height;
		DXGI_FORMAT m_format;
		DXGI_FORMAT m_dsvFormat;
		DXGI_FORMAT m_rtvFormat;
		D3D12_RESOURCE_FLAGS m_resourceFlags;
		D3D12_RESOURCE_STATES prevResourceState;

	};
}
