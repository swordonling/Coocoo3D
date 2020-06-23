#include "pch.h"
#include "ComputeShader.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;

ComputeShader ^ ComputeShader::CompileLoad(DeviceResources ^ deviceResources, const Platform::Array<byte>^ sourceCode)
{
	ComputeShader^ computeShader = ref new ComputeShader();
	computeShader->CompileReload(deviceResources, sourceCode);
	return computeShader;
}

void ComputeShader::CompileReload(DeviceResources ^ deviceResources, const Platform::Array<byte>^ sourceCode)
{
	Microsoft::WRL::ComPtr<ID3DBlob> data;
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
			data.GetAddressOf(),
			nullptr
		)
	);
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->
		CreateComputeShader(data->GetBufferPointer(), data->GetBufferSize(), nullptr, &m_computeShader));
}

ComputeShader ^ ComputeShader::Load(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	ComputeShader^ computeShader = ref new ComputeShader();
	computeShader->Reload(deviceResources, data);
	return computeShader;
}

void ComputeShader::Reload(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->
		CreateComputeShader(data->begin(), data->Length, nullptr, &m_computeShader));
}
