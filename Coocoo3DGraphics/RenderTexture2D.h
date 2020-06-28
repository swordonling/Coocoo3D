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

		Microsoft::WRL::ComPtr<ID3D11Texture2D> m_texture2D;
		Microsoft::WRL::ComPtr<ID3D11ShaderResourceView> m_shaderResourceView;
		Microsoft::WRL::ComPtr<ID3D11SamplerState> m_samplerState;
		Microsoft::WRL::ComPtr<ID3D11DepthStencilView> m_depthStencilView;
		UINT m_width;
		UINT m_height;
		DXGI_FORMAT m_format;
		UINT m_bindFlags;

	};
}
