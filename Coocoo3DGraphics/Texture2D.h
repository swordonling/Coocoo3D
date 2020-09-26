#pragma once
#include "ITexture.h"
#include "WICFactory.h"
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

		//���ϴ�GPU֮ǰ���޷�ʹ�õġ�ʹ��GraphicsContext::UploadTexture(Texture2D^ texture)�ϴ���
		void ReloadPure(int width, int height,Windows::Foundation::Numerics::float4 color);
		//���ϴ�GPU֮ǰ���޷�ʹ�õġ�ʹ��GraphicsContext::UploadTexture(Texture2D^ texture)�ϴ���
		void ReloadFromImage1(WICFactory^ wicFactory, const Platform::Array<byte>^ data);
		void ReloadFromImage(WICFactory^ wicFactory, IBuffer^ file1);
		void Reload(Texture2D^ texture);
		void Unload();

		virtual void ReleaseUploadHeapResource();

		virtual ~Texture2D();
	internal:

		Microsoft::WRL::ComPtr<ID3D12Resource>				m_texture;
		Microsoft::WRL::ComPtr<ID3D12Resource>				m_textureUpload;
		std::vector<ImageMipsData> m_imageMipsData;
		UINT m_heapRefIndex;
		UINT m_mipLevels;
		DXGI_FORMAT m_format;
	};
}
