#pragma once
#include "DeviceResources.h"
namespace Coocoo3DGraphics
{
	public ref class RenderTexture2D sealed
	{
	public:
		void ReloadAsDepthStencil(DeviceResources^ deviceResources, int width, int height);
	internal:
		void Initialize(DeviceResources ^ deviceResources, int width, int height);

		Microsoft::WRL::ComPtr<ID3D12Resource>				m_texture;
		UINT m_width;
		UINT m_height;
		DXGI_FORMAT m_format;
		UINT m_bindFlags;

	};
}
