#pragma once
#include "DeviceResources.h"
namespace Coocoo3DGraphics {
	public ref class GraphicsSignature sealed
	{
	public:
		void ReloadMMD(DeviceResources^ deviceResources);
	internal:
		Microsoft::WRL::ComPtr<ID3D12RootSignature> m_rootSignatures[10];
	};
}
