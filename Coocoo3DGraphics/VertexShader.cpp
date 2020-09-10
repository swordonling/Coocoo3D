#include "pch.h"
#include "VertexShader.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;

VertexShader^ VertexShader::CompileLoad(const Platform::Array<byte>^ sourceCode)
{
	VertexShader^ vertexShader = ref new VertexShader();
	vertexShader->CompileReload(sourceCode);
	return vertexShader;
}

void VertexShader::CompileReload(const Platform::Array<byte>^ sourceCode)
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
}

bool VertexShader::CompileReload1(const Platform::Array<byte>^ sourceCode, Platform::String^ entryPoint)
{
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
		nullptr,
		D3D_COMPILE_STANDARD_FILE_INCLUDE,
		entryPointStr,
		"vs_5_0",
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

VertexShader^ VertexShader::Load(const Platform::Array<byte>^ data)
{
	VertexShader^ vertexShader = ref new VertexShader();
	vertexShader->Reload(data);
	//WideCharToMultiByte()
	return vertexShader;
}

void VertexShader::Reload(const Platform::Array<byte>^ data)
{
	DX::ThrowIfFailed(D3DCreateBlob(data->Length, &byteCode));
	memcpy(byteCode->GetBufferPointer(), data->begin(), data->Length);
}

VertexShader::~VertexShader()
{
}
