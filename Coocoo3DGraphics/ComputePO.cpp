#include "pch.h"
#include "DirectXHelper.h"
#include "ComputePO.h"
using namespace Coocoo3DGraphics;

void ComputePO::CompileReload(DeviceResources^ deviceResources, GraphicsSignature^ rootSignature, const Platform::Array<byte>^ sourceCode)
{
	DX::ThrowIfFailed(
		D3DCompile(
			sourceCode->begin(),
			sourceCode->Length,
			nullptr,
			nullptr,
			D3D_COMPILE_STANDARD_FILE_INCLUDE,
			"main",
			"vs_5_0",
			0,
			0,
			&byteCode,
			nullptr
		)
	);
	D3D12_COMPUTE_PIPELINE_STATE_DESC desc = {};
	desc.CS.pShaderBytecode = byteCode->GetBufferPointer();
	desc.CS.BytecodeLength = byteCode->GetBufferSize();
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateComputePipelineState(&desc, IID_PPV_ARGS(&m_pipelineState)));
}

void ComputePO::Reload(DeviceResources^ deviceResources, GraphicsSignature^ rootSignature, const Platform::Array<byte>^ data)
{
	D3DCreateBlob(data->Length, &byteCode);
	memcpy(byteCode->GetBufferPointer(), data->begin(), data->Length);
	D3D12_COMPUTE_PIPELINE_STATE_DESC desc = {};
	desc.CS.pShaderBytecode = byteCode->GetBufferPointer();
	desc.CS.BytecodeLength = byteCode->GetBufferSize();
	desc.pRootSignature = rootSignature->m_rootSignature.Get();
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateComputePipelineState(&desc, IID_PPV_ARGS(&m_pipelineState)));
}
