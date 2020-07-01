#include "pch.h"
#include "PObject.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;

void PObject::Reload(DeviceResources^ deviceResources, GraphicsSignature ^ graphicsSignature, PObjectType type, VertexShader ^ vertexShader, GeometryShader ^ geometryShader, PixelShader ^ pixelShader)
{
	m_vertexShader = vertexShader;
	m_geometryShader = geometryShader;
	m_pixelShader = pixelShader;
	if (type == PObjectType::mmd)
	{
		static const D3D12_INPUT_ELEMENT_DESC inputLayout[] =
		{
			{ "POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 1, 0, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
			{ "NORMAL", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
			{ "TEXCOORD", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 12, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
			{ "EDGESCALE", 0, DXGI_FORMAT_R32_FLOAT, 0, 20, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
			{ "BONES", 0, DXGI_FORMAT_R32G32B32A32_UINT, 0, 24, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
			{ "WEIGHTS", 0, DXGI_FORMAT_R32G32B32A32_FLOAT, 0, 40, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
			{ "TANGENT", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 56, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
		};
		D3D12_GRAPHICS_PIPELINE_STATE_DESC state = {};
		state.InputLayout = { inputLayout, _countof(inputLayout) };
		state.pRootSignature = graphicsSignature->m_rootSignatures[0].Get();
		state.VS = CD3DX12_SHADER_BYTECODE(vertexShader->byteCode.Get());
		if (geometryShader != nullptr)
			state.GS = CD3DX12_SHADER_BYTECODE(geometryShader->byteCode.Get());
		state.PS = CD3DX12_SHADER_BYTECODE(pixelShader->byteCode.Get());
		state.BlendState = CD3DX12_BLEND_DESC(D3D12_DEFAULT);
		state.DepthStencilState = CD3DX12_DEPTH_STENCIL_DESC(D3D12_DEFAULT);
		state.SampleMask = UINT_MAX;
		state.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
		state.NumRenderTargets = 1;
		state.RTVFormats[0] = deviceResources->GetBackBufferFormat();
		state.DSVFormat = deviceResources->GetDepthBufferFormat();
		state.SampleDesc.Count = 1;

		state.RasterizerState = CD3DX12_RASTERIZER_DESC(D3D12_DEFAULT);
		DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateGraphicsPipelineState(&state, IID_PPV_ARGS(&m_pipelineState[0])));
		state.RasterizerState = CD3DX12_RASTERIZER_DESC(D3D12_FILL_MODE_SOLID, D3D12_CULL_MODE_NONE, false, 0, 0.0f, 0.0f, true, false, false, 0, D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF);
		DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateGraphicsPipelineState(&state, IID_PPV_ARGS(&m_pipelineState[1])));
		state.RasterizerState = CD3DX12_RASTERIZER_DESC(D3D12_FILL_MODE_SOLID, D3D12_CULL_MODE_FRONT, false, 0, 0.0f, 0.0f, true, false, false, 0, D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF);
		DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateGraphicsPipelineState(&state, IID_PPV_ARGS(&m_pipelineState[2])));


		D3D12_BLEND_DESC blendDesc1 = {};
		blendDesc1.RenderTarget[0].BlendEnable = true;
		blendDesc1.RenderTarget[0].SrcBlend = D3D12_BLEND_SRC_ALPHA;
		blendDesc1.RenderTarget[0].DestBlend = D3D12_BLEND_INV_SRC_ALPHA;
		blendDesc1.RenderTarget[0].BlendOp = D3D12_BLEND_OP_ADD;
		blendDesc1.RenderTarget[0].SrcBlendAlpha = D3D12_BLEND_ONE;
		blendDesc1.RenderTarget[0].DestBlendAlpha = D3D12_BLEND_ZERO;
		blendDesc1.RenderTarget[0].BlendOpAlpha = D3D12_BLEND_OP_ADD;
		blendDesc1.RenderTarget[0].RenderTargetWriteMask = D3D12_COLOR_WRITE_ENABLE_ALL;

		state.BlendState = blendDesc1;

		state.RasterizerState = CD3DX12_RASTERIZER_DESC(D3D12_DEFAULT);
		DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateGraphicsPipelineState(&state, IID_PPV_ARGS(&m_pipelineState[3])));
		state.RasterizerState = CD3DX12_RASTERIZER_DESC(D3D12_FILL_MODE_SOLID, D3D12_CULL_MODE_NONE, false, 0, 0.0f, 0.0f, true, false, false, 0, D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF);
		DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateGraphicsPipelineState(&state, IID_PPV_ARGS(&m_pipelineState[4])));
		state.RasterizerState = CD3DX12_RASTERIZER_DESC(D3D12_FILL_MODE_SOLID, D3D12_CULL_MODE_FRONT, false, 0, 0.0f, 0.0f, true, false, false, 0, D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF);
		DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateGraphicsPipelineState(&state, IID_PPV_ARGS(&m_pipelineState[5])));

		state.BlendState = CD3DX12_BLEND_DESC(D3D12_DEFAULT);
		state.RasterizerState = CD3DX12_RASTERIZER_DESC(D3D12_DEFAULT);
		state.PS = {};
		state.RTVFormats[0] = DXGI_FORMAT_UNKNOWN;
		state.NumRenderTargets = 0;
		DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateGraphicsPipelineState(&state, IID_PPV_ARGS(&m_pipelineState[c_indexPipelineStateDepth])));
	}
}

void PObject::Reload(PObject ^ pObject)
{
	m_vertexShader = pObject->m_vertexShader;
	m_geometryShader = pObject->m_geometryShader;
	m_pixelShader = pObject->m_pixelShader;
	for (int i = 0; i < _countof(m_pipelineState); i++)
	{
		m_pipelineState[i] = pObject->m_pipelineState[i];
	}
}

