#include "pch.h"
#include "DirectXHelper.h"
#include "ComputePO.h"
using namespace Coocoo3DGraphics;

bool Coocoo3DGraphics::ComputePO::CompileReload1(DeviceResources^ deviceResources, GraphicsSignature^ rootSignature, const Platform::Array<byte>^ sourceCode, Platform::String^ entryPoint, ShaderMacro macro)
{
	const D3D_SHADER_MACRO* macros = nullptr;
	if (macro == ShaderMacro::DEFINE_COO_SURFACE)macros = MACROS_DEFINE_COO_SURFACE;
	else if (macro == ShaderMacro::DEFINE_COO_PARTICLE)macros = MACROS_DEFINE_COO_PARTICLE;

	const wchar_t* wstr1 = entryPoint->Begin();
	UINT length1 = wcslen(wstr1);
	UINT strlen1 = WideCharToMultiByte(CP_ACP, 0, wstr1, length1, NULL, 0, NULL, NULL);
	char* entryPointStr = (char*)malloc(strlen1 + 1);
	WideCharToMultiByte(CP_ACP, 0, wstr1, length1, entryPointStr, strlen1, NULL, NULL);
	entryPointStr[strlen1] = 0;
	HRESULT hr = D3DCompile(
		sourceCode->begin(),
		sourceCode->Length,
		nullptr,
		macros,
		D3D_COMPILE_STANDARD_FILE_INCLUDE,
		entryPointStr,
		"cs_5_0",
		0,
		0,
		&byteCode,
		nullptr
	);
	free(entryPointStr);
	if (FAILED(hr))
		return false;

	D3D12_COMPUTE_PIPELINE_STATE_DESC desc = {};
	desc.CS.pShaderBytecode = byteCode->GetBufferPointer();
	desc.CS.BytecodeLength = byteCode->GetBufferSize();
	desc.pRootSignature = rootSignature->m_rootSignature.Get();
	if (FAILED(deviceResources->GetD3DDevice()->CreateComputePipelineState(&desc, IID_PPV_ARGS(&m_pipelineState))))return false;
	return true;
}

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
			"cs_5_0",
			0,
			0,
			&byteCode,
			nullptr
		)
	);
	D3D12_COMPUTE_PIPELINE_STATE_DESC desc = {};
	desc.CS.pShaderBytecode = byteCode->GetBufferPointer();
	desc.CS.BytecodeLength = byteCode->GetBufferSize();
	desc.pRootSignature = rootSignature->m_rootSignature.Get();
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
