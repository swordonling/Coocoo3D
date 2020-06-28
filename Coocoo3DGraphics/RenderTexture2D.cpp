#include "pch.h"
#include "RenderTexture2D.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;

void RenderTexture2D::ReloadAsDepthStencil(DeviceResources ^ deviceResources, int width, int height)
{
	Initialize(deviceResources, width, height);
}

void RenderTexture2D::Initialize(DeviceResources ^ deviceResources, int width, int height)
{
	m_width = width;
	m_height = height;
	D3D11_TEXTURE2D_DESC tex2DDesc = {};
	tex2DDesc.Width = width;
	tex2DDesc.Height = height;
	tex2DDesc.Format = DXGI_FORMAT_R24G8_TYPELESS;
	tex2DDesc.ArraySize = 1;
	tex2DDesc.SampleDesc.Count = 1;
	tex2DDesc.SampleDesc.Quality = 0;
	tex2DDesc.Usage = D3D11_USAGE_DEFAULT;
	tex2DDesc.CPUAccessFlags = 0;
	tex2DDesc.BindFlags = D3D11_BIND_DEPTH_STENCIL | D3D11_BIND_SHADER_RESOURCE;
	tex2DDesc.MiscFlags = 0;
	tex2DDesc.MipLevels = 1;

	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateTexture2D(&tex2DDesc, nullptr, &m_texture2D));

	D3D11_SHADER_RESOURCE_VIEW_DESC shaderResourceViewDesc =
		CD3D11_SHADER_RESOURCE_VIEW_DESC(m_texture2D.Get(), D3D11_SRV_DIMENSION_TEXTURE2D, DXGI_FORMAT_R24_UNORM_X8_TYPELESS);
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateShaderResourceView(
		m_texture2D.Get(),
		&shaderResourceViewDesc,
		&m_shaderResourceView
	));

	D3D11_DEPTH_STENCIL_VIEW_DESC depthStencilViewDesc = {};
	depthStencilViewDesc.Format = DXGI_FORMAT_D24_UNORM_S8_UINT;
	depthStencilViewDesc.ViewDimension = D3D11_DSV_DIMENSION_TEXTURE2D;
	depthStencilViewDesc.Texture2D.MipSlice = 0;
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateDepthStencilView(m_texture2D.Get(), &depthStencilViewDesc, &m_depthStencilView));

	float color[4] = { 1,0,1,1 };
	D3D11_SAMPLER_DESC samplerDesc;
	samplerDesc.Filter = D3D11_FILTER_COMPARISON_MIN_MAG_MIP_LINEAR;
	samplerDesc.AddressU = D3D11_TEXTURE_ADDRESS_WRAP;
	samplerDesc.AddressV = D3D11_TEXTURE_ADDRESS_WRAP;
	samplerDesc.AddressW = D3D11_TEXTURE_ADDRESS_WRAP;
	samplerDesc.MipLODBias = 0.0f;
	samplerDesc.MaxAnisotropy = deviceResources->GetDeviceFeatureLevel() > D3D_FEATURE_LEVEL_9_1 ? 4 : 2;
	samplerDesc.ComparisonFunc = D3D11_COMPARISON_LESS;
	samplerDesc.BorderColor[0] = 1.0f;
	samplerDesc.BorderColor[1] = 0.0f;
	samplerDesc.BorderColor[2] = 1.0f;
	samplerDesc.BorderColor[3] = 1.0f;
	samplerDesc.MinLOD = 0;
	samplerDesc.MaxLOD = D3D11_FLOAT32_MAX;

	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateSamplerState(&samplerDesc, &m_samplerState));
}