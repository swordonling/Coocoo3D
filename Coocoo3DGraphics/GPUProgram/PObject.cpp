#include "pch.h"
#include "PObject.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;
static const D3D12_INPUT_ELEMENT_DESC inputLayoutMMD[] =
{
	{ "POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 1, 0, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
	{ "POSITION", 1, DXGI_FORMAT_R32G32B32_FLOAT, 2, 0, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
	{ "NORMAL", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
	{ "TEXCOORD", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 12, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
	{ "EDGESCALE", 0, DXGI_FORMAT_R32_FLOAT, 0, 20, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
	{ "BONES", 0, DXGI_FORMAT_R32G32B32A32_UINT, 0, 24, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
	{ "WEIGHTS", 0, DXGI_FORMAT_R32G32B32A32_FLOAT, 0, 40, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
	{ "TANGENT", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 56, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
};
static const D3D12_INPUT_ELEMENT_DESC inputLayoutSkinned[] =
{
	{ "POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
	{ "NORMAL", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 12, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
	{ "TEXCOORD", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 24, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
	{ "TANGENT", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 32, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
	{ "EDGESCALE", 0, DXGI_FORMAT_R32_FLOAT, 0, 44, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
};
inline D3D12_BLEND_DESC BlendDescAlpha()
{
	D3D12_BLEND_DESC blendDescAlpha = {};
	blendDescAlpha.RenderTarget[0].BlendEnable = true;
	blendDescAlpha.RenderTarget[0].SrcBlend = D3D12_BLEND_SRC_ALPHA;
	blendDescAlpha.RenderTarget[0].DestBlend = D3D12_BLEND_INV_SRC_ALPHA;
	blendDescAlpha.RenderTarget[0].BlendOp = D3D12_BLEND_OP_ADD;
	blendDescAlpha.RenderTarget[0].SrcBlendAlpha = D3D12_BLEND_ONE;
	blendDescAlpha.RenderTarget[0].DestBlendAlpha = D3D12_BLEND_INV_SRC_ALPHA;
	blendDescAlpha.RenderTarget[0].BlendOpAlpha = D3D12_BLEND_OP_ADD;
	blendDescAlpha.RenderTarget[0].RenderTargetWriteMask = D3D12_COLOR_WRITE_ENABLE_ALL;
	return blendDescAlpha;
}
inline D3D12_BLEND_DESC BlendDescSelect(BlendState blendState)
{
	if (blendState == BlendState::none)
		return CD3DX12_BLEND_DESC(D3D12_DEFAULT);
	else if (blendState == BlendState::alpha)
		return BlendDescAlpha();
	return D3D12_BLEND_DESC{};
}

void PObject::Reload(DeviceResources^ deviceResources, GraphicsSignature^ graphicsSignature, PObjectType type, BlendState blendState, VertexShader^ vertexShader, GeometryShader^ geometryShader, PixelShader^ pixelShader, DxgiFormat rtvFormat)
{
	Unload();
	m_vertexShader = vertexShader;
	m_geometryShader = geometryShader;
	m_pixelShader = pixelShader;
	static const D3D12_INPUT_ELEMENT_DESC inputLayoutPosOnly[] =
	{
		{ "POSITION", 0, DXGI_FORMAT_R32G32B32A32_FLOAT, 0, 0, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0 },
	};

	D3D12_GRAPHICS_PIPELINE_STATE_DESC state = {};
	if (type == PObjectType::mmd)
		state.InputLayout = { inputLayoutMMD, _countof(inputLayoutMMD) };
	else if (type == PObjectType::postProcess)
		state.InputLayout = { inputLayoutPosOnly, _countof(inputLayoutPosOnly) };
	state.pRootSignature = graphicsSignature->m_rootSignature.Get();
	state.VS = CD3DX12_SHADER_BYTECODE(vertexShader->byteCode.Get());
	if (m_geometryShader != nullptr)
		state.GS = CD3DX12_SHADER_BYTECODE(m_geometryShader->byteCode.Get());
	state.PS = CD3DX12_SHADER_BYTECODE(pixelShader->byteCode.Get());
	state.BlendState = BlendDescSelect(blendState);
	state.DepthStencilState = CD3DX12_DEPTH_STENCIL_DESC(D3D12_DEFAULT);
	state.SampleMask = UINT_MAX;
	state.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
	state.NumRenderTargets = 1;
	state.RTVFormats[0] = (DXGI_FORMAT)rtvFormat;
	state.DSVFormat = deviceResources->GetDepthBufferFormat();
	state.SampleDesc.Count = 1;

	state.RasterizerState = CD3DX12_RASTERIZER_DESC(D3D12_DEFAULT);
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateGraphicsPipelineState(&state, IID_PPV_ARGS(&m_pipelineState[0])));
	state.RasterizerState = CD3DX12_RASTERIZER_DESC(D3D12_FILL_MODE_SOLID, D3D12_CULL_MODE_NONE, false, 0, 0.0f, 0.0f, true, false, false, 0, D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF);
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateGraphicsPipelineState(&state, IID_PPV_ARGS(&m_pipelineState[1])));
	state.RasterizerState = CD3DX12_RASTERIZER_DESC(D3D12_FILL_MODE_SOLID, D3D12_CULL_MODE_FRONT, false, 0, 0.0f, 0.0f, true, false, false, 0, D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF);
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateGraphicsPipelineState(&state, IID_PPV_ARGS(&m_pipelineState[2])));

}

void PObject::ReloadDepthOnly(DeviceResources^ deviceResources, GraphicsSignature^ graphicsSignature, VertexShader^ vsTransform, PixelShader^ psDepthAlphaClip, int depthOffset)
{
	Unload();
	m_vertexShader = vsTransform;
	m_geometryShader = nullptr;
	m_pixelShader = psDepthAlphaClip;

	D3D12_GRAPHICS_PIPELINE_STATE_DESC state = {};
	state.InputLayout = { inputLayoutSkinned, _countof(inputLayoutSkinned) };
	state.pRootSignature = graphicsSignature->m_rootSignature.Get();
	state.VS = CD3DX12_SHADER_BYTECODE(vsTransform->byteCode.Get());
	state.BlendState = CD3DX12_BLEND_DESC(D3D12_DEFAULT);
	state.DepthStencilState = CD3DX12_DEPTH_STENCIL_DESC(D3D12_DEFAULT);
	state.SampleMask = UINT_MAX;
	state.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
	state.DSVFormat = deviceResources->GetDepthBufferFormat();
	state.SampleDesc.Count = 1;
	state.RasterizerState = CD3DX12_RASTERIZER_DESC(D3D12_FILL_MODE_SOLID, D3D12_CULL_MODE_NONE, false, depthOffset, 0.0f, 1.0f, true, false, false, 0, D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF);
	if (psDepthAlphaClip != nullptr)
		state.PS = CD3DX12_SHADER_BYTECODE(psDepthAlphaClip->byteCode.Get());
	else
		state.PS = {};
	state.RTVFormats[0] = DXGI_FORMAT_UNKNOWN;
	state.NumRenderTargets = 0;
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateGraphicsPipelineState(&state, IID_PPV_ARGS(&m_pipelineState[c_indexPipelineStateDepth])));
}

bool PObject::ReloadSkinning(DeviceResources^ deviceResources, GraphicsSignature^ graphicsSignature, VertexShader^ vs, GeometryShader^ gs)
{
	Unload();
	m_vertexShader = vs;
	m_geometryShader = gs;
	m_pixelShader = nullptr;
	struct PipelineStateStream
	{
		CD3DX12_PIPELINE_STATE_STREAM_ROOT_SIGNATURE pROOTSIGNATURE;
		CD3DX12_PIPELINE_STATE_STREAM_INPUT_LAYOUT INPUTLAYOUT;
		CD3DX12_PIPELINE_STATE_STREAM_PRIMITIVE_TOPOLOGY PRIMITIVETOPOLOGY;
		CD3DX12_PIPELINE_STATE_STREAM_VS VS;
		CD3DX12_PIPELINE_STATE_STREAM_GS GS;
		CD3DX12_PIPELINE_STATE_STREAM_STREAM_OUTPUT STREAMOUT;
	} pipelineStateStream;
	pipelineStateStream.pROOTSIGNATURE = graphicsSignature->m_rootSignature.Get();
	pipelineStateStream.INPUTLAYOUT = { inputLayoutMMD, _countof(inputLayoutMMD) };
	pipelineStateStream.VS = CD3DX12_SHADER_BYTECODE(vs->byteCode.Get());
	if (gs != nullptr)
		pipelineStateStream.GS = CD3DX12_SHADER_BYTECODE(gs->byteCode.Get());
	D3D12_SO_DECLARATION_ENTRY declarations[] =
	{
		{0,"POSITION",0,0,3,0},
		{0,"NORMAL",0,0,3,0},
		{0,"TEXCOORD",0,0,2,0},
		{0,"TANGENT",0,0,3,0},
		{0,"EDGESCALE",0,0,1,0},
	};
	UINT bufferStrides[] = { 64 };
	pipelineStateStream.STREAMOUT = { declarations ,_countof(declarations),bufferStrides,_countof(bufferStrides),0 };
	pipelineStateStream.PRIMITIVETOPOLOGY = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
	D3D12_PIPELINE_STATE_STREAM_DESC state2 = { sizeof(pipelineStateStream),&pipelineStateStream };
	if (FAILED(deviceResources->GetD3DDevice5()->CreatePipelineState(&state2, IID_PPV_ARGS(&m_pipelineState[c_indexPipelineStateSkinning]))))
		return false;
	return true;
}

bool PObject::ReloadDrawing(DeviceResources^ deviceResources, GraphicsSignature^ graphicsSignature, BlendState blendState, VertexShader^ vs, GeometryShader^ gs, PixelShader^ ps, DxgiFormat rtvFormat)
{
	Unload();
	m_vertexShader = vs;
	m_geometryShader = gs;
	m_pixelShader = ps;


	D3D12_GRAPHICS_PIPELINE_STATE_DESC state = {};
	state.InputLayout = { inputLayoutSkinned, _countof(inputLayoutSkinned) };
	state.pRootSignature = graphicsSignature->m_rootSignature.Get();
	state.VS = CD3DX12_SHADER_BYTECODE(vs->byteCode.Get());
	if (m_geometryShader != nullptr)
		state.GS = CD3DX12_SHADER_BYTECODE(m_geometryShader->byteCode.Get());
	state.PS = CD3DX12_SHADER_BYTECODE(ps->byteCode.Get());
	state.BlendState = BlendDescSelect(blendState);
	state.DepthStencilState = CD3DX12_DEPTH_STENCIL_DESC(D3D12_DEFAULT);
	state.DepthStencilState.DepthFunc = D3D12_COMPARISON_FUNC_LESS_EQUAL;
	state.SampleMask = UINT_MAX;
	state.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
	state.NumRenderTargets = 1;
	state.RTVFormats[0] = (DXGI_FORMAT)rtvFormat;
	state.DSVFormat = deviceResources->GetDepthBufferFormat();
	state.SampleDesc.Count = 1;

	state.RasterizerState = CD3DX12_RASTERIZER_DESC(D3D12_DEFAULT);
	if (FAILED(deviceResources->GetD3DDevice()->CreateGraphicsPipelineState(&state, IID_PPV_ARGS(&m_pipelineState[0]))))return false;
	state.RasterizerState = CD3DX12_RASTERIZER_DESC(D3D12_FILL_MODE_SOLID, D3D12_CULL_MODE_NONE, false, 0, 0.0f, 0.0f, true, false, false, 0, D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF);
	if (FAILED(deviceResources->GetD3DDevice()->CreateGraphicsPipelineState(&state, IID_PPV_ARGS(&m_pipelineState[1]))))return false;
	state.RasterizerState = CD3DX12_RASTERIZER_DESC(D3D12_FILL_MODE_SOLID, D3D12_CULL_MODE_FRONT, false, 0, 0.0f, 0.0f, true, false, false, 0, D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF);
	if (FAILED(deviceResources->GetD3DDevice()->CreateGraphicsPipelineState(&state, IID_PPV_ARGS(&m_pipelineState[2]))))return false;


	return true;
}

void PObject::Reload(PObject^ pObject)
{
	m_vertexShader = pObject->m_vertexShader;
	m_geometryShader = pObject->m_geometryShader;
	m_pixelShader = pObject->m_pixelShader;
	for (int i = 0; i < _countof(m_pipelineState); i++)
	{
		m_pipelineState[i] = pObject->m_pipelineState[i];
	}
}

void PObject::Unload()
{
	Status = GraphicsObjectStatus::unload;
	for (int i = 0; i < _countof(m_pipelineState); i++)
	{
		m_pipelineState[i].Reset();
	}
}
