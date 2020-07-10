#pragma once
#include "DeviceResources.h"
#include "GraphicsSignature.h"
#include "RayTracing/RaytracingHlslCompat.h"
namespace Coocoo3DGraphics
{
	public ref class RayTracingPipelineStateObject sealed
	{
	public:
		void Reload(DeviceResources^ deviceResources, GraphicsSignature^ rayTracingSignature, const Platform::Array<byte>^ data);
	internal:
		Microsoft::WRL::ComPtr<ID3D12StateObject> m_dxrStateObject;
		RayGenConstantBuffer m_rayGenCB;

		Microsoft::WRL::ComPtr<ID3D12Resource> m_missShaderTable;
		Microsoft::WRL::ComPtr<ID3D12Resource> m_hitGroupShaderTable;
		Microsoft::WRL::ComPtr<ID3D12Resource> m_rayGenShaderTable;
	};
}

