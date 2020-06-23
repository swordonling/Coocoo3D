#include "pch.h"
#include "BlendState.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;

BlendState ^ BlendState::Load(DeviceResources ^ deviceResources)
{
	BlendState^ blendState = ref new BlendState();
	blendState->Reload(deviceResources);
	return blendState;
}

void BlendState::Reload(DeviceResources ^ deviceResources)
{
	auto device = deviceResources->GetD3DDevice();
	D3D11_BLEND_DESC blendDesc = {0};
	blendDesc.RenderTarget[0].BlendEnable = true;
	blendDesc.RenderTarget[0].SrcBlend = D3D11_BLEND_SRC_ALPHA;
	blendDesc.RenderTarget[0].DestBlend = D3D11_BLEND_INV_SRC_ALPHA;
	blendDesc.RenderTarget[0].BlendOp = D3D11_BLEND_OP_ADD;
	blendDesc.RenderTarget[0].SrcBlendAlpha = D3D11_BLEND_ONE;
	blendDesc.RenderTarget[0].DestBlendAlpha = D3D11_BLEND_ZERO;
	blendDesc.RenderTarget[0].BlendOpAlpha = D3D11_BLEND_OP_ADD;
	blendDesc.RenderTarget[0].RenderTargetWriteMask = D3D11_COLOR_WRITE_ENABLE_ALL;
	DX::ThrowIfFailed(device->CreateBlendState(&blendDesc, &m_blendState));
}
