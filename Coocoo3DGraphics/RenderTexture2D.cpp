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
	m_resourceFlags = D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL;
}
