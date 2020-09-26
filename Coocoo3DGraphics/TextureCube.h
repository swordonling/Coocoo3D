#pragma once
#include "WICFactory.h"
#include "ITexture.h"
namespace Coocoo3DGraphics
{
	using namespace Windows::Storage;
	using namespace ::Windows::Storage::Streams;
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
		void ReloadFromImage(WICFactory^ wicFactory, int width, int height, IBuffer^ file1, IBuffer^ file2, IBuffer^ file3, IBuffer^ file4, IBuffer^ file5, IBuffer^ file6);

		virtual void ReleaseUploadHeapResource();
	internal:
		Microsoft::WRL::ComPtr<ID3D12Resource>				m_texture;
		Microsoft::WRL::ComPtr<ID3D12Resource>				m_textureUpload;
		std::vector<ImageMipsData> m_imageMipsData;
		UINT m_heapRefIndex;
		DXGI_FORMAT m_format;
		UINT m_mipLevels;
	};
}
