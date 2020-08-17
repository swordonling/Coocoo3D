#pragma once
#include "ITexture.h"
#include "WICFactory.h"
#include "GraphicsObjectStatus.h"
namespace Coocoo3DGraphics
{
	public ref class Texture2D sealed :public ITexture
	{
	public:
		property GraphicsObjectStatus Status;
		property Platform::Object^ LoadTask;
		property Platform::String^ Path;
		property Platform::Array<byte>^ m_textureData;
		property UINT m_width;
		property UINT m_height;

		//在上传GPU之前是无法使用的。使用GraphicsContext::UploadTexture(Texture2D^ texture)上传。
		void ReloadPure(int width, int height,Windows::Foundation::Numerics::float4 color);
		//在上传GPU之前是无法使用的。使用GraphicsContext::UploadTexture(Texture2D^ texture)上传。
		void ReloadFromImage1(WICFactory^ wicFactory, const Platform::Array<byte>^ data);
		void Reload(Texture2D^ texture);
		void Unload();

		virtual void ReleaseUploadHeapResource();

		virtual ~Texture2D();
	internal:

		Microsoft::WRL::ComPtr<ID3D12Resource>				m_texture;
		Microsoft::WRL::ComPtr<ID3D12Resource>				m_textureUpload;
		UINT m_heapRefIndex;
		DXGI_FORMAT m_format;
		UINT m_bindFlags;
	};
}
