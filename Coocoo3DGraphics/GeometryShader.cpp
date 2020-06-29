#include "pch.h"
#include "GeometryShader.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;

GeometryShader ^ GeometryShader::CompileLoad(DeviceResources ^ deviceResources, const Platform::Array<byte>^ sourceCode)
{
	GeometryShader^ geometryShader = ref new GeometryShader();
	geometryShader->CompileReload(deviceResources, sourceCode);
	return geometryShader;
}

void GeometryShader::CompileReload(DeviceResources ^ deviceResources, const Platform::Array<byte>^ sourceCode)
{
	DX::ThrowIfFailed(
		D3DCompile(
			sourceCode->begin(),
			sourceCode->Length,
			nullptr,
			nullptr,
			D3D_COMPILE_STANDARD_FILE_INCLUDE,
			"main",
			"gs_5_0",
			0,
			0,
			&byteCode,
			nullptr
		)
	);
}

GeometryShader ^ GeometryShader::Load(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	GeometryShader^ geometryShader = ref new GeometryShader();
	geometryShader->Reload(deviceResources, data);
	return geometryShader;
}

void GeometryShader::Reload(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	D3DCreateBlob(data->Length, &byteCode);
	memcpy(byteCode->GetBufferPointer(), data->begin(), data->Length);
}
