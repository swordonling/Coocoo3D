#pragma once
#include "DeviceResources.h"
namespace Coocoo3DGraphics
{
	public ref class Texture2D sealed
	{
	public:
		property bool Ready;
		property Platform::Object^ LoadTask;
		property Platform::String^ Path;

		//在上传GPU之前是无法使用的。使用GraphicsContext::UploadTexture(Texture2D^ texture)上传。
		void ReloadPure(DeviceResources^ deviceResources, int width, int height,Windows::Foundation::Numerics::float4 color);
		void Reload(Texture2D^ texture);

		//在上传GPU之前是无法使用的。使用GraphicsContext::UploadTexture(Texture2D^ texture)上传。
		void ReloadFromImage1(DeviceResources^ deviceResources, const Platform::Array<byte>^ data);

		property Platform::Array<byte>^ m_textureData;

		virtual ~Texture2D();
	internal:
		Microsoft::WRL::ComPtr<ID3D11Texture2D> m_texture2D;
		Microsoft::WRL::ComPtr<ID3D11ShaderResourceView> m_shaderResourceView;
		Microsoft::WRL::ComPtr<ID3D11SamplerState> m_samplerState;
		UINT m_width;
		UINT m_height;
		DXGI_FORMAT m_format;
		UINT m_bindFlags;
	};
}
