#pragma once
#include "DeviceResources.h"
namespace Coocoo3DGraphics {
	public ref class GraphicsBuffer sealed
	{
	public:
		static GraphicsBuffer^ Load(DeviceResources^ deviceResources, int count, int stride);
		void Reload(DeviceResources^ deviceResources, int count, int stride);
	internal:
		Microsoft::WRL::ComPtr<ID3D11Buffer> m_buffer;
		Microsoft::WRL::ComPtr<ID3D11ShaderResourceView> m_shaderResourceView;
		Microsoft::WRL::ComPtr<ID3D11UnorderedAccessView> m_unorderedAccessView;
	};
}
