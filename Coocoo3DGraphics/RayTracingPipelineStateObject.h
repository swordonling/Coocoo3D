#pragma once
#include "DeviceResources.h"
#include "GraphicsSignature.h"
namespace Coocoo3DGraphics
{
	public ref class RayTracingPipelineStateObject sealed
	{
	public:
		void Reload(DeviceResources^ deviceResources, GraphicsSignature^ rayTracingSignature, const Platform::Array<byte>^ data);
		void ReloadTablesForModels(DeviceResources^ deviceResources, int modelCount);
	internal:
		Microsoft::WRL::ComPtr<ID3D12StateObject> m_dxrStateObject;
		int lastUpdateIndex = 0;
		Microsoft::WRL::ComPtr<ID3D12Resource> m_missShaderTable[c_frameCount];
		Microsoft::WRL::ComPtr<ID3D12Resource> m_hitGroupShaderTable[c_frameCount];
		Microsoft::WRL::ComPtr<ID3D12Resource> m_rayGenShaderTable[c_frameCount];
	};
}

