#include "pch.h"
#include "GraphicsCommandList.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;

GraphicsCommandList ^ GraphicsCommandList::Load(DeviceResources ^ deviceResources)
{
	GraphicsCommandList^ graphicsCommandList =ref new GraphicsCommandList();
	graphicsCommandList->Reload(deviceResources);
	return graphicsCommandList;
}

void GraphicsCommandList::Reload(DeviceResources ^ deviceResources)
{
	deviceResources->GetD3DDevice()->CreateDeferredContext3(0, &m_deferredContext);
}
