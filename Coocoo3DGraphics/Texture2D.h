#pragma once
#include "DeviceResources.h"
#include "GraphicsObjectStatus.h"
namespace Coocoo3DGraphics
{
	public ref class Texture2D sealed
	{
	public:
		property GraphicsObjectStatus Status;
		property Platform::Object^ LoadTask;
		property Platform::String^ Path;
		property Platform::Array<byte>^ m_textureData;

		//在上传GPU之前是无法使用的。使用GraphicsContext::UploadTexture(Texture2D^ texture)上传。
		void ReloadPure(int width, int height,Windows::Foundation::Numerics::float4 color);
		//在上传GPU之前是无法使用的。使用GraphicsContext::UploadTexture(Texture2D^ texture)上传。
		void ReloadFromImage1(DeviceResources^ deviceResources, const Platform::Array<byte>^ data);
		void Reload(Texture2D^ texture);
		void Unload();

		void ReleaseUploadHeapResource();

		virtual ~Texture2D();
	internal:

		//Microsoft::WRL::ComPtr<ID3D12DescriptorHeap>		m_srvHeap;
		Microsoft::WRL::ComPtr<ID3D12Resource>				m_texture;
		Microsoft::WRL::ComPtr<ID3D12Resource>				m_textureUpload;
		UINT m_heapRefIndex;
		UINT m_width;
		UINT m_height;
		DXGI_FORMAT m_format;
		UINT m_bindFlags;
	};
}
