#include "pch.h"
#include "RenderTexture2D.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;

void RenderTexture2D::ReloadAsDepthStencil(DeviceResources ^ deviceResources, int width, int height)
{
	m_width = width;
	m_height = height;
	m_format = DXGI_FORMAT_R32_FLOAT;
	m_dsvFormat = DXGI_FORMAT_D32_FLOAT;
	m_rtvFormat = DXGI_FORMAT_UNKNOWN;
	m_resourceFlags = D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL;
}

void RenderTexture2D::ReloadAsRenderTarget(DeviceResources ^ deviceResources, int width, int height)
{
	m_width = width;
	m_height = height;
	m_format = deviceResources->GetBackBufferFormat();
	m_dsvFormat = DXGI_FORMAT_UNKNOWN;
	m_rtvFormat = deviceResources->GetBackBufferFormat();
	m_resourceFlags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;
}

void RenderTexture2D::ReloadAsRenderTarget(DeviceResources ^ deviceResources, int width, int height, DxgiFormat format)
{
	m_width = width;
	m_height = height;
	m_format = (DXGI_FORMAT)format;
	m_dsvFormat = DXGI_FORMAT_UNKNOWN;
	m_rtvFormat = (DXGI_FORMAT)format;
	m_resourceFlags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;
}
