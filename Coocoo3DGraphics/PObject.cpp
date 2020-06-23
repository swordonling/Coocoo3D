#include "pch.h"
#include "PObject.h"
using namespace Coocoo3DGraphics;

PObject ^ PObject::Load(VertexShader ^ vertexShader, PixelShader ^ pixelShader)
{
	PObject^ pobject = ref new PObject();
	pobject->Reload(vertexShader, pixelShader);
	return pobject;
}

void PObject::Reload(VertexShader ^ vertexShader, PixelShader ^ pixelShader)
{
	m_vertexShader = vertexShader;
	m_geometryShader = nullptr;
	m_pixelShader = pixelShader;
}

PObject ^ PObject::Load(VertexShader ^ vertexShader, GeometryShader ^ geometryShader, PixelShader ^ pixelShader)
{
	PObject^ pobject = ref new PObject();
	pobject->Reload(vertexShader,geometryShader, pixelShader);
	return pobject;
}

void PObject::Reload(VertexShader ^ vertexShader, GeometryShader ^ geometryShader, PixelShader ^ pixelShader)
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
