#pragma once
#include "DeviceResources.h"

namespace Coocoo3DGraphics
{
	public ref class GraphicsCommandList sealed
	{
	public:
		static GraphicsCommandList^ Load(DeviceResources^ deviceResources);
		void Reload(DeviceResources^ deviceResources);
	internal:
		Microsoft::WRL::ComPtr<ID3D11DeviceContext3> m_deferredContext;
	};
}
