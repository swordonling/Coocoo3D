#include "pch.h"
#include "PixelShader.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;

bool PixelShader::CompileReload1(const Platform::Array<byte>^ sourceCode, Platform::String^ entryPoint, ShaderMacro macro)
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
		"ps_5_0",
		0,
		0,
		&byteCode,
		nullptr
	);
	free(entryPointStr);
	if (FAILED(hr))
		return false;
	else
		return true;
}

PixelShader^ PixelShader::Load(const Platform::Array<byte>^ data)
{
	PixelShader^ pixelShader = ref new PixelShader();
	pixelShader->Reload(data);
	return pixelShader;
}

void PixelShader::Reload(const Platform::Array<byte>^ data)
{
	DX::ThrowIfFailed(D3DCreateBlob(data->Length, &byteCode));
	memcpy(byteCode->GetBufferPointer(), data->begin(), data->Length);
}

PixelShader::~PixelShader()
{
}
