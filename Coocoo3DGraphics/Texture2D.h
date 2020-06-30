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

		//���ϴ�GPU֮ǰ���޷�ʹ�õġ�ʹ��GraphicsContext::UploadTexture(Texture2D^ texture)�ϴ���
		void ReloadPure(int width, int height,Windows::Foundation::Numerics::float4 color);
		//���ϴ�GPU֮ǰ���޷�ʹ�õġ�ʹ��GraphicsContext::UploadTexture(Texture2D^ texture)�ϴ���
		void ReloadFromImage1(DeviceResources^ deviceResources, const Platform::Array<byte>^ data);
		void Reload(Texture2D^ texture);

		void ReleaseUploadHeapResource();
		property Platform::Array<byte>^ m_textureData;

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
