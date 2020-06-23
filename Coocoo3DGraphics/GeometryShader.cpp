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
	Microsoft::WRL::ComPtr<ID3DBlob> data;
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
			data.GetAddressOf(),
			nullptr
		)
	);
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->
		CreateGeometryShader(data->GetBufferPointer(), data->GetBufferSize(), nullptr, &m_geometryShader));
}

GeometryShader ^ GeometryShader::CompileLoadSO(DeviceResources ^ deviceResources, const Platform::Array<byte>^ sourceCode)
{
	GeometryShader^ geometryShader = ref new GeometryShader();
	geometryShader->CompileReloadSO(deviceResources, sourceCode);
	return geometryShader;
}

void GeometryShader::CompileReloadSO(DeviceResources ^ deviceResources, const Platform::Array<byte>^ sourceCode)
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
			"gs_5_0",
			0,
			0,
			data.GetAddressOf(),
			nullptr
		)
	);
	D3D11_SO_DECLARATION_ENTRY pDecl[] =
	{
		{0, "SV_POSITION", 0, 0, 4, 0 },
		{0, "POSWORLD", 0, 0, 3, 0 },
		{0, "NORMAL", 0, 0, 3, 0 },
		{0, "TEXCOORD", 0, 0, 2, 0 },
		{0, "TANGENT", 0, 0, 3, 0 },
	};
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->
		CreateGeometryShaderWithStreamOutput(data->GetBufferPointer(), data->GetBufferSize(), pDecl, sizeof(pDecl), nullptr, 0, 0, nullptr, &m_geometryShader));
}

GeometryShader ^ GeometryShader::Load(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	GeometryShader^ geometryShader = ref new GeometryShader();
	geometryShader->Reload(deviceResources, data);
	return geometryShader;
}

void GeometryShader::Reload(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->
		CreateGeometryShader(data->begin(), data->Length, nullptr, &m_geometryShader));
}
