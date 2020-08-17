#include "pch.h"
#include "RenderTextureCube.h"
using namespace Coocoo3DGraphics;

void RenderTextureCube::ReloadRTVUAV(int width, int height, DxgiFormat format)
{
	m_width = width;
	m_height = height;
	m_format = (DXGI_FORMAT)format;
	m_dsvFormat = DXGI_FORMAT_UNKNOWN;
	m_rtvFormat = (DXGI_FORMAT)format;
	m_uavFormat = (DXGI_FORMAT)format;
	m_resourceFlags = D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET | D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;
}
