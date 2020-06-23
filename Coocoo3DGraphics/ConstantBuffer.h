#pragma once
#include "DeviceResources.h"
namespace Coocoo3DGraphics
{
	public ref class ConstantBuffer sealed
	{
	public:
		static ConstantBuffer^ Load(DeviceResources^ deviceResources, int size);
		void Reload(DeviceResources^ deviceResources, int size);
		property int Size;
	internal:
		void Initialize(DeviceResources^ deviceResources, int length, void* initData);
		Microsoft::WRL::ComPtr<ID3D11Buffer> m_buffer;
	};
}