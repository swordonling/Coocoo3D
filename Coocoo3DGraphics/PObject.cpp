#include "pch.h"
#include "PObject.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;

void PObject::Reload(DeviceResources^ deviceResources, PObjectType type, VertexShader ^ vertexShader, GeometryShader ^ geometryShader, PixelShader ^ pixelShader)
{
	m_geometryShader = geometryShader;
	m_pixelShader = pixelShader;


	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->
		CreateVertexShader(vertexShader->byteCode->GetBufferPointer(), vertexShader->byteCode->GetBufferSize(), nullptr, &m_vertexShader));
	if (type == PObjectType::mmd)
	{
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
			CreateInputLayout(vertexDesc, ARRAYSIZE(vertexDesc), vertexShader->byteCode->GetBufferPointer(), vertexShader->byteCode->GetBufferSize(), &m_inputLayout));
	}
	else if (type == PObjectType::ui3d)
	{
		static const D3D11_INPUT_ELEMENT_DESC vertexDesc[] =
		{
			{ "POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0, D3D11_INPUT_PER_VERTEX_DATA, 0 },
			{ "RCI", 0, DXGI_FORMAT_R32G32B32_UINT, 0, 12, D3D11_INPUT_PER_VERTEX_DATA, 0 },
			{ "SIZE", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 24, D3D11_INPUT_PER_VERTEX_DATA, 0 },
		};
		DX::ThrowIfFailed(deviceResources->GetD3DDevice()->
			CreateInputLayout(vertexDesc, ARRAYSIZE(vertexDesc), vertexShader->byteCode->GetBufferPointer(), vertexShader->byteCode->GetBufferSize(), &m_inputLayout));
	}
	Other1(deviceResources);
}

void PObject::Reload(PObject ^ pObject)
{
	m_vertexShader = pObject->m_vertexShader;
	m_inputLayout = pObject->m_inputLayout;

	m_geometryShader = pObject->m_geometryShader;
	m_pixelShader = pObject->m_pixelShader;

	m_blendStateAlpha = pObject->m_blendStateAlpha;
	m_blendStateOqaque = pObject->m_blendStateOqaque;
	m_RasterizerStateCullBack = pObject->m_RasterizerStateCullBack;
	m_RasterizerStateCullFront = pObject->m_RasterizerStateCullFront;
	m_RasterizerStateCullNone = pObject->m_RasterizerStateCullNone;
}

void PObject::Other1(DeviceResources^ deviceResources)
{
	auto d3dDevice = deviceResources->GetD3DDevice();
	D3D11_BLEND_DESC blendDesc = { 0 };
	blendDesc.RenderTarget[0].BlendEnable = true;
	blendDesc.RenderTarget[0].SrcBlend = D3D11_BLEND_SRC_ALPHA;
	blendDesc.RenderTarget[0].DestBlend = D3D11_BLEND_INV_SRC_ALPHA;
	blendDesc.RenderTarget[0].BlendOp = D3D11_BLEND_OP_ADD;
	blendDesc.RenderTarget[0].SrcBlendAlpha = D3D11_BLEND_ONE;
	blendDesc.RenderTarget[0].DestBlendAlpha = D3D11_BLEND_ZERO;
	blendDesc.RenderTarget[0].BlendOpAlpha = D3D11_BLEND_OP_ADD;
	blendDesc.RenderTarget[0].RenderTargetWriteMask = D3D11_COLOR_WRITE_ENABLE_ALL;
	DX::ThrowIfFailed(d3dDevice->CreateBlendState(&blendDesc, &m_blendStateAlpha));

	auto desc = D3D11_RASTERIZER_DESC();
	desc.FillMode = D3D11_FILL_SOLID;
	desc.CullMode = D3D11_CULL_BACK;
	desc.FrontCounterClockwise = FALSE;
	desc.DepthBias = D3D11_DEFAULT_DEPTH_BIAS;
	desc.DepthBiasClamp = D3D11_DEFAULT_DEPTH_BIAS_CLAMP;
	desc.SlopeScaledDepthBias = D3D11_DEFAULT_SLOPE_SCALED_DEPTH_BIAS;
	desc.DepthClipEnable = TRUE;
	desc.ScissorEnable = FALSE;
	desc.MultisampleEnable = FALSE;
	desc.AntialiasedLineEnable = FALSE;

	desc.CullMode = D3D11_CULL_NONE;
	d3dDevice->CreateRasterizerState(&desc, &m_RasterizerStateCullNone);
	desc.CullMode = D3D11_CULL_FRONT;
	d3dDevice->CreateRasterizerState(&desc, &m_RasterizerStateCullFront);
	desc.CullMode = D3D11_CULL_BACK;
	d3dDevice->CreateRasterizerState(&desc, &m_RasterizerStateCullBack);
}
