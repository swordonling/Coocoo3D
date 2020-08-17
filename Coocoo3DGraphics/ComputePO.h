#pragma once
#include "DeviceResources.h"
#include "GraphicsObjectStatus.h"
#include "GraphicsSignature.h"
namespace Coocoo3DGraphics
{
	public ref class ComputePO sealed
	{
	public:
		property GraphicsObjectStatus Status;
		property Platform::Object^ LoadTask;
		property Platform::String^ Path;
		void CompileReload(DeviceResources^ deviceResources, GraphicsSignature^ rootSignature, const Platform::Array<byte>^ sourcesCode);
		void Reload(DeviceResources^ deviceResources,GraphicsSignature^ rootSignature, const Platform::Array<byte>^ data);
	internal:
		Microsoft::WRL::ComPtr<ID3D12PipelineState> m_pipelineState;
		Microsoft::WRL::ComPtr<ID3DBlob> byteCode;
	};
}
