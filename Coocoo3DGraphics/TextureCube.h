#pragma once
#include "WICFactory.h"
#include "ITexture.h"
namespace Coocoo3DGraphics
{
	public ref class TextureCube sealed : public ITexture
	{
	public:
		property Platform::Array<byte>^ m_textureData;
		property UINT m_width;
		property UINT m_height;
		//在上传GPU之前是无法使用的。使用GraphicsContext::UploadTexture(Texture2DArray^ texture)上传。
		void ReloadPure(int width, int height, const Platform::Array< Windows::Foundation::Numerics::float4>^ color);
		//在上传GPU之前是无法使用的。使用GraphicsContext::UploadTexture(Texture2DArray^ texture)上传。
		void ReloadFromImage1(WICFactory^ wicFactory, int width, int height, const Platform::Array<byte>^ data1, const Platform::Array<byte>^ data2, const Platform::Array<byte>^ data3, const Platform::Array<byte>^ data4, const Platform::Array<byte>^ data5, const Platform::Array<byte>^ data6);

		virtual void ReleaseUploadHeapResource();
	internal:
		Microsoft::WRL::ComPtr<ID3D12Resource>				m_texture;
		Microsoft::WRL::ComPtr<ID3D12Resource>				m_textureUpload;
		UINT m_heapRefIndex;
		DXGI_FORMAT m_format;
		UINT m_bindFlags;
	};
}
