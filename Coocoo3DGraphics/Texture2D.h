#pragma once
#include "ITexture.h"
#include "GraphicsObjectStatus.h"
namespace Coocoo3DGraphics
{
	using namespace Windows::Storage::Streams;
	public ref class Texture2D sealed :public ITexture
	{
	public:
		property GraphicsObjectStatus Status;
		property Platform::Array<byte>^ m_textureData;
		property UINT m_width;
		property UINT m_height;
		property UINT m_mipLevels;

		//在上传GPU之前是无法使用的。使用GraphicsContext::UploadTexture(Texture2D^ texture)上传。
		void ReloadPure(int width, int height,Windows::Foundation::Numerics::float4 color);
		//在上传GPU之前是无法使用的。使用GraphicsContext::UploadTexture(Texture2D^ texture)上传。
		void ReloadFromImage(IBuffer^ file1);
		void ReloadFromImageNoMip(IBuffer^ file1);
		void Reload(Texture2D^ texture);
		void Unload();

		virtual void ReleaseUploadHeapResource();

		virtual ~Texture2D();
	internal:

		Microsoft::WRL::ComPtr<ID3D12Resource>				m_texture;
		Microsoft::WRL::ComPtr<ID3D12Resource>				m_textureUpload;
		std::vector<ImageMipsData> m_imageMipsData;
		UINT m_heapRefIndex;
		DXGI_FORMAT m_format;
	};
}
