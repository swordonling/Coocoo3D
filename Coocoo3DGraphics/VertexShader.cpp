#include "pch.h"
#include "VertexShader.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;

VertexShader^ VertexShader::CompileLoad(DeviceResources ^ deviceResources, const Platform::Array<byte>^ sourceCode)
{
	VertexShader^ vertexShader = ref new VertexShader();
	vertexShader->CompileReload(deviceResources, sourceCode);
	return vertexShader;
}

void VertexShader::CompileReload(DeviceResources ^ deviceResources, const Platform::Array<byte>^ sourceCode)
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

VertexShader ^ VertexShader::Load(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	VertexShader^ vertexShader = ref new VertexShader();
	vertexShader->Reload(deviceResources, data);
	return vertexShader;
}

void VertexShader::Reload(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	D3DCreateBlob(data->Length, &byteCode);
	memcpy(byteCode->GetBufferPointer(), data->begin(), data->Length);
}

VertexShader::~VertexShader()
{
}
