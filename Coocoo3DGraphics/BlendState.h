#pragma once
#include "DeviceResources.h"
namespace Coocoo3DGraphics
{
	public enum struct BlendOP
	{

	};
	public ref class BlendState sealed
	{
	public:
		static BlendState^Load(DeviceResources^deviceResources);
		void Reload(DeviceResources^deviceResources);
	internal:
		Microsoft::WRL::ComPtr<ID3D11BlendState> m_blendState;
	};
}
