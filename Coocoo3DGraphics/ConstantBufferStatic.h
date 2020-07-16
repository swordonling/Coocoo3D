#pragma once
#include "DeviceResources.h"
namespace Coocoo3DGraphics
{
	public ref class ConstantBufferStatic sealed
	{
	public:
		static ConstantBufferStatic^ Load(DeviceResources^ deviceResources, int size);
		void Reload(DeviceResources^ deviceResources, int size);
		void Unload();
		property int Size;
	internal:
		void Initialize(DeviceResources^ deviceResources, int size);
		D3D12_GPU_VIRTUAL_ADDRESS GetCurrentVirtualAddress();
		Microsoft::WRL::ComPtr<ID3D12Resource>				m_constantBufferUpload;
		Microsoft::WRL::ComPtr<ID3D12Resource>				m_constantBuffers[c_frameCount];
		int lastUpdateIndex = 0;
		byte* m_mappedConstantBuffer = nullptr;
	};
}
