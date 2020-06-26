#include "pch.h"
#include "PObject.h"
using namespace Coocoo3DGraphics;

void PObject::Reload(DeviceResources^ deviceResources, VertexShader ^ vertexShader, PixelShader ^ pixelShader)
{
	m_vertexShader = vertexShader;
	m_geometryShader = nullptr;
	m_pixelShader = pixelShader;
}

void PObject::Reload(DeviceResources^ deviceResources, VertexShader ^ vertexShader, GeometryShader ^ geometryShader, PixelShader ^ pixelShader)
{
	m_vertexShader = vertexShader;
	m_geometryShader = geometryShader;
	m_pixelShader = pixelShader;
}

void PObject::Reload(PObject ^ pObject)
{
	m_vertexShader = pObject->m_vertexShader;
	m_geometryShader = pObject->m_geometryShader;
	m_pixelShader = pObject->m_pixelShader;
}
