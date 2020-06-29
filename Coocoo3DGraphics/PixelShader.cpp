#include "pch.h"
#include "PixelShader.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;
PixelShader ^ PixelShader::CompileLoad(DeviceResources ^ deviceResources, const Platform::Array<byte>^ sourceCode)
{
	PixelShader^ pixelShader = ref new PixelShader();
	pixelShader->CompileReload(deviceResources, sourceCode);
	return pixelShader;
}
void PixelShader::CompileReload(DeviceResources ^ deviceResources, const Platform::Array<byte>^ sourceCode)
{
	DX::ThrowIfFailed(
		D3DCompile(
			sourceCode->begin(),
			sourceCode->Length,
			nullptr,
			nullptr,
			D3D_COMPILE_STANDARD_FILE_INCLUDE,
			"main",
			"ps_5_0",
			0,
			0,
			&byteCode,
			nullptr
		)
	);
}

PixelShader ^ PixelShader::Load(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	PixelShader^ pixelShader = ref new PixelShader();
	pixelShader->Reload(deviceResources, data);
	return pixelShader;
}

void PixelShader::Reload(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	D3DCreateBlob(data->Length, &byteCode);
	memcpy(byteCode->GetBufferPointer(), data->begin(), data->Length);
}

PixelShader::~PixelShader()
{
}
