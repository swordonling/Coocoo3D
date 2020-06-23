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
	Microsoft::WRL::ComPtr<ID3DBlob> data;
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
			data.GetAddressOf(),
			nullptr
		)
	);

	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->
		CreateVertexShader(data->GetBufferPointer(), data->GetBufferSize(), nullptr, &m_vertexShader));
	static const D3D11_INPUT_ELEMENT_DESC vertexDesc[] =
	{
		{ "POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 1, 0, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "NORMAL", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "TEXCOORD", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 12, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "EDGESCALE", 0, DXGI_FORMAT_R32_FLOAT, 0, 20, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "BONES", 0, DXGI_FORMAT_R32G32B32A32_UINT, 0, 24, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "WEIGHTS", 0, DXGI_FORMAT_R32G32B32A32_FLOAT, 0, 40, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "TANGENT", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 56, D3D11_INPUT_PER_VERTEX_DATA, 0 },
	};
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->
		CreateInputLayout(vertexDesc, ARRAYSIZE(vertexDesc), data->GetBufferPointer(), data->GetBufferSize(), &m_inputLayout));
}

VertexShader ^ VertexShader::Load(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	VertexShader^ vertexShader = ref new VertexShader();
	vertexShader->Reload(deviceResources, data);
	return vertexShader;
}

void VertexShader::Reload(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->
		CreateVertexShader(data->begin(), data->Length, nullptr, &m_vertexShader));
	static const D3D11_INPUT_ELEMENT_DESC vertexDesc[] =
	{
		{ "POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 1, 0, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "NORMAL", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "TEXCOORD", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 12, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "EDGESCALE", 0, DXGI_FORMAT_R32_FLOAT, 0, 20, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "BONES", 0, DXGI_FORMAT_R32G32B32A32_UINT, 0, 24, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "WEIGHTS", 0, DXGI_FORMAT_R32G32B32A32_FLOAT, 0, 40, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "TANGENT", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 56, D3D11_INPUT_PER_VERTEX_DATA, 0 },
	};
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->
		CreateInputLayout(vertexDesc, ARRAYSIZE(vertexDesc), data->begin(), data->Length, &m_inputLayout));
}

VertexShader ^ VertexShader::LoadParticle(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	VertexShader^ vertexShader = ref new VertexShader();
	vertexShader->ReloadParticle(deviceResources, data);
	return vertexShader;
}

void VertexShader::ReloadParticle(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->
		CreateVertexShader(data->begin(), data->Length, nullptr, &m_vertexShader));
	static const D3D11_INPUT_ELEMENT_DESC vertexDesc[] =
	{
		{ "POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "TEXCOORD", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 12, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "INDEX", 0, DXGI_FORMAT_R32_UINT, 0, 20, D3D11_INPUT_PER_VERTEX_DATA, 0 },
	};
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->
		CreateInputLayout(vertexDesc, ARRAYSIZE(vertexDesc), data->begin(), data->Length, &m_inputLayout));
}

VertexShader ^ VertexShader::LoadSkyBox(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	VertexShader^ vertexShader = ref new VertexShader();
	vertexShader->ReloadSkyBox(deviceResources, data);
	return vertexShader;
}

void VertexShader::ReloadSkyBox(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->
		CreateVertexShader(data->begin(), data->Length, nullptr, &m_vertexShader));
	static const D3D11_INPUT_ELEMENT_DESC vertexDesc[] =
	{
		{ "POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "TEXCOORD", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 12, D3D11_INPUT_PER_VERTEX_DATA, 0 },
	};
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->
		CreateInputLayout(vertexDesc, ARRAYSIZE(vertexDesc), data->begin(), data->Length, &m_inputLayout));
}

VertexShader ^ Coocoo3DGraphics::VertexShader::LoadUI(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	VertexShader^ vertexShader = ref new VertexShader();
	vertexShader->ReloadUI(deviceResources, data);
	return vertexShader;
}

void Coocoo3DGraphics::VertexShader::ReloadUI(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->
		CreateVertexShader(data->begin(), data->Length, nullptr, &m_vertexShader));
	static const D3D11_INPUT_ELEMENT_DESC vertexDesc[] =
	{
		{ "POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "RCI", 0, DXGI_FORMAT_R32G32B32_UINT, 0, 12, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		{ "SIZE", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 24, D3D11_INPUT_PER_VERTEX_DATA, 0 },
	};
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->
		CreateInputLayout(vertexDesc, ARRAYSIZE(vertexDesc), data->begin(), data->Length, &m_inputLayout));
}

VertexShader::~VertexShader()
{
}
